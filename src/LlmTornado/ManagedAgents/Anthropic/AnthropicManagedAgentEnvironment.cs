using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Managed Agent environment from <c>POST/GET /v1/environments</c>.
/// </summary>
public class AnthropicManagedAgentEnvironment : ApiResultBase
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("config")]
    public JObject? Config { get; set; }

    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonProperty("archived_at")]
    public string? ArchivedAt { get; set; }
}

/// <summary>
/// Request body for <c>POST /v1/environments</c>.
/// </summary>
public class AnthropicManagedAgentEnvironmentCreateRequest
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    [JsonProperty("config")]
    public AnthropicManagedAgentEnvironmentConfig Config { get; set; } = AnthropicManagedAgentEnvironmentConfig.CloudDefault();

    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Metadata { get; set; }

    internal string Serialize() => VendorAnthropicManagedAgentsJson.Serialize(this);
}

/// <summary>
/// Environment sandbox configuration.
/// </summary>
public class AnthropicManagedAgentEnvironmentConfig
{
    [JsonProperty("type")]
    public string Type { get; set; } = "cloud";

    [JsonProperty("networking", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Networking { get; set; }

    public static AnthropicManagedAgentEnvironmentConfig CloudDefault() => new()
    {
        Type = "cloud",
        Networking = new JObject { ["type"] = "unrestricted" }
    };
}
