using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Videos.Models;

namespace LlmTornado.Videos;

/// <summary>
///     Given a prompt, the model will generate a new video.
/// </summary>
public class VideoGenerationEndpoint : EndpointBase
{
    /// <summary>
    ///     Constructor of the api endpoint. Rather than instantiating this yourself, access it through an instance of
    ///     <see cref="TornadoApi" /> as <see cref="TornadoApi.Videos" />.
    /// </summary>
    /// <param name="api"></param>
    internal VideoGenerationEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "videos".
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Videos;
    
    /// <summary>
    ///     Ask the API to create a video given a prompt.
    /// </summary>
    /// <param name="input">A text description of the desired video(s)</param>
    /// <returns>Asynchronously returns the video result. Look in its <see cref="VideoGenerationResult"/></returns>
    public Task<VideoGenerationResult?> CreateVideo(string input)
    {
        VideoGenerationRequest req = new VideoGenerationRequest(input);
        return CreateVideo(req);
    }

    /// <summary>
    ///     Ask the API to create a video given a prompt.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <returns>Asynchronously returns the video result. Look in its <see cref="VideoGenerationResult"/></returns>
    public Task<VideoGenerationResult?> CreateVideo(VideoGenerationRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model ?? VideoModel.Google.Veo.V31Fast);
        TornadoRequestContent requestBody = request.Serialize(provider);
        
