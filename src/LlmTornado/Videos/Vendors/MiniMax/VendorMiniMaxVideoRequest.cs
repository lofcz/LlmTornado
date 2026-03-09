using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using LlmTornado.Videos.Models;
using LlmTornado.Videos.Models.MiniMax;
using Newtonsoft.Json;

namespace LlmTornado.Videos.Vendors.MiniMax;

/// <summary>
/// MiniMax video generation request DTO.
/// </summary>
internal class VendorMiniMaxVideoGenerationRequest
{
    /// <summary>
    /// Model name.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Text description of the video, up to 2000 characters.
    /// </summary>
    [JsonProperty("prompt", NullValueHandling = NullValueHandling.Ignore)]
    public string? Prompt { get; set; }
    
    /// <summary>
    /// Image as the starting frame of the video. URL or base64-encoded data URL.
    /// </summary>
    [JsonProperty("first_frame_image", NullValueHandling = NullValueHandling.Ignore)]
    public string? FirstFrameImage { get; set; }
    
    /// <summary>
    /// Whether to automatically optimize the prompt. Defaults to true.
    /// </summary>
    [JsonProperty("prompt_optimizer", NullValueHandling = NullValueHandling.Ignore)]
    public bool? PromptOptimizer { get; set; }
    
    /// <summary>
    /// Reduces optimization time when prompt_optimizer is enabled.
    /// </summary>
    [JsonProperty("fast_pretreatment", NullValueHandling = NullValueHandling.Ignore)]
    public bool? FastPretreatment { get; set; }
    
    /// <summary>
    /// Video length in seconds. Default is 6.
    /// </summary>
    [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
    public int? Duration { get; set; }
    
    /// <summary>
    /// Video resolution: 512P, 720P, 768P, or 1080P.
    /// </summary>
    [JsonProperty("resolution", NullValueHandling = NullValueHandling.Ignore)]
    public string? Resolution { get; set; }
    
    /// <summary>
    /// Callback URL for asynchronous task status updates.
    /// </summary>
    [JsonProperty("callback_url", NullValueHandling = NullValueHandling.Ignore)]
    public string? CallbackUrl { get; set; }
    
    /// <summary>
    /// Creates a MiniMax video generation request from a generic VideoGenerationRequest.
    /// </summary>
    public static VendorMiniMaxVideoGenerationRequest FromRequest(VideoGenerationRequest request)
    {
        VendorMiniMaxVideoGenerationRequest miniMaxRequest = new VendorMiniMaxVideoGenerationRequest
        {
            Model = request.Model?.Name ?? VideoModelMiniMaxHailuo.ModelHailuo23.Name,
            Prompt = request.Prompt
        };
        
        // Handle image input for image-to-video
        if (request.Image is not null)
        {
            miniMaxRequest.FirstFrameImage = request.Image.Url;
        }
        
        // Duration
        if (request.DurationSeconds.HasValue)
        {
            miniMaxRequest.Duration = request.DurationSeconds.Value;
        }
        else if (request.Duration.HasValue && request.Duration.Value != VideoDuration.Custom)
        {
            miniMaxRequest.Duration = (int)request.Duration.Value;
        }
        
        // Resolution - map from harmonized enum if set, otherwise use MiniMax extensions
        if (request.Resolution.HasValue)
        {
            miniMaxRequest.Resolution = request.Resolution.Value switch
            {
                VideoResolution.SD => "720P",
                VideoResolution.HD => "720P",
                VideoResolution.FullHD => "1080P",
                _ => null
            };
        }
        
        // MiniMax-specific extensions
        if (request.MiniMaxExtensions is not null)
        {
            VideoMiniMaxExtensions ext = request.MiniMaxExtensions;
            
            if (ext.Resolution.HasValue)
            {
                miniMaxRequest.Resolution = GetEnumMemberValue(ext.Resolution.Value);
            }
            
            if (ext.PromptOptimizer.HasValue)
            {
                miniMaxRequest.PromptOptimizer = ext.PromptOptimizer.Value;
            }
            
            if (ext.FastPretreatment.HasValue)
            {
                miniMaxRequest.FastPretreatment = ext.FastPretreatment.Value;
            }
            
            if (!string.IsNullOrEmpty(ext.CallbackUrl))
            {
                miniMaxRequest.CallbackUrl = ext.CallbackUrl;
            }
        }
        
        return miniMaxRequest;
    }
    
    private static string? GetEnumMemberValue<T>(T enumValue) where T : Enum
    {
        FieldInfo? memberInfo = typeof(T).GetField(enumValue.ToString());
        object[]? attributes = memberInfo?.GetCustomAttributes(typeof(EnumMemberAttribute), false);
        
        if (attributes?.Length > 0 && attributes[0] is EnumMemberAttribute enumMemberAttr)
        {
            return enumMemberAttr.Value;
        }
        
        return enumValue.ToString();
    }
}

/// <summary>
/// Response from MiniMax video generation create request.
/// </summary>
internal class VendorMiniMaxVideoCreateResponse
{
    /// <summary>
    /// The video generation task ID.
    /// </summary>
    [JsonProperty("task_id")]
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// Error status code and details.
    /// </summary>
    [JsonProperty("base_resp")]
    public VendorMiniMaxBaseResp? BaseResp { get; set; }
}

/// <summary>
/// Response from MiniMax video generation query request.
/// </summary>
internal class VendorMiniMaxVideoQueryResponse
{
    /// <summary>
    /// The queried task ID.
    /// </summary>
    [JsonProperty("task_id")]
    public string? TaskId { get; set; }
    
