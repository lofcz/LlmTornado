using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Paginated list of Gemini webhooks.
/// </summary>
public class GeminiWebhookList
{
    /// <summary>
    /// Configured webhooks for the project.
    /// </summary>
    [JsonProperty("webhooks")]
    public List<GeminiWebhook> Webhooks { get; set; } = [];

    /// <summary>
    /// Token for the next page, if any.
    /// </summary>
    [JsonProperty("nextPageToken")]
    public string? NextPageToken { get; set; }
}
