using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Videos.Vendors.OpenAi;

/// <summary>
/// Handles all video operations for OpenAI.
/// </summary>
internal static class VendorOpenAiVideoHandler
{
    /// <summary>
    /// Creates a new video generation job.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Create(
        VideoGenerationRequest request, 
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        object postData = VendorOpenAiVideoRequest.RequiresJsonContent(request)
            ? VendorOpenAiVideoRequest.SerializeJson(request)
            : VendorOpenAiVideoRequest.SerializeMultipart(request);
        
        HttpCallResult<VideoJob> result = await endpoint.HttpPost<VideoJob>(
            provider, 
            CapabilityEndpoints.Videos, 
            postData: postData,
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        SetSourceProvider(result);
        return result;
    }

    /// <summary>
    /// Creates a character from an uploaded video clip.
    /// </summary>
    public static async Task<HttpCallResult<VideoCharacter>> CreateCharacter(
        string name,
        byte[] videoBytes,
        string mimeType,
        IEndpointProvider provider,
        EndpointBase endpoint,
        CancellationToken cancellationToken)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");

        ByteArrayContent videoContent = new ByteArrayContent(videoBytes);
        videoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        content.Add(videoContent, "video", "character.mp4");

        string url = endpoint.GetUrl(provider, "/characters");

        HttpCallResult<VideoCharacter> result = await endpoint.HttpPost<VideoCharacter>(
            provider,
            CapabilityEndpoints.Videos,
            url,
            postData: content,
            ct: cancellationToken
        ).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Retrieves a character by ID.
    /// </summary>
    public static async Task<HttpCallResult<VideoCharacter>> GetCharacter(
        string characterId,
        IEndpointProvider provider,
        EndpointBase endpoint,
        CancellationToken cancellationToken)
    {
        string url = endpoint.GetUrl(provider, $"/characters/{characterId}");

        return await endpoint.HttpGet<VideoCharacter>(
            provider,
            CapabilityEndpoints.Videos,
            url,
            ct: cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Extends a completed video with a new segment.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Extend(
        VideoExtensionRequest request,
        IEndpointProvider provider,
        EndpointBase endpoint,
        CancellationToken cancellationToken)
    {
        string url = endpoint.GetUrl(provider, "/extensions");
        object requestBody = VendorOpenAiVideoRequest.SerializeExtension(request);

        HttpCallResult<VideoJob> result = await endpoint.HttpPost<VideoJob>(
            provider,
            CapabilityEndpoints.Videos,
            url,
            postData: requestBody,
            ct: cancellationToken
        ).ConfigureAwait(false);

        SetSourceProvider(result);
        return result;
    }

    /// <summary>
    /// Edits an existing video via POST /v1/videos/edits.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Edit(
        VideoEditRequest request,
        IEndpointProvider provider,
        EndpointBase endpoint,
        CancellationToken cancellationToken)
    {
        string url = endpoint.GetUrl(provider, "/edits");
        object postData = request.VideoBytes is not null
            ? VendorOpenAiVideoRequest.SerializeEditMultipart(request)
            : VendorOpenAiVideoRequest.SerializeEditJson(request);

        HttpCallResult<VideoJob> result = await endpoint.HttpPost<VideoJob>(
            provider,
            CapabilityEndpoints.Videos,
            url,
            postData: postData,
            ct: cancellationToken
        ).ConfigureAwait(false);

        SetSourceProvider(result);
        return result;
    }
    
    /// <summary>
    /// Retrieves the status of a video job.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Get(
        string videoId, 
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        string url = endpoint.GetUrl(provider, $"/{videoId}");
        
        HttpCallResult<VideoJob> result = await endpoint.HttpGet<VideoJob>(
            provider, 
            CapabilityEndpoints.Videos, 
            url,
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        SetSourceProvider(result);
        return result;
    }
    
    /// <summary>
    /// Lists videos with optional pagination.
    /// </summary>
    public static async Task<HttpCallResult<ListResponse<VideoJob>>> List(
        ListQuery? query,
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        string url = endpoint.GetUrl(provider);
        
        HttpCallResult<ListResponse<VideoJob>> result = await endpoint.HttpGet<ListResponse<VideoJob>>(
            provider, 
            CapabilityEndpoints.Videos,
            url,
            queryParams: query?.ToQueryParams(provider),
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        if (result.Data?.Items is not null)
        {
            foreach (VideoJob job in result.Data.Items)
            {
                job.SourceProvider = LLmProviders.OpenAi;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Deletes a video job.
    /// </summary>
    public static async Task<HttpCallResult<VideoJob>> Delete(
        string videoId,
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        string url = endpoint.GetUrl(provider, $"/{videoId}");
        
        HttpCallResult<VideoJob> result = await endpoint.HttpDelete<VideoJob>(
            provider, 
            CapabilityEndpoints.Videos, 
            url,
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        SetSourceProvider(result);
        return result;
    }
    
    /// <summary>
    /// Downloads video content.
    /// </summary>
    public static async Task<StreamResponse?> GetContent(
        string videoId,
        VideoContentVariant? variant,
        IEndpointProvider provider, 
        EndpointBase endpoint, 
        CancellationToken cancellationToken)
    {
        Dictionary<string, object>? queryParams = null;
        
        if (variant is not null && variant != VideoContentVariant.Video)
        {
            queryParams = new Dictionary<string, object>
            {
                ["variant"] = variant.Value.ToString().ToLowerInvariant()
            };
        }
        
        string url = endpoint.GetUrl(provider, $"/{videoId}/content");
        
        return await endpoint.HttpGetStream(
            provider, 
            CapabilityEndpoints.Videos, 
            url,
            queryParams: queryParams,
            ct: cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a remix of a completed video.
    /// </summary>
    [Obsolete("POST /v1/videos/{video_id}/remix is deprecated. Use Edit instead.")]
    public static Task<HttpCallResult<VideoJob>> Remix(
        string videoId,
        string prompt,
        IEndpointProvider provider,
        EndpointBase endpoint,
        CancellationToken cancellationToken)
    {
        return Edit(new VideoEditRequest
        {
            VideoId = videoId,
            Prompt = prompt
        }, provider, endpoint, cancellationToken);
    }
    
    private static void SetSourceProvider(HttpCallResult<VideoJob> result)
    {
        if (result.Data is not null)
        {
            result.Data.SourceProvider = LLmProviders.OpenAi;
        }
    }
}
