using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Common;

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
        StreamResponse? x = await HttpPostStream(Api.GetProvider(LLmProviders.OpenAi), Endpoint, $"/speech", request);
        return x is null ? null : new SpeechTtsResult(x);
    }

    private async Task<TranscriptionResult?> PostAudio(string url, TranscriptionRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model);
        url = provider.ApiUrl(CapabilityEndpoints.Audio, url);
   
        MultipartFormDataContent content = new MultipartFormDataContent();
        MemoryStream? ms = null;
        StreamContent? sc = null;

        if (request.File.Data is not null)
        {
            ms = new MemoryStream(request.File.Data);
            sc = new StreamContent(ms);
            
            sc.Headers.ContentLength = request.File.Data.Length;
            sc.Headers.ContentType = new MediaTypeHeaderValue(request.File.GetContentType);
        
            content.Add(sc, "file", "test.wav");
        }
        else if (request.File.File is not null)
        {
            sc = new StreamContent(request.File.File);
            sc.Headers.ContentLength = request.File.File.Length;
            sc.Headers.ContentType = new MediaTypeHeaderValue(request.File.GetContentType);
        
            content.Add(sc, "file", "test.wav");
        }
        
        content.Add(new StringContent(request.Model.GetApiName), "model");

        if (!request.Prompt.IsNullOrWhiteSpace())
        {
            content.Add(new StringContent(request.Prompt), "prompt");
        }
        
        content.Add(new StringContent(request.GetResponseFormat), "response_format");

        if (!request.Temperature.HasValue)
        {
            content.Add(new StringContent(0f.ToString(CultureInfo.InvariantCulture)), "temperature");
        }

        if (!request.Language.IsNullOrWhiteSpace())
        {
            content.Add(new StringContent(request.Language), "language");
        }

        TranscriptionResult? result;

        try
        {
            if (request.ResponseFormat is AudioTranscriptionResponseFormats.Text or AudioTranscriptionResponseFormats.Srt or AudioTranscriptionResponseFormats.Vtt)
            {
                object? obj = await HttpPost1(typeof(string), provider, Endpoint, url, content);

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
            
            result = await HttpPost1<TranscriptionResult>(provider, Endpoint, url, content);
        }
        finally
        {
            content.Dispose();
            sc?.Dispose();

            if (ms is not null)
            {
                await ms.DisposeAsync();   
            }
        }

        return result;
    }
}