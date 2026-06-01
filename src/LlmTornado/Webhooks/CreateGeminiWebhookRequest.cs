using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Request body for creating a project-level Gemini webhook.
/// </summary>
public class CreateGeminiWebhookRequest
{
    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [JsonProperty("name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// HTTPS listener URL.
    /// </summary>
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Event types to subscribe to (e.g. batch.succeeded, batch.failed).
    /// </summary>
    [JsonProperty("subscribed_events")]
    public List<string> SubscribedEvents { get; set; } = [];

    /// <summary>
    /// Serializes the request for the Gemini REST API.
    /// </summary>
    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }
}
