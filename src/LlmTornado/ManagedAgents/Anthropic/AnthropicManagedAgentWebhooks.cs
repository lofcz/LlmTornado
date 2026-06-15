using Newtonsoft.Json;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Webhook event <c>data.type</c> values from Managed Agents webhooks.
/// </summary>
public static class AnthropicManagedAgentWebhookEventTypes
{
    // Session lifecycle (documented for subscription)
    public const string SessionStatusRunStarted = "session.status_run_started";
    public const string SessionStatusIdled = "session.status_idled";
    public const string SessionStatusRescheduled = "session.status_rescheduled";
    public const string SessionStatusTerminated = "session.status_terminated";
    public const string SessionThreadCreated = "session.thread_created";
    public const string SessionThreadIdled = "session.thread_idled";
    public const string SessionThreadTerminated = "session.thread_terminated";
    public const string SessionOutcomeEvaluationEnded = "session.outcome_evaluation_ended";

    // Session lifecycle (additional API event types)
    public const string SessionCreated = "session.created";
    public const string SessionPending = "session.pending";
    public const string SessionRunning = "session.running";
    public const string SessionIdled = "session.idled";
    public const string SessionRequiresAction = "session.requires_action";
    public const string SessionArchived = "session.archived";
    public const string SessionDeleted = "session.deleted";

    // Vault lifecycle
    public const string VaultCreated = "vault.created";
    public const string VaultArchived = "vault.archived";
    public const string VaultDeleted = "vault.deleted";
    public const string VaultCredentialCreated = "vault_credential.created";
    public const string VaultCredentialArchived = "vault_credential.archived";
    public const string VaultCredentialDeleted = "vault_credential.deleted";
    public const string VaultCredentialRefreshFailed = "vault_credential.refresh_failed";
}

/// <summary>
/// Webhook delivery payload. Top-level object type is always <c>event</c>.
/// </summary>
public class AnthropicManagedAgentWebhookEvent
{
    /// <summary>
    /// Always <c>event</c>.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Unique event identifier for idempotency (same across retries).
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }

    [JsonProperty("data")]
    public AnthropicManagedAgentWebhookEventData? Data { get; set; }

    /// <summary>
    /// Parses a raw webhook JSON body.
    /// </summary>
    public static AnthropicManagedAgentWebhookEvent? Parse(string json) =>
        JsonConvert.DeserializeObject<AnthropicManagedAgentWebhookEvent>(json, VendorAnthropicManagedAgentsJson.Settings);
}

/// <summary>
/// Webhook event data — contains the event type and resource ID, not the full object.
/// </summary>
public class AnthropicManagedAgentWebhookEventData
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// ID of the session, vault, or credential that triggered the event.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("organization_id")]
    public string? OrganizationId { get; set; }

    [JsonProperty("workspace_id")]
    public string? WorkspaceId { get; set; }

    /// <summary>
    /// Present on <c>vault_credential.*</c> events.
    /// </summary>
    [JsonProperty("vault_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? VaultId { get; set; }
}
