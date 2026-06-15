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
/// Environment config type discriminator values.
/// </summary>
public static class AnthropicManagedAgentEnvironmentConfigTypes
{
    public const string Cloud = "cloud";
    public const string SelfHosted = "self_hosted";
}

/// <summary>
/// Visibility scope for self-hosted environments.
/// </summary>
public static class AnthropicManagedAgentEnvironmentScopes
{
    public const string Organization = "organization";
    public const string Account = "account";
}

/// <summary>
/// Environment sandbox configuration for create/update requests.
/// Use <see cref="CloudDefault"/> or <see cref="SelfHostedDefault"/> factories.
/// </summary>
public class AnthropicManagedAgentEnvironmentConfig
{
    [JsonProperty("type")]
    public string Type { get; set; } = AnthropicManagedAgentEnvironmentConfigTypes.Cloud;

    [JsonProperty("networking", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Networking { get; set; }

    /// <summary>
    /// Self-hosted visibility scope: <c>organization</c> or <c>account</c>. Only applicable when <see cref="Type"/> is <c>self_hosted</c>.
    /// </summary>
    [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
    public string? Scope { get; set; }

    public static AnthropicManagedAgentEnvironmentConfig CloudDefault() => new()
    {
        Type = AnthropicManagedAgentEnvironmentConfigTypes.Cloud,
        Networking = new JObject { ["type"] = "unrestricted" }
    };

    /// <summary>
    /// Self-hosted environment with default scope (server-assigned based on organization type).
    /// </summary>
    public static AnthropicManagedAgentEnvironmentConfig SelfHostedDefault() => new()
    {
        Type = AnthropicManagedAgentEnvironmentConfigTypes.SelfHosted
    };

    /// <summary>
    /// Self-hosted environment with an explicit visibility scope.
    /// </summary>
    public static AnthropicManagedAgentEnvironmentConfig SelfHosted(string? scope = null) => new()
    {
        Type = AnthropicManagedAgentEnvironmentConfigTypes.SelfHosted,
        Scope = scope
    };
}

/// <summary>
/// Resolved self-hosted environment configuration on an environment resource.
/// </summary>
public class AnthropicManagedAgentSelfHostedEnvironmentConfig
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <c>organization</c> or <c>account</c>.
    /// </summary>
    [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
    public string? Scope { get; set; }
}
