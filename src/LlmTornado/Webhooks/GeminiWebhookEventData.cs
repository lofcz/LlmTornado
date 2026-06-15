using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Thin payload data included in a Gemini webhook event envelope.
/// </summary>
public class GeminiWebhookEventData
{
    /// <summary>
    /// Resource identifier (batch id, interaction id, operation id, etc.).
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Output file URI for successful batch or video jobs (e.g. gs://bucket/results.jsonl).
    /// </summary>
    [JsonProperty("output_file_uri")]
    public string? OutputFileUri { get; set; }

    /// <summary>
    /// Generated file name for video events.
    /// </summary>
    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// Error code when an event represents a failure.
    /// </summary>
    [JsonProperty("error_code")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message when an event represents a failure.
    /// </summary>
    [JsonProperty("error_message")]
    public string? ErrorMessage { get; set; }
}
