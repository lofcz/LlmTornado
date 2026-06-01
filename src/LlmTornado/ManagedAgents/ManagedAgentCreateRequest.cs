using System.Collections.Generic;
using LlmTornado.Interactions;
using Newtonsoft.Json;

namespace LlmTornado.ManagedAgents;

/// <summary>
/// Request body for <c>POST /v1beta/agents</c>.
/// </summary>
public class ManagedAgentCreateRequest
{
    /// <summary>
    /// Unique agent ID (used as the <c>agent</c> parameter in interactions).
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Base managed agent to extend (currently <see cref="GoogleManagedAgentIds.AntigravityPreview052026"/>).
    /// </summary>
    [JsonProperty("base_agent")]
    public string BaseAgent { get; set; } = GoogleManagedAgentIds.AntigravityPreview052026;

    /// <summary>
    /// System instruction for the agent.
    /// </summary>
    [JsonProperty("system_instruction", NullValueHandling = NullValueHandling.Ignore)]
    public string? SystemInstruction { get; set; }

    /// <summary>
    /// Developer-facing description.
    /// </summary>
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    [JsonProperty("display_name", NullValueHandling = NullValueHandling.Ignore)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Environment provisioned on each invocation (sources, network rules).
    /// </summary>
    [JsonProperty("base_environment", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionEnvironmentConfig? BaseEnvironment { get; set; }

    /// <summary>
    /// Tools the agent may use.
    /// </summary>
    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionTool>? Tools { get; set; }

    internal string Serialize() => JsonConvert.SerializeObject(this, VendorGoogleInteractionsJson.Settings);

    internal Dictionary<string, object?> GetApiRevisionHeaders() =>
        new() { ["Api-Revision"] = InteractionSchemaRevision.May2026.ToHeaderValue()! };
}
