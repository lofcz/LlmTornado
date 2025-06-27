using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Audio.Models.OpenAi;

namespace LlmTornado.Audio;

/// <summary>
///     You can use this endpoint for audio transcription or translation.
/// </summary>
public class AudioEndpoint : EndpointBase
{
    /// <summary>
    ///     Creates audio endpoint object.
    /// </summary>
    /// <param name="api"></param>
    public AudioEndpoint(TornadoApi api) : base(api)
    {
    }
    
    /// <summary>
    ///     Audio endpoint.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Audio;
    
    /// <summary>
    ///     Sends transcript request to openai and returns verbose_json result.
    /// </summary>
    public Task<TranscriptionResult?> CreateTranscription(TranscriptionRequest request)
    {
        return PostAudio($"/transcriptions", request);
    }

    /// <summary>
    ///     Translates audio into English.
    /// </summary>
    public Task<TranscriptionResult?> CreateTranslation(TranslationRequest request)
    {
        return PostAudio($"/translations", new TranscriptionRequest
            {
                File = request.File,
                Model = request.Model,
                Prompt = request.Prompt,
                // ResponseFormat = request.ResponseFormat,
                Temperature = request.Temperature
            }
        );
    }

    /// <summary>
    ///     Converts string text into speech (tts)
    /// </summary>
    public Task<SpeechTtsResult?> CreateSpeech(SpeechRequest request)
    {
        return PostSpeech(request);
    }

    private async Task<SpeechTtsResult?> PostSpeech(SpeechRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model);
        string url = provider.ApiUrl(CapabilityEndpoints.Audio, $"/speech");
        
