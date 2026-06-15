using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Claude Managed Agent session from <c>POST/GET /v1/sessions</c>.
/// </summary>
public class AnthropicManagedAgentSession : ApiResultBase
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("environment_id")]
    public string? EnvironmentId { get; set; }

    [JsonProperty("agent")]
    public JObject? Agent { get; set; }

    [JsonProperty("resources")]
    public List<JObject>? Resources { get; set; }

    [JsonProperty("vault_ids")]
    public List<string>? VaultIds { get; set; }

    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonProperty("usage")]
    public JObject? Usage { get; set; }

    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonProperty("archived_at")]
    public string? ArchivedAt { get; set; }
}

/// <summary>
/// Request body for <c>POST /v1/sessions</c>.
/// </summary>
public class AnthropicManagedAgentSessionCreateRequest
{
    [JsonProperty("agent")]
    public AnthropicManagedAgentSessionAgent Agent { get; set; } = AnthropicManagedAgentSessionAgent.FromId(string.Empty);

    [JsonProperty("environment_id")]
    public string EnvironmentId { get; set; } = string.Empty;

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? Resources { get; set; }

    [JsonProperty("vault_ids", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? VaultIds { get; set; }

    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Metadata { get; set; }

    internal string Serialize() => VendorAnthropicManagedAgentsJson.Serialize(this);
}

/// <summary>
/// Request body for <c>POST /v1/sessions/{id}</c>.
/// </summary>
public class AnthropicManagedAgentSessionUpdateRequest
{
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonProperty("agent", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Agent { get; set; }

    [JsonProperty("vault_ids", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? VaultIds { get; set; }

    internal string Serialize() => VendorAnthropicManagedAgentsJson.Serialize(this);
}
