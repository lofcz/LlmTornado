using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Claude Managed Agent resource from <c>POST/GET /v1/agents</c>.
/// </summary>
public class AnthropicManagedAgent : ApiResultBase
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("model")]
    public JToken? Model { get; set; }

    [JsonProperty("system")]
    public string? System { get; set; }

    [JsonProperty("version")]
    public long? Version { get; set; }

    [JsonProperty("tools")]
    public List<JObject>? Tools { get; set; }

    [JsonProperty("mcp_servers")]
    public List<AnthropicManagedAgentMcpServer>? McpServers { get; set; }

    [JsonProperty("skills")]
    public List<JObject>? Skills { get; set; }

    [JsonProperty("multiagent")]
    public AnthropicManagedAgentMultiagent? Multiagent { get; set; }

    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonProperty("archived_at")]
    public string? ArchivedAt { get; set; }
}