        StreamResponse? x = await HttpPostStream(provider, Endpoint, url, request);
        return x is null ? null : new SpeechTtsResult(x);
    }

    private static TranscriptionSerializedRequest SerializeRequest(TranscriptionRequest request)
    {
        TranscriptionSerializedRequest serializedRequest = new TranscriptionSerializedRequest();

        if (request.Stream ?? false)
        {
            serializedRequest.Content.Add(new StringContent("True"), "stream");
        }
        
        if (request.TimestampGranularities?.Count > 0 && AudioModelOpenAi.VerboseJsonCompatibleModels.Contains(request.Model) && request.ResponseFormat is AudioTranscriptionResponseFormats.VerboseJson)
        {
            foreach (TimestampGranularities granularity in request.TimestampGranularities)
            {
                serializedRequest.Content.Add(new StringContent(TimestampGranularitiesCls.Encode(granularity)), "timestamp_granularities[]");
            }
        }
        
        if (request.Include?.Count > 0 && AudioModelOpenAi.IncludeCompatibleModels.Contains(request.Model) && request.ResponseFormat is AudioTranscriptionResponseFormats.Json)
        {
            foreach (TranscriptionRequestIncludeItems item in request.Include)
            {
                serializedRequest.Content.Add(new StringContent(TranscriptionRequestIncludeItemsCls.Encode(item)), "include[]");
            }
        }
        
        if (request.File.Data is not null)
        {
            serializedRequest.Ms = new MemoryStream(request.File.Data);
            serializedRequest.Sc = new StreamContent(serializedRequest.Ms);
            
            serializedRequest.Sc.Headers.ContentLength = request.File.Data.Length;
            serializedRequest.Sc.Headers.ContentType = new MediaTypeHeaderValue(request.File.GetContentType);
        
            serializedRequest.Content.Add(serializedRequest.Sc, "file", "test.wav");
        }
        else if (request.File.File is not null)
        {
            serializedRequest.Sc = new StreamContent(request.File.File);
            serializedRequest.Sc.Headers.ContentLength = request.File.File.Length;
            serializedRequest.Sc.Headers.ContentType = new MediaTypeHeaderValue(request.File.GetContentType);
        
            serializedRequest.Content.Add(serializedRequest.Sc, "file", "test.wav");
        }
        
        serializedRequest.Content.Add(new StringContent(request.Model.GetApiName), "model");

        if (!request.Prompt.IsNullOrWhiteSpace())
        {
            serializedRequest.Content.Add(new StringContent(request.Prompt), "prompt");
        }
        
        serializedRequest.Content.Add(new StringContent(request.GetResponseFormat), "response_format");

        if (!request.Temperature.HasValue)
        {
            serializedRequest.Content.Add(new StringContent(0f.ToString(CultureInfo.InvariantCulture)), "temperature");
        }

        if (!request.Language.IsNullOrWhiteSpace())
        {
            serializedRequest.Content.Add(new StringContent(request.Language), "language");
        }

        return serializedRequest;
    }

    /// <summary>
    /// Streams transcription.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="eventsHandler">Handler of the streamed events.</param>
    /// <param name="token">Optional cancellation token.</param>
    public async Task StreamTranscriptionRich(TranscriptionRequest request, TranscriptionStreamEventHandler? eventsHandler, CancellationToken token = default)
    {
        await foreach (object res in StreamAudio($"/transcriptions", request, eventsHandler).WithCancellation(token))
        {
            if (res is TranscriptionResult tr)
            {
                switch (tr.EventType)
                {
                    case AudioStreamEventTypes.TranscriptDelta:
                    {
                        if (eventsHandler?.ChunkHandler is not null)
                        {
                            await eventsHandler.ChunkHandler.Invoke(tr);   
                        }

                        break;
                    }
                    case AudioStreamEventTypes.TranscriptDone:
                    {
                        if (eventsHandler?.BlockHandler is not null)
                        {
                            await eventsHandler.BlockHandler.Invoke(tr);   
                        }

                        break;
                    }
                }
            }
        }
    }
    
    private async IAsyncEnumerable<object> StreamAudio(string url, TranscriptionRequest request, TranscriptionStreamEventHandler? handler)
    {
        request.Stream = true;
        
        IEndpointProvider provider = Api.GetProvider(request.Model);
        url = provider.ApiUrl(CapabilityEndpoints.Audio, url);

        TranscriptionSerializedRequest serialized = SerializeRequest(request); 

        TornadoRequestContent requestBody = new TornadoRequestContent(serialized.Content, request.Model, url, provider, CapabilityEndpoints.Audio);
        await using TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(provider, Endpoint, requestBody.Url, queryParams: null, HttpVerbs.Post, requestBody.Body, request.Model, request.CancellationToken);

        if (tornadoStreamRequest.Exception is not null)
        {
            if (handler?.HttpExceptionHandler is null)
            {
                throw tornadoStreamRequest.Exception;
            }

            await handler.HttpExceptionHandler(new HttpFailedRequest
            {
                Exception = tornadoStreamRequest.Exception,
                Result = tornadoStreamRequest.CallResponse,
                Request = tornadoStreamRequest.CallRequest,
                RawMessage = tornadoStreamRequest.Response ?? new HttpResponseMessage(),
                Body = requestBody
            });
            
            yield break;
        }

        if (handler?.OutboundHttpRequestHandler is not null && tornadoStreamRequest.CallRequest is not null)
        {
            await handler.OutboundHttpRequestHandler(tornadoStreamRequest.CallRequest);
        }

        if (tornadoStreamRequest.StreamReader is not null)
        {
            await foreach (AudioStreamEvent? x in provider.InboundStream<AudioStreamEvent>(tornadoStreamRequest.StreamReader))
            {
                if (x is null)
                {
                    continue;
                }

                AudioStreamEventTypes eventType = AudioStreamEvent.Map.GetValueOrDefault(x.Type, AudioStreamEventTypes.Unknown);

                switch (eventType)
                {
                    case AudioStreamEventTypes.TranscriptDelta:
                    {
                        yield return new TranscriptionResult
                        {
                            Logprobs = x.Logprobs,
                            Text = x.Delta ?? string.Empty,
                            EventType = AudioStreamEventTypes.TranscriptDelta
                        };
                        break;
                    }
                    case AudioStreamEventTypes.TranscriptDone:
                    {
                        yield return new TranscriptionResult
                        {
                            Logprobs = x.Logprobs,
                            Text = x.Text ?? string.Empty,
                            EventType = AudioStreamEventTypes.TranscriptDone
                        };
                        break;
                    }
                }
            }
        }
    }

    private async Task<TranscriptionResult?> PostAudio(string url, TranscriptionRequest request)
    {
        request.Stream = null;
        
        IEndpointProvider provider = Api.GetProvider(request.Model);
        url = provider.ApiUrl(CapabilityEndpoints.Audio, url);

        TranscriptionSerializedRequest serialized = SerializeRequest(request); 
        TranscriptionResult? result;

        try
        {
            if (request.ResponseFormat is AudioTranscriptionResponseFormats.Text or AudioTranscriptionResponseFormats.Srt or AudioTranscriptionResponseFormats.Vtt)
            {
                object? obj = await HttpPost1(typeof(string), provider, Endpoint, url, serialized.Content, ct: request.CancellationToken);

                if (obj is string str)
                {
                    result = new TranscriptionResult
                    {
                        Text = str.Trim(),
                        Task = "transcription"
                    };

                    return result;
                }
            }
            
            result = await HttpPost1<TranscriptionResult>(provider, Endpoint, url, serialized.Content, ct: request.CancellationToken);
        }
        finally
        {
            serialized.Dispose();
        }

        return result;
    }
}