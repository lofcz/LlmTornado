using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Per-request dynamic webhook configuration for Gemini async jobs (batch, video, interactions).
/// </summary>
public class GeminiWebhookConfig
{
    /// <summary>
    /// HTTPS listener URLs that receive event notifications for this job.
    /// </summary>
    [JsonProperty("uris")]
    public List<string> Uris { get; set; } = [];

    /// <summary>
    /// Optional opaque metadata echoed in webhook deliveries for routing or correlation.
    /// </summary>
    [JsonProperty("user_metadata")]
    public Dictionary<string, string>? UserMetadata { get; set; }
}
