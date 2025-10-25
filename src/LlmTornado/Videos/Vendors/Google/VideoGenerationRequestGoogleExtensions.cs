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
    /// Add an invisible watermark to the generated videos. The default value is false for the veo-2.0-generate-001 model, and true for the veo-3.0-generate-001, veo-3.0-fast-generate-001, and veo-3.1-generate-preview models.
    /// </summary>
    public bool? AddWatermark { get; set; }
    
    /// <summary>
    /// A setting that controls safety filter thresholds for generated videos.
    /// </summary>
    public VideoGenerationRequestGoogleExtensionsSafetySettings? SafetySetting { get; set; }
    
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