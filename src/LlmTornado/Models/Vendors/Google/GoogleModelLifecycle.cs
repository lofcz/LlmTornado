using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Models.Vendors.Google;

/// <summary>
/// Lifecycle stage of a Gemini API model. Returned in <c>modelStatus.modelStage</c> on generateContent responses
/// and on model metadata from the models API.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum GoogleModelStage
{
    /// <summary>
    /// Default / unspecified stage.
    /// </summary>
    [EnumMember(Value = "MODEL_STAGE_UNSPECIFIED")]
    Unspecified,

    /// <summary>
    /// Subject to frequent tuning; not suitable for production.
    /// </summary>
    [EnumMember(Value = "UNSTABLE_EXPERIMENTAL")]
    UnstableExperimental,

    /// <summary>
    /// Experimental models for early testing.
    /// </summary>
    [EnumMember(Value = "EXPERIMENTAL")]
    Experimental,

    /// <summary>
    /// Preview models; may be used in production with billing enabled.
    /// </summary>
    [EnumMember(Value = "PREVIEW")]
    Preview,

    /// <summary>
    /// Stable models ready for production use.
    /// </summary>
    [EnumMember(Value = "STABLE")]
    Stable,

    /// <summary>
    /// On the path to deprecation; may be restricted to existing customers.
    /// </summary>
    [EnumMember(Value = "LEGACY")]
    Legacy,

    /// <summary>
    /// Deprecated; should not be used for new workloads.
    /// </summary>
    [EnumMember(Value = "DEPRECATED")]
    Deprecated,

    /// <summary>
    /// Retired; endpoint is no longer available.
    /// </summary>
    [EnumMember(Value = "RETIRED")]
    Retired
}

/// <summary>
/// Model lifecycle status returned by the Gemini API (<c>modelStatus</c>).
/// </summary>
public class GoogleModelStatus
{
    /// <summary>
    /// Human-readable explanation of the current model status.
    /// </summary>
    [JsonProperty("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Current lifecycle stage of the model.
    /// </summary>
    [JsonProperty("modelStage")]
    public GoogleModelStage? ModelStage { get; set; }

    /// <summary>
    /// UTC time when the model is scheduled to be retired, if applicable.
    /// </summary>
    [JsonProperty("retirementTime")]
    public DateTime? RetirementTime { get; set; }
}

/// <summary>
/// Known lifecycle metadata for a catalog model. Mirrors API <see cref="GoogleModelStatus"/> fields
/// plus a recommended replacement model id from the deprecations page.
/// </summary>
public class GoogleModelLifecycleInfo
{
    /// <summary>
    /// Lifecycle stage of the model.
    /// </summary>
    public GoogleModelStage Stage { get; set; }

    /// <summary>
    /// Scheduled retirement time, if known.
    /// </summary>
    public DateTime? RetirementTime { get; set; }

    /// <summary>
    /// Recommended replacement model id from the Gemini deprecations page.
    /// </summary>
    public string? ReplacementModel { get; set; }

    /// <summary>
    /// Optional status message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Converts API metadata to catalog lifecycle info.
    /// </summary>
    public static GoogleModelLifecycleInfo? FromStatus(GoogleModelStatus? status)
    {
        if (status is null)
        {
            return null;
        }

        return new GoogleModelLifecycleInfo
        {
            Stage = status.ModelStage ?? GoogleModelStage.Unspecified,
            RetirementTime = status.RetirementTime,
            Message = status.Message
        };
    }
}

/// <summary>
/// Documented Gemini file input size limits (see File input methods guide).
/// </summary>
public static class GeminiFileInputLimits
{
    /// <summary>
    /// Maximum inline data payload size per request (100 MB).
    /// </summary>
    public const long InlineDataMaxBytes = 100L * 1024 * 1024;

    /// <summary>
    /// Maximum inline PDF payload size per request (50 MB).
    /// </summary>
    public const long InlinePdfMaxBytes = 50L * 1024 * 1024;

    /// <summary>
    /// Maximum size for external HTTPS / pre-signed URL file references per request (100 MB).
    /// </summary>
    public const long ExternalUrlMaxBytes = 100L * 1024 * 1024;

    /// <summary>
    /// Maximum size for a single File API upload (2 GB).
    /// </summary>
    public const long FileApiUploadMaxBytes = 2L * 1024 * 1024 * 1024;
}
