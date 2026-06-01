using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Coordinator multiagent configuration on agent create/update.
/// </summary>
public class AnthropicManagedAgentMultiagentConfig
{
    /// <summary>
    /// Must be <c>coordinator</c>.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "coordinator";

    /// <summary>
    /// Agents the coordinator may delegate to (1–20 entries).
    /// </summary>
    [JsonProperty("agents")]
    public List<AnthropicManagedAgentRosterEntry> Agents { get; set; } = [];
}

/// <summary>
/// Resolved multiagent topology on an agent resource.
/// </summary>
public class AnthropicManagedAgentMultiagent
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("agents")]
    public List<AnthropicManagedAgentReference>? Agents { get; set; }
}

/// <summary>
/// Roster entry when creating a coordinator: agent ID, versioned reference, or self.
/// </summary>
[JsonConverter(typeof(AnthropicManagedAgentRosterEntryConverter))]
public class AnthropicManagedAgentRosterEntry
{
    /// <summary>
    /// When set, serializes as a plain agent ID string.
    /// </summary>
    [JsonIgnore]
    public string? AgentId { get; set; }

    /// <summary>
    /// <c>agent</c>, <c>self</c>, or omitted when using <see cref="AgentId"/>.
    /// </summary>
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
    public int? Version { get; set; }

    public static AnthropicManagedAgentRosterEntry FromAgentId(string agentId) => new() { AgentId = agentId };

    public static AnthropicManagedAgentRosterEntry FromAgent(string agentId, int? version = null) => new()
    {
        Type = "agent",
        Id = agentId,
        Version = version
    };

    public static AnthropicManagedAgentRosterEntry Self { get; } = new() { Type = "self" };
}

/// <summary>
/// Resolved agent reference in a coordinator roster.
/// </summary>
public class AnthropicManagedAgentReference
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("version")]
    public int? Version { get; set; }
}

internal sealed class AnthropicManagedAgentRosterEntryConverter : JsonConverter<AnthropicManagedAgentRosterEntry>
{
    public override AnthropicManagedAgentRosterEntry? ReadJson(JsonReader reader, Type objectType, AnthropicManagedAgentRosterEntry? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            return AnthropicManagedAgentRosterEntry.FromAgentId(reader.Value?.ToString() ?? string.Empty);
        }

        JObject obj = JObject.Load(reader);
        return obj.ToObject<AnthropicManagedAgentRosterEntry>(serializer);
    }

    public override void WriteJson(JsonWriter writer, AnthropicManagedAgentRosterEntry? value, JsonSerializer serializer)
    {
        if (value?.AgentId is not null)
        {
            writer.WriteValue(value.AgentId);
            return;
        }

        JObject.FromObject(value ?? new AnthropicManagedAgentRosterEntry(), serializer).WriteTo(writer);
    }
}
