using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// A project-level Gemini webhook endpoint registered via the WebhookService API.
/// </summary>
public class GeminiWebhook
{
    /// <summary>
    /// Resource name (e.g. webhooks/{id}).
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Webhook identifier.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [JsonProperty("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// HTTPS URL that receives POST notifications.
    /// </summary>
    [JsonProperty("uri")]
    public string? Uri { get; set; }

    /// <summary>
    /// Event types this webhook is subscribed to.
    /// </summary>
    [JsonProperty("subscribed_events")]
    public List<string>? SubscribedEvents { get; set; }

    /// <summary>
    /// Signing secret returned only when the webhook is created or its secret is rotated.
    /// Store securely; it is not returned on subsequent GET requests.
    /// </summary>
    [JsonProperty("new_signing_secret")]
    public string? NewSigningSecret { get; set; }
}
