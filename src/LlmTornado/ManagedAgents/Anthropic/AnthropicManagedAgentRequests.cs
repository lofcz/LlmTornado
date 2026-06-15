using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Request body for <c>POST /v1/agents</c>.
/// </summary>
public class AnthropicManagedAgentCreateRequest
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("model")]
    public object Model { get; set; } = AnthropicManagedAgentModels.ClaudeSonnet46;

    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    [JsonProperty("system", NullValueHandling = NullValueHandling.Ignore)]
    public string? System { get; set; }

    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? Tools { get; set; }

    [JsonProperty("mcp_servers", NullValueHandling = NullValueHandling.Ignore)]
    public List<AnthropicManagedAgentMcpServer>? McpServers { get; set; }

    [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? Skills { get; set; }

    [JsonProperty("multiagent", NullValueHandling = NullValueHandling.Ignore)]
    public AnthropicManagedAgentMultiagentConfig? Multiagent { get; set; }

    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Metadata { get; set; }

    internal string Serialize() => VendorAnthropicManagedAgentsJson.Serialize(this);
}

/// <summary>
/// Request body for <c>POST /v1/agents/{id}</c> (creates a new agent version).
/// </summary>
public class AnthropicManagedAgentUpdateRequest
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public object? Model { get; set; }

    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    [JsonProperty("system", NullValueHandling = NullValueHandling.Ignore)]
    public string? System { get; set; }

    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? Tools { get; set; }

    [JsonProperty("mcp_servers", NullValueHandling = NullValueHandling.Ignore)]
    public List<AnthropicManagedAgentMcpServer>? McpServers { get; set; }

    [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? Skills { get; set; }

    [JsonProperty("multiagent", NullValueHandling = NullValueHandling.Ignore)]
    public AnthropicManagedAgentMultiagentConfig? Multiagent { get; set; }

    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Metadata { get; set; }

    internal string Serialize() => VendorAnthropicManagedAgentsJson.Serialize(this);
}

/// <summary>
/// Agent reference for session create: agent ID string or versioned object.
/// </summary>
[JsonConverter(typeof(AnthropicManagedAgentSessionAgentConverter))]
public class AnthropicManagedAgentSessionAgent
{
    [JsonIgnore]
    public string? AgentId { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
    public long? Version { get; set; }

    public static AnthropicManagedAgentSessionAgent FromId(string agentId) => new() { AgentId = agentId };

    public static AnthropicManagedAgentSessionAgent FromVersioned(string agentId, long version) => new()
    {
        Type = "agent",
        Id = agentId,
        Version = version
    };
}

internal sealed class AnthropicManagedAgentSessionAgentConverter : JsonConverter<AnthropicManagedAgentSessionAgent>
{
    public override AnthropicManagedAgentSessionAgent? ReadJson(JsonReader reader, Type objectType, AnthropicManagedAgentSessionAgent? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            return AnthropicManagedAgentSessionAgent.FromId(reader.Value?.ToString() ?? string.Empty);
        }

        JObject obj = JObject.Load(reader);
        return obj.ToObject<AnthropicManagedAgentSessionAgent>(serializer);
    }

    public override void WriteJson(JsonWriter writer, AnthropicManagedAgentSessionAgent? value, JsonSerializer serializer)
    {
        if (value?.AgentId is not null)
        {
            writer.WriteValue(value.AgentId);
            return;
        }

        JObject.FromObject(value ?? new AnthropicManagedAgentSessionAgent(), serializer).WriteTo(writer);
    }
}
