using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Videos.Vendors.MiniMax;

/// <summary>
/// MiniMax-specific extensions for video generation requests.
/// </summary>
public class VideoMiniMaxExtensions
{
    /// <summary>
    /// Video resolution. Options depend on the model and duration.
    /// Defaults to 768P for Hailuo-2.3 and Hailuo-02 models, 720P for legacy models.
    /// </summary>
    public VideoMiniMaxResolution? Resolution { get; set; }
    
    /// <summary>
    /// Whether to automatically optimize the prompt. Defaults to true.
    /// Set to false for more precise control over the prompt.
    /// </summary>
    public bool? PromptOptimizer { get; set; }
    
    /// <summary>
    /// Reduces optimization time when <see cref="PromptOptimizer"/> is enabled.
    /// Defaults to false. Applies only to MiniMax-Hailuo-2.3, MiniMax-Hailuo-2.3-Fast, and MiniMax-Hailuo-02 models.
    /// </summary>
    public bool? FastPretreatment { get; set; }
    
    /// <summary>
    /// A callback URL to receive asynchronous task status updates.
    /// MiniMax sends a POST with a challenge field for validation, then pushes status updates.
    /// </summary>
    public string? CallbackUrl { get; set; }
}

/// <summary>
/// Video resolution options for MiniMax video generation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VideoMiniMaxResolution
{
    /// <summary>
    /// 512P resolution. Supported by MiniMax-Hailuo-02 for image-to-video.
    /// </summary>
    [EnumMember(Value = "512P")]
    P512,
    
    /// <summary>
    /// 720P resolution. Default for legacy models (T2V-01, I2V-01, etc.).
    /// </summary>
    [EnumMember(Value = "720P")]
    P720,
    
    /// <summary>
    /// 768P resolution. Default for MiniMax-Hailuo-2.3 and MiniMax-Hailuo-02 models.
    /// </summary>
    [EnumMember(Value = "768P")]
    P768,
    
    /// <summary>
    /// 1080P resolution. Supported by Hailuo-2.3 and Hailuo-02 models (6s duration only).
    /// </summary>
    [EnumMember(Value = "1080P")]
    P1080
}
