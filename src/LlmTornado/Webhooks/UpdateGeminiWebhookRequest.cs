using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Request body for updating an existing Gemini webhook.
/// </summary>
public class UpdateGeminiWebhookRequest
{
    /// <summary>
    /// Updated display name.
    /// </summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Updated listener URL.
    /// </summary>
    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? Uri { get; set; }

    /// <summary>
    /// Updated subscribed event types.
    /// </summary>
    [JsonProperty("subscribed_events", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? SubscribedEvents { get; set; }

    /// <summary>
    /// Serializes the request for the Gemini REST API.
    /// </summary>
    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }
}
