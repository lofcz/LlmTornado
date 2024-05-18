using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado;

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
    public Task<TranscriptionVerboseJsonResult?> CreateTranscriptionAsync(TranscriptionRequest request)
    {
        return PostAudioAsync($"/transcriptions", request);
    }

    /// <summary>
    ///     Translates audio into English.
    /// </summary>
    public Task<TranscriptionVerboseJsonResult?> CreateTranslationAsync(TranslationRequest request)
    {
        return PostAudioAsync($"/translations", new TranscriptionRequest
            {
                File = request.File,
                Model = request.Model,
                Prompt = request.Prompt,
                ResponseFormat = request.ResponseFormat,
                Temperature = request.Temperature
            }
        );
    }

    /// <summary>
    ///     Converts string text into speech (tts)
    /// </summary>
    public Task<SpeechTtsResult?> CreateSpeechAsync(SpeechRequest request)
    {
        return PostSpeechAsync(request);
    }

    private async Task<SpeechTtsResult?> PostSpeechAsync(SpeechRequest request)
    {
        StreamResponse? x = await HttpPostStream(Api.GetProvider(LLmProviders.OpenAi), Endpoint, $"/speech", request);
        return x is null ? null : new SpeechTtsResult(x);
    }

    private Task<TranscriptionVerboseJsonResult?> PostAudioAsync(string url, TranscriptionRequest request)
    {
        MultipartFormDataContent content = new();
        StreamContent fileContent = new(request.File.File);
        fileContent.Headers.ContentLength = request.File.ContentLength;
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);
        content.Add(fileContent, "file", request.File.Name);
        content.Add(new StringContent(request.Model), "model");

        if (!request.Prompt.IsNullOrWhiteSpace()) content.Add(new StringContent(request.Prompt), "prompt");

        if (!request.ResponseFormat.IsNullOrWhiteSpace()) content.Add(new StringContent(request.ResponseFormat), "response_format");

        if (!request.Temperature.HasValue) content.Add(new StringContent((request.Temperature ?? 0f).ToString(CultureInfo.InvariantCulture)), "temperature");

        if (!request.Language.IsNullOrWhiteSpace()) content.Add(new StringContent(request.Language), "language");

        return HttpPost1<TranscriptionVerboseJsonResult>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, url, content);
    }
}