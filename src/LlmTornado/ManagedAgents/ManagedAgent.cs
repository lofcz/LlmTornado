using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Interactions;
using Newtonsoft.Json;

namespace LlmTornado.ManagedAgents;

/// <summary>
/// A saved managed agent configuration on the Gemini API.
/// </summary>
public class ManagedAgent : ApiResultBase
{
    /// <summary>
    /// Resource type, typically <c>agent</c>.
    /// </summary>
    [JsonProperty("object")]
    public string? Object { get; set; }

    /// <summary>
    /// Unique agent ID used when invoking via the Interactions API.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    [JsonProperty("display_name", NullValueHandling = NullValueHandling.Ignore)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Base managed agent (e.g. <see cref="GoogleManagedAgentIds.AntigravityPreview052026"/>).
    /// </summary>
    [JsonProperty("base_agent", NullValueHandling = NullValueHandling.Ignore)]
    public string? BaseAgent { get; set; }

    /// <summary>
    /// System instruction for the agent.
    /// </summary>
    [JsonProperty("system_instruction", NullValueHandling = NullValueHandling.Ignore)]
    public string? SystemInstruction { get; set; }

    /// <summary>
    /// Description for developers.
    /// </summary>
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    /// <summary>
    /// Base environment forked on each invocation.
    /// </summary>
    [JsonProperty("base_environment", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionEnvironmentConfig? BaseEnvironment { get; set; }

    /// <summary>
    /// Tools available to the agent.
    /// </summary>
    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionTool>? Tools { get; set; }

    /// <summary>
    /// ISO 8601 creation time.
    /// </summary>
    [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
    public string? Created { get; set; }

    /// <summary>
    /// ISO 8601 last update time.
    /// </summary>
    [JsonProperty("updated", NullValueHandling = NullValueHandling.Ignore)]
    public string? Updated { get; set; }
}
