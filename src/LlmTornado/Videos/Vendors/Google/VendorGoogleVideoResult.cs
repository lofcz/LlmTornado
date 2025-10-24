using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Videos.Vendors.Google;

/// <summary>
/// Google-specific video generation result.
/// </summary>
internal class VendorGoogleVideoResult
{
    /// <summary>
    /// The name of the operation for long-running video generation.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// Whether the operation is done.
    /// </summary>
    [JsonProperty("done")]
    public bool Done { get; set; }
    
    /// <summary>
    /// The response containing the generated video(s).
    /// </summary>
    [JsonProperty("response")]
    public VideoGenerationResponse? Response { get; set; }
    
    /// <summary>
    /// Error information if the operation failed.
    /// </summary>
    [JsonProperty("error")]
    public VideoGenerationError? Error { get; set; }
    
    /// <summary>
    /// Metadata associated with the operation.
    /// </summary>
    [JsonProperty("metadata")]
    public VideoGenerationMetadata? Metadata { get; set; }

    /// <summary>
    /// The response containing the generated video(s).
    /// </summary>
    internal class VideoGenerationResponse
    {
        /// <summary>
        /// The video generation response.
        /// </summary>
        [JsonProperty("generateVideoResponse")]
        public VideoGenerationData? GenerateVideoResponse { get; set; }
    }

    /// <summary>
    /// The video generation data.
    /// </summary>
    internal class VideoGenerationData
    {
        /// <summary>
        /// The generated video samples.
        /// </summary>
        [JsonProperty("generatedSamples")]
        public List<GeneratedVideoSample>? GeneratedSamples { get; set; }
    }

    /// <summary>
    /// A generated video sample.
    /// </summary>
    internal class GeneratedVideoSample
    {
        /// <summary>
        /// The generated video.
        /// </summary>
        [JsonProperty("video")]
        public GeneratedVideo? Video { get; set; }
    }

    /// <summary>
    /// A generated video.
    /// </summary>
    internal class GeneratedVideo
    {
        /// <summary>
        /// The URI of the generated video.
        /// </summary>
        [JsonProperty("uri")]
        public string? Uri { get; set; }
        
        /// <summary>
        /// The MIME type of the generated video.
        /// </summary>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
    }

    /// <summary>
    /// Error information for a failed video generation operation.
    /// </summary>
    internal class VideoGenerationError
    {
        /// <summary>
        /// The error code.
        /// </summary>
        [JsonProperty("code")]
        public int? Code { get; set; }
        
        /// <summary>
        /// The error message.
        /// </summary>
        [JsonProperty("message")]
        public string? Message { get; set; }
        
        /// <summary>
        /// The error status.
        /// </summary>
        [JsonProperty("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Metadata associated with a video generation operation.
    /// </summary>
    internal class VideoGenerationMetadata
    {
        /// <summary>
        /// The progress percentage of the operation.
        /// </summary>
        [JsonProperty("progressPercent")]
        public int? ProgressPercent { get; set; }
        
        /// <summary>
        /// The start time of the operation.
        /// </summary>
        [JsonProperty("startTime")]
        public string? StartTime { get; set; }
        
        /// <summary>
        /// The end time of the operation.
        /// </summary>
        [JsonProperty("endTime")]
        public string? EndTime { get; set; }
    }
}