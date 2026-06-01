using System;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Parsed Gemini webhook event envelope delivered to a listener URL.
/// </summary>
public class GeminiWebhookEvent
{
    /// <summary>
    /// Event type (e.g. batch.succeeded, video.generated).
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Envelope schema version (e.g. v1).
    /// </summary>
    [JsonProperty("version")]
    public string? Version { get; set; }

    /// <summary>
    /// UTC timestamp of the event.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Event-specific payload pointers.
    /// </summary>
    [JsonProperty("data")]
    public GeminiWebhookEventData? Data { get; set; }

    /// <summary>
    /// Parses a webhook JSON payload.
    /// </summary>
    public static GeminiWebhookEvent? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<GeminiWebhookEvent>(json);
    }

    /// <summary>
    /// Whether this event represents a terminal batch state.
    /// </summary>
    public bool IsBatchTerminalEvent =>
        Type is GeminiWebhookEventTypes.BatchSucceeded
            or GeminiWebhookEventTypes.BatchFailed
            or GeminiWebhookEventTypes.BatchCancelled
            or GeminiWebhookEventTypes.BatchExpired;

    /// <summary>
    /// Whether this event represents a terminal video generation state.
    /// </summary>
    public bool IsVideoTerminalEvent => Type == GeminiWebhookEventTypes.VideoGenerated;
}
