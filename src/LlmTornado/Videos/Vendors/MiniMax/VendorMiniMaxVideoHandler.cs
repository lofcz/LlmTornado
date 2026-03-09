using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Videos.Vendors.MiniMax;

/// <summary>
/// Handles all video operations for MiniMax.
/// </summary>
internal static class VendorMiniMaxVideoHandler
{
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };
    
    /// <summary>
    /// Creates a new video generation job.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Create(
        VideoGenerationRequest request, 
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        VendorMiniMaxVideoGenerationRequest miniMaxRequest = VendorMiniMaxVideoGenerationRequest.FromRequest(request);
        string json = JsonConvert.SerializeObject(miniMaxRequest, SerializerSettings);
        
        // MiniMax uses /v1/video_generation endpoint
        string url = provider.ApiUrl(CapabilityEndpoints.Videos, null);
        
        HttpCallResult<VendorMiniMaxVideoCreateResponse> result = await endpoint.HttpPost<VendorMiniMaxVideoCreateResponse>(
            provider, 
            CapabilityEndpoints.Videos, 
            url,
            postData: json,
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        if (!result.Ok || result.Data is null)
        {
            return new HttpCallResult<VideoJob>(result.Code, result.Response, null, false, result.Request)
            {
                Exception = result.Exception
            };
        }
        
        // Check MiniMax base_resp for errors
        if (result.Data.BaseResp is { StatusCode: not 0 })
        {
            return new HttpCallResult<VideoJob>(result.Code, result.Response, null, false, result.Request)
            {
                Exception = new Exception($"MiniMax error {result.Data.BaseResp.StatusCode}: {result.Data.BaseResp.StatusMsg}")
            };
        }
        
        // Convert to harmonized VideoJob
        VideoJob job = new VideoJob
        {
            Id = result.Data.TaskId,
            Status = VideoJobStatus.Queued,
            Model = request.Model?.Name,
            Prompt = request.Prompt,
            SourceProvider = LLmProviders.MiniMax
        };
        
        return new HttpCallResult<VideoJob>(result.Code, result.Response, job, true, result.Request);
    }
    
    /// <summary>
    /// Retrieves the status of a video job.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Get(
        string taskId, 
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        // MiniMax uses GET /v1/query/video_generation?task_id=X
        // Build the query URL by replacing the endpoint fragment
        string baseUrl = provider.ApiUrl(CapabilityEndpoints.Videos, null);
        string queryUrl = baseUrl.Replace("/video_generation", "/query/video_generation");
        
        HttpCallResult<VendorMiniMaxVideoQueryResponse> result = await endpoint.HttpGet<VendorMiniMaxVideoQueryResponse>(
            provider, 
            CapabilityEndpoints.None,
            queryUrl,
            queryParams: new Dictionary<string, object> { { "task_id", taskId } },
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        if (!result.Ok || result.Data is null)
        {
            return new HttpCallResult<VideoJob>(result.Code, result.Response, null, false, result.Request)
            {
                Exception = result.Exception
            };
        }
        
        // Convert to harmonized VideoJob
        VideoJob job = new VideoJob
        {
            Id = taskId,
            SourceProvider = LLmProviders.MiniMax
        };
        
        // Map MiniMax status to harmonized VideoJobStatus
        job.Status = MapTaskStatus(result.Data.Status);
        
        // Store file_id as part of the video URI for later retrieval via File API
        if (!string.IsNullOrEmpty(result.Data.FileId))
        {
            job.VideoUri = result.Data.FileId;
        }
        
        // Set video dimensions as size string
        if (result.Data.VideoWidth.HasValue && result.Data.VideoHeight.HasValue)
        {
            job.Size = $"{result.Data.VideoWidth}x{result.Data.VideoHeight}";
        }
        
        return new HttpCallResult<VideoJob>(result.Code, result.Response, job, true, result.Request);
    }
    
    /// <summary>
    /// Downloads video content. MiniMax uses a two-step process:
    /// 1. Call GET /v1/files/retrieve?file_id=X to get a JSON response with download_url
    /// 2. Stream the actual video content from that download_url
    /// </summary>
    public static async Task<StreamResponse?> GetContent(
        string fileId,
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(fileId))
        {
            return null;
        }
        
        // Step 1: Call /v1/files/retrieve?file_id=X to get the download URL
        string baseUrl = provider.ApiUrl(CapabilityEndpoints.Videos, null);
        string fileUrl = baseUrl.Replace("/video_generation", "/files/retrieve");
        
        HttpCallResult<VendorMiniMaxFileRetrieveResponse> result = await endpoint.HttpGet<VendorMiniMaxFileRetrieveResponse>(
            provider,
            CapabilityEndpoints.None,
            fileUrl,
            queryParams: new Dictionary<string, object> { { "file_id", fileId } },
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        if (!result.Ok || result.Data?.File?.DownloadUrl is null)
        {
            return null;
        }
        
        // Step 2: Download the actual video from the CDN URL (no provider auth needed)
        return await endpoint.HttpGetRawStream(provider, result.Data.File.DownloadUrl, ct: cancellationToken).ConfigureAwait(false);
    }
    
    private static VideoJobStatus MapTaskStatus(string? status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return VideoJobStatus.Unknown;
        }
        
        return status switch
        {
            "Preparing" => VideoJobStatus.Queued,
            "Queueing" => VideoJobStatus.Queued,
            "Processing" => VideoJobStatus.InProgress,
            "Success" => VideoJobStatus.Completed,
            "Fail" => VideoJobStatus.Failed,
            _ => VideoJobStatus.Unknown
        };
    }
}
