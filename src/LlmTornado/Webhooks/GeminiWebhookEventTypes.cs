namespace LlmTornado.Webhooks;

/// <summary>
/// Known Gemini webhook event type identifiers.
/// </summary>
public static class GeminiWebhookEventTypes
{
    /// <summary>Batch job completed successfully.</summary>
    public const string BatchSucceeded = "batch.succeeded";

    /// <summary>Batch job was cancelled.</summary>
    public const string BatchCancelled = "batch.cancelled";

    /// <summary>Batch job expired without finishing within 24 hours.</summary>
    public const string BatchExpired = "batch.expired";

    /// <summary>Batch job failed.</summary>
    public const string BatchFailed = "batch.failed";

    /// <summary>Interaction requires user action (e.g. function call).</summary>
    public const string InteractionRequiresAction = "interaction.requires_action";

    /// <summary>Interaction LRO completed successfully.</summary>
    public const string InteractionCompleted = "interaction.completed";

    /// <summary>Interaction LRO failed.</summary>
    public const string InteractionFailed = "interaction.failed";

    /// <summary>Interaction LRO was cancelled.</summary>
    public const string InteractionCancelled = "interaction.cancelled";

    /// <summary>Video generation LRO completed.</summary>
    public const string VideoGenerated = "video.generated";

    /// <summary>All batch-related event types.</summary>
    public static readonly string[] BatchEvents =
    [
        BatchSucceeded,
        BatchCancelled,
        BatchExpired,
        BatchFailed
    ];

    /// <summary>All interaction-related event types.</summary>
    public static readonly string[] InteractionEvents =
    [
        InteractionRequiresAction,
        InteractionCompleted,
        InteractionFailed,
        InteractionCancelled
    ];
}
