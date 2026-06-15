using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.ManagedAgents;

/// <summary>
/// List response from <c>GET /v1beta/agents</c>.
/// </summary>
public class ManagedAgentsListResponse
{
    [JsonProperty("object", NullValueHandling = NullValueHandling.Ignore)]
    public string? Object { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public List<ManagedAgent>? Data { get; set; }

    [JsonProperty("agents", NullValueHandling = NullValueHandling.Ignore)]
    public List<ManagedAgent>? Agents { get; set; }

    [JsonProperty("nextPageToken", NullValueHandling = NullValueHandling.Ignore)]
    public string? NextPageToken { get; set; }

    /// <summary>
    /// Agents from either <see cref="Data"/> or <see cref="Agents"/>.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<ManagedAgent> Items => Data ?? Agents ?? [];
}
