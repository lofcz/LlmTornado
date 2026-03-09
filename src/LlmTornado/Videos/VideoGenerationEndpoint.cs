using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Videos.Models;
using LlmTornado.Videos.Vendors.Google;
using LlmTornado.Videos.Vendors.OpenAi;
using LlmTornado.Videos.Vendors.XAi;
using LlmTornado.Videos.Vendors.MiniMax;
using LlmTornado.Videos.Vendors.Zai;

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
    ///     Creates a new video generation job.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created video job</returns>
    public async Task<HttpCallResult<VideoJob>> Create(VideoGenerationRequest request, LLmProviders? provider = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(provider ?? request.Model?.Provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await VendorOpenAiVideoHandler.Create(request, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.Google => await VendorGoogleVideoHandler.Create(request, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.XAi => await VendorXAiVideoHandler.Create(request, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.Zai => await VendorZaiVideoHandler.Create(request, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.MiniMax => await VendorMiniMaxVideoHandler.Create(request, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API is not supported for provider {resolvedProvider.Provider}")
        };
    }
    
    /// <summary>
    ///     Retrieves the status of a video job.
    /// </summary>
    /// <param name="videoId">The video job identifier (operation name for Google)</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="model">The model used for generation (needed for Google)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The video job with current status</returns>
    public async Task<HttpCallResult<VideoJob>> Get(string videoId, LLmProviders? provider = null, VideoModel? model = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(provider ?? model?.Provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await VendorOpenAiVideoHandler.Get(videoId, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.Google => await VendorGoogleVideoHandler.Get(videoId, resolvedProvider, this, model?.Name, cancellationToken).ConfigureAwait(false),
            LLmProviders.XAi => await VendorXAiVideoHandler.Get(videoId, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.Zai => await VendorZaiVideoHandler.Get(videoId, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            LLmProviders.MiniMax => await VendorMiniMaxVideoHandler.Get(videoId, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API is not supported for provider {resolvedProvider.Provider}")
        };
    }
    
    /// <summary>
    ///     Lists video jobs with optional pagination. Works with OpenAI only.
    /// </summary>
    /// <param name="query">Optional pagination query</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of video jobs</returns>
    public async Task<HttpCallResult<ListResponse<VideoJob>>> List(ListQuery? query = null, LLmProviders? provider = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await VendorOpenAiVideoHandler.List(query, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API List is not supported for provider {resolvedProvider.Provider}")
        };
    }
    
    /// <summary>
    ///     Deletes a video job. Works with OpenAI only.
    /// </summary>
    /// <param name="videoId">The video job identifier to delete</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deleted video job metadata</returns>
    public async Task<HttpCallResult<VideoJob>> Delete(string videoId, LLmProviders? provider = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await VendorOpenAiVideoHandler.Delete(videoId, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API Delete is not supported for provider {resolvedProvider.Provider}")
        };
    }
    
    /// <summary>
    ///     Downloads the video content.
    /// </summary>
    /// <param name="job">The video job to download content from</param>
    /// <param name="variant">Which asset to download (video, thumbnail, spritesheet). OpenAI only, ignored for Google.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream response containing the video/asset content</returns>
    public async Task<StreamResponse?> DownloadContent(VideoJob job, VideoContentVariant? variant = null, CancellationToken cancellationToken = default)
    {
        return job.SourceProvider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await DownloadContentOpenAi(job.Id, variant, cancellationToken).ConfigureAwait(false),
            LLmProviders.Google => await DownloadContentGoogle(job, cancellationToken).ConfigureAwait(false),
            LLmProviders.XAi => await DownloadContentXAi(job, cancellationToken).ConfigureAwait(false),
            LLmProviders.Zai => await DownloadContentZai(job, cancellationToken).ConfigureAwait(false),
            LLmProviders.MiniMax => await DownloadContentMiniMax(job, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API DownloadContent is not supported for provider {job.SourceProvider}")
        };
    }
    
    /// <summary>
    ///     Downloads the video content by ID. Works with OpenAI.
    /// </summary>
    /// <param name="videoId">The video job identifier</param>
    /// <param name="variant">Which asset to download (video, thumbnail, spritesheet). Defaults to video.</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream response containing the video/asset content</returns>
    public async Task<StreamResponse?> DownloadContent(string videoId, VideoContentVariant? variant = null, LLmProviders? provider = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await DownloadContentOpenAi(videoId, variant, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API DownloadContent by ID is not supported for provider {resolvedProvider.Provider}. Use DownloadContent(VideoJob) for Google.")
        };
    }
    
    private async Task<StreamResponse?> DownloadContentOpenAi(string videoId, VideoContentVariant? variant, CancellationToken cancellationToken)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.OpenAi);
        return await VendorOpenAiVideoHandler.GetContent(videoId, variant, resolvedProvider, this, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task<StreamResponse?> DownloadContentGoogle(VideoJob job, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(job.VideoUri))
        {
            return null;
        }
        
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.Google);
        return await VendorGoogleVideoHandler.GetContent(job.VideoUri, resolvedProvider, this, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task<StreamResponse?> DownloadContentXAi(VideoJob job, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(job.VideoUri))
        {
            return null;
        }
        
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.XAi);
        return await VendorXAiVideoHandler.GetContent(job.VideoUri, resolvedProvider, this, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task<StreamResponse?> DownloadContentZai(VideoJob job, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(job.VideoUri))
        {
            return null;
        }
        
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.Zai);
        return await VendorZaiVideoHandler.GetContent(job.VideoUri, resolvedProvider, this, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task<StreamResponse?> DownloadContentMiniMax(VideoJob job, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(job.VideoUri))
        {
            return null;
        }
        
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.MiniMax);
        return await VendorMiniMaxVideoHandler.GetContent(job.VideoUri, resolvedProvider, this, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Creates a remix of a completed video. Works with OpenAI only.
    /// </summary>
    /// <param name="videoId">The video job identifier to remix</param>
    /// <param name="prompt">Updated text prompt for the remix</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new remix video job</returns>
    public async Task<HttpCallResult<VideoJob>> Remix(string videoId, string prompt, LLmProviders? provider = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.OpenAi or LLmProviders.Custom => await VendorOpenAiVideoHandler.Remix(videoId, prompt, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API Remix is not supported for provider {resolvedProvider.Provider}")
        };
    }
    
    /// <summary>
    ///     Edits an existing video based on a prompt. Works with xAI only.
    ///     Note: The input video URL must be a direct, publicly accessible link. Maximum supported video length is 8.7 seconds.
    /// </summary>
    /// <param name="prompt">Prompt describing the desired changes</param>
    /// <param name="videoUrl">URL of the video to edit (public URL or base64-encoded data URL)</param>
    /// <param name="request">Optional request with model and extension settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The video edit job</returns>
    public async Task<HttpCallResult<VideoJob>> Edit(string prompt, string videoUrl, VideoGenerationRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new VideoGenerationRequest();
        request.Model ??= VideoModel.XAi.Grok.ImagineVideo;
        
        IEndpointProvider resolvedProvider = Api.ResolveProvider(request.Model.Provider);
        
        return resolvedProvider.Provider switch
        {
            LLmProviders.XAi => await VendorXAiVideoHandler.Edit(prompt, videoUrl, request, resolvedProvider, this, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Video API Edit is not supported for provider {resolvedProvider.Provider}")
        };
    }
    
    /// <summary>
    ///     Edits an existing video and waits for completion. Works with xAI only.
    /// </summary>
    /// <param name="prompt">Prompt describing the desired changes</param>
    /// <param name="videoUrl">URL of the video to edit</param>
    /// <param name="request">Optional request with model and extension settings</param>
    /// <param name="pollingIntervalMs">Interval between polls in milliseconds</param>
    /// <param name="maxWaitMs">Maximum wait time in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The completed video edit job</returns>
    public async Task<HttpCallResult<VideoJob>> EditAndWait(
        string prompt,
        string videoUrl,
        VideoGenerationRequest? request = null,
        int pollingIntervalMs = 10000,
        int maxWaitMs = 86400000,
        CancellationToken cancellationToken = default)
    {
        HttpCallResult<VideoJob> editResult = await Edit(prompt, videoUrl, request, cancellationToken).ConfigureAwait(false);
        
        if (!editResult.Ok || editResult.Data is null)
        {
            return editResult;
        }
        
        return await WaitForCompletion(editResult.Data.Id, pollingIntervalMs, maxWaitMs, LLmProviders.XAi, request?.Model, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Polls a video job until it completes or reaches a terminal state.
    /// </summary>
    /// <param name="videoId">The video job identifier (operation name for Google)</param>
    /// <param name="pollingIntervalMs">Interval between polls in milliseconds</param>
    /// <param name="maxWaitMs">Maximum wait time in milliseconds</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="model">The model used for generation (needed for Google)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The completed video job</returns>
    public async Task<HttpCallResult<VideoJob>> WaitForCompletion(
        string videoId,
        int pollingIntervalMs = 10000,
        int maxWaitMs = 86400000,
        LLmProviders? provider = null,
        VideoModel? model = null,
        CancellationToken cancellationToken = default)
    {
        DateTime startTime = DateTime.UtcNow;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpCallResult<VideoJob> result = await Get(videoId, provider, model, cancellationToken).ConfigureAwait(false);
            
            if (!result.Ok || result.Data is null)
            {
                return result;
            }
            
            VideoJobStatus status = result.Data.Status;
            
            if (status is VideoJobStatus.Completed or VideoJobStatus.Failed || (DateTime.UtcNow - startTime).TotalMilliseconds > maxWaitMs)
            {
                return result;
            }
            
            await Task.Delay(pollingIntervalMs, cancellationToken).ConfigureAwait(false);
        }
        
        return await Get(videoId, provider, model, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Creates a video and waits for completion.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <param name="pollingIntervalMs">Interval between polls in milliseconds</param>
    /// <param name="maxWaitMs">Maximum wait time in milliseconds</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The completed video job</returns>
    public async Task<HttpCallResult<VideoJob>> CreateAndWait(
        VideoGenerationRequest request,
        int pollingIntervalMs = 10000,
        int maxWaitMs = 86400000,
        LLmProviders? provider = null,
        CancellationToken cancellationToken = default)
    {
        HttpCallResult<VideoJob> createResult = await Create(request, provider, cancellationToken).ConfigureAwait(false);
        
        if (!createResult.Ok || createResult.Data is null)
        {
            return createResult;
        }
        
        LLmProviders jobProvider = createResult.Data.SourceProvider;
        string jobId = jobProvider == LLmProviders.Google ? (createResult.Data.OperationName ?? createResult.Data.Id) : createResult.Data.Id;
        
        return await WaitForCompletion(jobId, pollingIntervalMs, maxWaitMs, jobProvider, request.Model, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Creates a video and waits for completion with event callbacks.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <param name="events">Event handlers for progress and completion</param>
    /// <param name="pollingIntervalMs">Interval between polls in milliseconds</param>
    /// <param name="maxWaitMs">Maximum wait time in milliseconds</param>
    /// <param name="provider">Optional provider override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The completed video job</returns>
    public async Task<HttpCallResult<VideoJob>> CreateAndWait(
        VideoGenerationRequest request,
        VideoJobEvents events,
        int pollingIntervalMs = 10000,
        int maxWaitMs = 86400000,
        LLmProviders? provider = null,
        CancellationToken cancellationToken = default)
    {
        HttpCallResult<VideoJob> createResult = await Create(request, provider, cancellationToken).ConfigureAwait(false);
        
        if (!createResult.Ok || createResult.Data is null)
        {
            return createResult;
        }
        
        LLmProviders jobProvider = createResult.Data.SourceProvider;
        string jobId = jobProvider == LLmProviders.Google ? (createResult.Data.OperationName ?? createResult.Data.Id) : createResult.Data.Id;
        
        return await WaitForCompletionWithEvents(jobId, request.Model, events, pollingIntervalMs, maxWaitMs, jobProvider, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task<HttpCallResult<VideoJob>> WaitForCompletionWithEvents(
        string videoId,
        VideoModel? model,
        VideoJobEvents events,
        int pollingIntervalMs,
        int maxWaitMs,
        LLmProviders? provider,
        CancellationToken cancellationToken)
    {
        DateTime startTime = DateTime.UtcNow;
        Stopwatch stopwatch = Stopwatch.StartNew();
        int pollIndex = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpCallResult<VideoJob> result = await Get(videoId, provider, model, cancellationToken).ConfigureAwait(false);
            
            if (!result.Ok || result.Data is null)
            {
                return result;
            }
            
            // Call OnPoll event
            if (events.OnPoll is not null)
            {
                await events.OnPoll(result.Data, pollIndex, stopwatch.Elapsed);
            }
            
            VideoJobStatus status = result.Data.Status;
            
            if (status is VideoJobStatus.Completed or VideoJobStatus.Failed || (DateTime.UtcNow - startTime).TotalMilliseconds > maxWaitMs)
            {
                // If completed successfully, download and call OnFinished
                if (status == VideoJobStatus.Completed && events.OnFinished is not null)
                {
                    StreamResponse? stream = await DownloadContent(result.Data, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (stream is not null)
                    {
                        VideoStream videoStream = new VideoStream(stream.Stream);
                        try
                        {
                            await events.OnFinished(result.Data, videoStream);
                        }
                        finally
                        {
                            await videoStream.DisposeAsync();
                        }
                    }
                }
                
                return result;
            }
            
            pollIndex++;
            await Task.Delay(pollingIntervalMs, cancellationToken).ConfigureAwait(false);
        }
        
        return await Get(videoId, provider, model, cancellationToken).ConfigureAwait(false);
    }
}
