using LlmTornado.Webhooks;
using Newtonsoft.Json;

namespace LlmTornado.Videos.Vendors.Google;

/// <summary>
/// Extensions to video generation request for Google.
/// </summary>
public class VideoGenerationRequestGoogleExtensions
{
    /// <summary>
    /// The Cloud Storage bucket to store the output videos. If not provided, base64-encoded video bytes are returned in the response.
    /// </summary>
    public string? StorageUri { get; set; }
    
    /// <summary>
    /// Add an invisible watermark to the generated videos. The default value is false for the veo-2.0-generate-001 model, and true for veo-3.x models including veo-3.1-generate-preview, veo-3.1-fast-generate-preview, and veo-3.1-lite-generate-preview.
    /// </summary>
    public bool? AddWatermark { get; set; }
    
    /// <summary>
    /// Seed used in decoding for Veo 3 models. Does not guarantee determinism but slightly improves reproducibility.
    /// </summary>
    public int? Seed { get; set; }
    
    /// <summary>
    /// Number of videos to generate. Only used for video extension (Veo 3.1 and Veo 3.1 Fast). Defaults to 1.
    /// </summary>
    public int? NumberOfVideos { get; set; }
    
    /// <summary>
    /// A setting that controls safety filter thresholds for generated videos.
    /// </summary>
    public VideoGenerationRequestGoogleExtensionsSafetySettings? SafetySetting { get; set; }

    /// <summary>
    /// Per-request dynamic webhook configuration. When set, Gemini POSTs a video.generated event on completion instead of requiring polling.
    /// </summary>
    public GeminiWebhookConfig? WebhookConfig { get; set; }
    
    /// <summary>
    /// Empty Google extensions.
    /// </summary>
    public VideoGenerationRequestGoogleExtensions()
    {
        
    }
}

/// <summary>
/// A setting that controls safety filter thresholds for generated videos.
/// </summary>
public enum VideoGenerationRequestGoogleExtensionsSafetySettings
{
    /// <summary>
    /// The highest safety threshold, resulting in the largest amount of generated videos that are filtered.
    /// </summary>
    BlockLowAndAbove,
    
    /// <summary>
    /// A medium safety threshold that balances filtering for potentially harmful and safe content.
    /// </summary>
    BlockMediumAndAbove,
    
    /// <summary>
    /// A safety threshold that reduces the number of requests blocked due to safety filters.
    /// </summary>
    BlockOnlyHigh
}