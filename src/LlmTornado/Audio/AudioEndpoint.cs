using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Audio.Models;
using LlmTornado.Audio.Models;
using LlmTornado.Audio.Models.OpenAi;
using LlmTornado.Audio.Vendors.MiniMax;
using LlmTornado.Audio.Vendors.Zai;
using Newtonsoft.Json;

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
                Url = request.Url,
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
    
    /// <summary>
    /// Generates music from lyrics and an optional style/mood prompt.
    /// Currently supported by MiniMax.
    /// </summary>
    /// <param name="request">The music generation request containing lyrics and style description.</param>
    /// <returns>The generated music result with audio data and metadata.</returns>
    public async Task<MusicGenerationResult?> GenerateMusic(MusicGenerationRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model ?? AudioModel.MiniMax.Music.Music25);
        string url = provider.ApiUrl(CapabilityEndpoints.Music, null);
        string json = JsonConvert.SerializeObject(new VendorMiniMaxMusicRequest(request), EndpointBase.NullSettings);
        
        return await HttpPost1<MusicGenerationResult>(provider, CapabilityEndpoints.Music, url, postData: json);
    }
    
    /// <summary>
    /// Generates lyrics for a song, supporting full song creation and lyrics editing/continuation.
    /// Currently supported by MiniMax.
    /// </summary>
    /// <param name="request">The lyrics generation request with mode, prompt, and optional existing lyrics.</param>
    /// <returns>The generated lyrics with song title and style tags.</returns>
    public async Task<LyricsGenerationResult?> GenerateLyrics(LyricsGenerationRequest request)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.MiniMax);
        string url = provider.ApiUrl(CapabilityEndpoints.Lyrics, null);
        string json = JsonConvert.SerializeObject(new VendorMiniMaxLyricsRequest(request), EndpointBase.NullSettings);
        
        return await HttpPost1<LyricsGenerationResult>(provider, CapabilityEndpoints.Lyrics, url, postData: json);
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
        
        // Timestamp granularities: supported by OpenAI Whisper models and Groq models with verbose_json format
        bool supportsTimestampGranularities = AudioModelOpenAi.VerboseJsonCompatibleModels.Contains(request.Model) || request.Model?.Provider == LLmProviders.Groq;
        if (request.TimestampGranularities?.Count > 0 && supportsTimestampGranularities && request.ResponseFormat is AudioTranscriptionResponseFormats.VerboseJson)
        {
            foreach (TimestampGranularities granularity in request.TimestampGranularities)
            {
                serializedRequest.Content.Add(new StringContent(TimestampGranularitiesCls.Encode(granularity)), "timestamp_granularities[]");
            }
        }
        
        if (request.Include?.Count > 0 && AudioModelOpenAi.IncludeCompatibleModels.Contains(request.Model ?? string.Empty) && request.ResponseFormat is AudioTranscriptionResponseFormats.Json && request.Model != AudioModel.OpenAi.Gpt4.Gpt4OTranscribeDiarize)
        {
            foreach (TranscriptionRequestIncludeItems item in request.Include)
            {
                serializedRequest.Content.Add(new StringContent(TranscriptionRequestIncludeItemsCls.Encode(item)), "include[]");
            }
        }
        
        if (request.ChunkingStrategy is not null)
        {
            switch (request.ChunkingStrategy.Type)
            {
                case TranscriptionChunkingStrategyType.Auto:
                {
                    serializedRequest.Content.Add(new StringContent("auto"), "chunking_strategy");
                    break;
                }
                case TranscriptionChunkingStrategyType.ServerVad:
                {
                    var strategy = new 
                    {
                        type = "server_vad",
                        prefix_padding_ms = request.ChunkingStrategy.PrefixPaddingMs,
                        silence_duration_ms = request.ChunkingStrategy.SilenceDurationMs,
                        threshold = request.ChunkingStrategy.Threshold
                    };
                    serializedRequest.Content.Add(new StringContent(JsonConvert.SerializeObject(strategy, EndpointBase.NullSettings)), "chunking_strategy");
                    break;
                }
            }
        }

        if (request.KnownSpeakerNames?.Count > 0)
        {
            foreach (string name in request.KnownSpeakerNames)
            {
                serializedRequest.Content.Add(new StringContent(name), "known_speaker_names[]");
            }
        }
        
        if (request.KnownSpeakerReferences?.Count > 0)
        {
            foreach (string reference in request.KnownSpeakerReferences)
            {
                serializedRequest.Content.Add(new StringContent(reference), "known_speaker_references[]");
            }
        }
        
        if (request.File?.Data is not null)
        {
            serializedRequest.Ms = new MemoryStream(request.File.Data);
            serializedRequest.Sc = new StreamContent(serializedRequest.Ms);
            
            serializedRequest.Sc.Headers.ContentLength = request.File.Data.Length;
            serializedRequest.Sc.Headers.ContentType = new MediaTypeHeaderValue(request.File.GetContentType);

            string fileName = $"audio.{request.File.GetFileExtension}";
            serializedRequest.Content.Add(serializedRequest.Sc, "file", fileName);
        }
        else if (request.File?.File is not null)
        {
            serializedRequest.Sc = new StreamContent(request.File.File);
            serializedRequest.Sc.Headers.ContentLength = request.File.File.Length;
            serializedRequest.Sc.Headers.ContentType = new MediaTypeHeaderValue(request.File.GetContentType);

            string fileName = $"audio.{request.File.GetFileExtension}";
            serializedRequest.Content.Add(serializedRequest.Sc, "file", fileName);
        }
        
        // URL parameter for audio URL (Groq supports this as alternative to file)
        if (!request.Url.IsNullOrWhiteSpace())
        {
            serializedRequest.Content.Add(new StringContent(request.Url), "url");
        }
        
        serializedRequest.Content.Add(new StringContent(request.Model?.GetApiName), "model");

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

        // Use Zai-specific serialization for Z.AI provider
        MultipartFormDataContent content = provider.Provider == LLmProviders.Zai 
            ? VendorZaiAudioHandler.CreateStreamingRequest(request) 
            : SerializeRequest(request).Content;

        TornadoRequestContent requestBody = new TornadoRequestContent(content, request.Model, url, provider, CapabilityEndpoints.Audio);
        await using TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(provider, Endpoint, requestBody.Url, queryParams: null, HttpVerbs.Post, requestBody.Body, request.Model, request, request.CancellationToken);

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
        
        // Route to Zai handler for Z.AI provider
        if (provider.Provider == LLmProviders.Zai)
        {
            return await VendorZaiAudioHandler.CreateTranscription(request, provider, this, request.CancellationToken);
        }
        
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