        return HttpPost1<VideoGenerationResult>(provider, Endpoint, requestBody.Url, postData: requestBody.Body);
    }

    /// <summary>
    ///     Get the status of a video generation operation.
    /// </summary>
    /// <param name="operationName">The name of the operation returned from CreateVideo</param>
    /// <param name="model">The model used for the operation</param>
    /// <returns>Asynchronously returns the video result with updated status</returns>
    public async Task<VideoGenerationResult?> GetVideoStatus(string operationName, VideoModel? model = null, CancellationToken? ct = null)
    {
        IEndpointProvider provider = Api.GetProvider(model ?? VideoModel.Google.Veo.V31Fast);
        
        // The operation name is already a complete path like "models/veo-3.1-generate-preview/operations/xyz"
        // We need to append it directly to the base URL without going through the Videos endpoint fragment
        string url = provider.ApiUrl(CapabilityEndpoints.BaseUrl, operationName);
        
        return (await HttpGet<VideoGenerationResult>(provider, CapabilityEndpoints.Videos, url, ct: ct)).Data;
    }
    
    /// <summary>
    ///     Poll for the completion of a video generation operation.
    /// </summary>
    /// <param name="operationName">The name of the operation returned from CreateVideo</param>
    /// <param name="model">The model used for the operation</param>
    /// <param name="pollingInterval">The interval in seconds between polls</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronously returns the completed video result</returns>
    public async Task<VideoGenerationResult?> WaitForVideoCompletion(
        string operationName, 
        VideoModel? model = null, 
        int pollingInterval = 10, 
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            VideoGenerationResult? result = await GetVideoStatus(operationName, model);
            
            if (result is null)
            {
                throw new InvalidOperationException("Failed to get video generation status");
            }
            
            if (result.Done)
            {
                return result;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(pollingInterval), cancellationToken);
        }
        
        throw new OperationCanceledException("Video generation was canceled");
    }
    
    /// <summary>
    ///     Create a video and wait for its completion.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <param name="pollingInterval">The interval in seconds between polls</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronously returns the completed video result</returns>
    public async Task<VideoGenerationResult?> CreateVideoAndWait(
        VideoGenerationRequest request, 
        int pollingInterval = 10, 
        CancellationToken cancellationToken = default)
    {
        VideoGenerationResult? initialResult = await CreateVideo(request);
        
        if (initialResult is null)
        {
            throw new InvalidOperationException("Failed to start video generation");
        }
        
        if (string.IsNullOrEmpty(initialResult.Name))
        {
            throw new InvalidOperationException("No operation name returned from video generation");
        }
        
        return await WaitForVideoCompletion(initialResult.Name, request.Model, pollingInterval, cancellationToken);
    }
    
    /// <summary>
    ///     Create a video and wait for its completion.
    /// </summary>
    /// <param name="input">A text description of the desired video(s)</param>
    /// <param name="model">The model to use</param>
    /// <param name="pollingInterval">The interval in seconds between polls</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronously returns the completed video result</returns>
    public async Task<VideoGenerationResult?> CreateVideoAndWait(
        string input, 
        VideoModel? model = null, 
        int pollingInterval = 10, 
        CancellationToken cancellationToken = default)
    {
        VideoGenerationRequest request = new VideoGenerationRequest(input) { Model = model };
        return await CreateVideoAndWait(request, pollingInterval, cancellationToken);
    }
    
    /// <summary>
    ///     Create a video and wait for its completion with event callbacks.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <param name="events">Event handlers for progress and completion</param>
    /// <param name="pollingInterval">The interval in seconds between polls</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronously returns the completed video result</returns>
    public async Task<VideoGenerationResult?> CreateVideoAndWait(
        VideoGenerationRequest request,
        VideoGenerationEvents events,
        int pollingInterval = 10,
        CancellationToken cancellationToken = default)
    {
        VideoGenerationResult? initialResult = await CreateVideo(request);
        
        if (initialResult is null)
        {
            throw new InvalidOperationException("Failed to start video generation");
        }
        
        if (string.IsNullOrEmpty(initialResult.Name))
        {
            throw new InvalidOperationException("No operation name returned from video generation");
        }
        
        return await WaitForVideoCompletion(initialResult.Name, request.Model, events, pollingInterval, cancellationToken);
    }
    
    /// <summary>
    ///     Poll for the completion of a video generation operation with event callbacks.
    /// </summary>
    /// <param name="operationName">The name of the operation returned from CreateVideo</param>
    /// <param name="model">The model used for the operation</param>
    /// <param name="events">Event handlers for progress and completion</param>
    /// <param name="pollingInterval">The interval in seconds between polls</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronously returns the completed video result</returns>
    public async Task<VideoGenerationResult?> WaitForVideoCompletion(
        string operationName,
        VideoModel? model,
        VideoGenerationEvents events,
        int pollingInterval = 10,
        CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        int pollIndex = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            VideoGenerationResult? result = await GetVideoStatus(operationName, model);
            
            if (result is null)
            {
                throw new InvalidOperationException("Failed to get video generation status");
            }
            
            // Call OnPoll event
            if (events.OnPoll is not null)
            {
                await events.OnPoll(result, pollIndex, stopwatch.Elapsed);
            }
            
            if (result.Done)
            {
                // Video is complete, download it
                await DownloadAndProcessVideo(result, events, model);
                return result;
            }
            
            pollIndex++;
            await Task.Delay(TimeSpan.FromSeconds(pollingInterval), cancellationToken);
        }
        
        throw new OperationCanceledException("Video generation was canceled");
    }
    
    /// <summary>
    ///     Download video content from the generated video URI.
    /// </summary>
    /// <param name="videoUri">The URI of the video to download</param>
    /// <param name="model">The model used for generation</param>
    /// <returns>Stream containing the video content</returns>
    public async Task<Stream> GetVideoContent(string videoUri, VideoModel? model = null)
    {
        IEndpointProvider provider = Api.GetProvider(model ?? VideoModel.Google.Veo.V31Fast);
        
        // For Google, handle the download URL
        if (provider is GoogleEndpointProvider)
        {
            string downloadUrl;
            
            // Check if URI already contains :download (it's already a download URL)
            if (videoUri.Contains(":download"))
            {
                downloadUrl = videoUri;
            }
            else
            {
                // Extract file ID from URI like: https://generativelanguage.googleapis.com/v1beta/files/{fileId}
                string uriWithoutQuery = videoUri.Split('?')[0];
                string fileId = uriWithoutQuery.Split('/').Last();
                downloadUrl = $"https://generativelanguage.googleapis.com/download/v1beta/files/{fileId}:download?alt=media";
            }
            
            ProviderAuthentication? auth = Api.GetProvider(LLmProviders.Google).Auth;
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            
            // Add API key as header
            if (auth?.ApiKey is not null)
            {
                request.Headers.Add("x-goog-api-key", auth.ApiKey.Trim());
            }
            
            request.Headers.Add("User-Agent", GetUserAgent());
            
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStreamAsync();
        }
        
        throw new NotSupportedException($"Video download not yet implemented for provider {provider.Provider}");
    }
    
    /// <summary>
    ///     Download and process the video with events.
    /// </summary>
    private async Task DownloadAndProcessVideo(VideoGenerationResult result, VideoGenerationEvents events, VideoModel? model)
    {
        // Extract video URI from result
        string? videoUri = result.Response?.GenerateVideoResponse?.GeneratedSamples?.FirstOrDefault()?.Video?.Uri;
        
        if (string.IsNullOrEmpty(videoUri))
        {
            throw new InvalidOperationException("No video URI found in the completed result");
        }
        
        // Download the video
        Stream innerStream = await GetVideoContent(videoUri, model);
        
        // Wrap in VideoStream for disposal management
        VideoStream videoStream = new VideoStream(innerStream);
        
        try
        {
            // Call OnFinished event with VideoStream
            if (events.OnFinished is not null)
            {
                await events.OnFinished(result, videoStream);
            }
        }
        finally
        {
            await videoStream.DisposeAsync();
        }
    }
}