    /// <summary>
    /// Current status: Preparing, Queueing, Processing, Success, Fail.
    /// </summary>
    [JsonProperty("status")]
    public string? Status { get; set; }
    
    /// <summary>
    /// File ID of the generated video (returned on success).
    /// </summary>
    [JsonProperty("file_id")]
    public string? FileId { get; set; }
    
    /// <summary>
    /// Width of the generated video in pixels (returned on success).
    /// </summary>
    [JsonProperty("video_width")]
    public int? VideoWidth { get; set; }
    
    /// <summary>
    /// Height of the generated video in pixels (returned on success).
    /// </summary>
    [JsonProperty("video_height")]
    public int? VideoHeight { get; set; }
    
    /// <summary>
    /// Error status code and details.
    /// </summary>
    [JsonProperty("base_resp")]
    public VendorMiniMaxBaseResp? BaseResp { get; set; }
}

/// <summary>
/// MiniMax base response containing status code and message.
/// </summary>
internal class VendorMiniMaxBaseResp
{
    /// <summary>
    /// Status code. 0 means success.
    /// </summary>
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }
    
    /// <summary>
    /// Status message details.
    /// </summary>
    [JsonProperty("status_msg")]
    public string? StatusMsg { get; set; }
}

/// <summary>
/// Response from MiniMax file retrieve endpoint (GET /v1/files/retrieve).
/// </summary>
internal class VendorMiniMaxFileRetrieveResponse
{
    /// <summary>
    /// The file object containing download URL and metadata.
    /// </summary>
    [JsonProperty("file")]
    public VendorMiniMaxFileObject? File { get; set; }
    
    /// <summary>
    /// Error status code and details.
    /// </summary>
    [JsonProperty("base_resp")]
    public VendorMiniMaxBaseResp? BaseResp { get; set; }
}

/// <summary>
/// MiniMax file object returned by the file retrieve endpoint.
/// </summary>
internal class VendorMiniMaxFileObject
{
    /// <summary>
    /// The unique identifier for the file.
    /// </summary>
    [JsonProperty("file_id")]
    public long FileId { get; set; }
    
    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    [JsonProperty("bytes")]
    public long Bytes { get; set; }
    
    /// <summary>
    /// Unix timestamp (seconds) when the file was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }
    
    /// <summary>
    /// The name of the file.
    /// </summary>
    [JsonProperty("filename")]
    public string? Filename { get; set; }
    
    /// <summary>
    /// The purpose of the file (e.g. "video_generation").
    /// </summary>
    [JsonProperty("purpose")]
    public string? Purpose { get; set; }
    
    /// <summary>
    /// The URL for downloading the file. Valid for 1 hour.
    /// </summary>
    [JsonProperty("download_url")]
    public string? DownloadUrl { get; set; }
}
