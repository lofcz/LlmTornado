using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// The type of shell execution environment.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseShellEnvironmentTypes
{
    /// <summary>
    /// Local execution. Commands are run on the caller's machine.
    /// </summary>
    [EnumMember(Value = "local")]
    Local,

    /// <summary>
    /// OpenAI provisions and manages a container automatically for the request.
    /// </summary>
    [EnumMember(Value = "container_auto")]
    ContainerAuto,

    /// <summary>
    /// References a container previously created with the /v1/containers endpoint.
    /// </summary>
    [EnumMember(Value = "container_reference")]
    ContainerReference
}

/// <summary>
/// The type of skill attached to a shell environment.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseShellSkillTypes
{
    /// <summary>
    /// A reference to a skill uploaded via /v1/skills.
    /// </summary>
    [EnumMember(Value = "skill_reference")]
    SkillReference,

    /// <summary>
    /// An inline skill bundle included directly in the request.
    /// </summary>
    [EnumMember(Value = "inline")]
    Inline
}

/// <summary>
/// The encoding type of an inline skill source.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseShellInlineSkillSourceTypes
{
    /// <summary>
    /// Base64-encoded content.
    /// </summary>
    [EnumMember(Value = "base64")]
    Base64
}

/// <summary>
/// The type of network policy for a hosted container environment.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseNetworkPolicyTypes
{
    /// <summary>
    /// An allowlist-based policy. Only specified domains are accessible.
    /// </summary>
    [EnumMember(Value = "allowlist")]
    Allowlist
}

/// <summary>
/// Base class for shell tool environment configurations.
/// Determines where and how shell commands are executed.
/// </summary>
[JsonConverter(typeof(ResponseShellEnvironmentConverter))]
public abstract class ResponseShellEnvironment
{
    /// <summary>
    /// The type of the environment.
    /// </summary>
    [JsonProperty("type")]
    public abstract ResponseShellEnvironmentTypes Type { get; }
}

/// <summary>
/// Local shell environment. Commands are executed on the caller's local machine.
/// The caller is responsible for running <c>shell_call</c> actions and returning <c>shell_call_output</c>.
/// </summary>
public class ResponseShellEnvironmentLocal : ResponseShellEnvironment
{
    /// <summary>
    /// The environment type. Always <see cref="ResponseShellEnvironmentTypes.Local"/>.
    /// </summary>
    public override ResponseShellEnvironmentTypes Type => ResponseShellEnvironmentTypes.Local;

    /// <summary>
    /// An optional list of skills to make available in the local shell environment.
    /// </summary>
    [JsonProperty("skills")]
    public List<ResponseShellSkill>? Skills { get; set; }
}

/// <summary>
/// Automatic container environment. OpenAI provisions and manages a container for the request.
/// </summary>
public class ResponseShellEnvironmentContainerAuto : ResponseShellEnvironment
{
    /// <summary>
    /// The environment type. Always <see cref="ResponseShellEnvironmentTypes.ContainerAuto"/>.
    /// </summary>
    public override ResponseShellEnvironmentTypes Type => ResponseShellEnvironmentTypes.ContainerAuto;

    /// <summary>
    /// An optional list of skills to mount in the container environment.
    /// </summary>
    [JsonProperty("skills")]
    public List<ResponseShellSkill>? Skills { get; set; }

    /// <summary>
    /// Optional network policy controlling outbound access from the container.
    /// </summary>
    [JsonProperty("network_policy")]
    public ResponseNetworkPolicy? NetworkPolicy { get; set; }
}

/// <summary>
/// References a container previously created with the /v1/containers endpoint.
/// Use this for long-running environments that persist across requests.
/// </summary>
public class ResponseShellEnvironmentContainerReference : ResponseShellEnvironment
{
    /// <summary>
    /// The environment type. Always <see cref="ResponseShellEnvironmentTypes.ContainerReference"/>.
    /// </summary>
    public override ResponseShellEnvironmentTypes Type => ResponseShellEnvironmentTypes.ContainerReference;

    /// <summary>
    /// The ID of the referenced container.
    /// </summary>
    [JsonProperty("container_id")]
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new empty container reference environment.
    /// </summary>
    public ResponseShellEnvironmentContainerReference()
    {
    }

    /// <summary>
    /// Creates a container reference environment with the specified container ID.
    /// </summary>
    /// <param name="containerId">The ID of the container to reference.</param>
    public ResponseShellEnvironmentContainerReference(string containerId)
    {
        ContainerId = containerId;
    }
}

/// <summary>
/// Base class for skill definitions that can be mounted in shell environments.
/// </summary>
[JsonConverter(typeof(ResponseShellSkillConverter))]
public abstract class ResponseShellSkill
{
    /// <summary>
    /// The type of the skill.
    /// </summary>
    [JsonProperty("type")]
    public abstract ResponseShellSkillTypes Type { get; }
}

/// <summary>
/// References a skill uploaded via the /v1/skills endpoint.
/// </summary>
public class ResponseShellSkillReference : ResponseShellSkill
{
    /// <summary>
    /// The type of the skill. Always <see cref="ResponseShellSkillTypes.SkillReference"/>.
    /// </summary>
    public override ResponseShellSkillTypes Type => ResponseShellSkillTypes.SkillReference;

    /// <summary>
    /// The ID of the skill to reference.
    /// </summary>
    [JsonProperty("skill_id")]
    public string SkillId { get; set; } = string.Empty;

    /// <summary>
    /// The version of the skill. Can be an integer version number or "latest".
    /// If not specified, the default version is used.
    /// </summary>
    [JsonProperty("version")]
    public object? Version { get; set; }

    /// <summary>
    /// Creates a new empty skill reference.
    /// </summary>
    public ResponseShellSkillReference()
    {
    }

    /// <summary>
    /// Creates a skill reference with the specified skill ID.
    /// </summary>
    /// <param name="skillId">The ID of the skill to reference.</param>
    public ResponseShellSkillReference(string skillId)
    {
        SkillId = skillId;
    }

    /// <summary>
    /// Creates a skill reference with the specified skill ID and version.
    /// </summary>
    /// <param name="skillId">The ID of the skill to reference.</param>
    /// <param name="version">The version number or "latest".</param>
    public ResponseShellSkillReference(string skillId, object version)
    {
        SkillId = skillId;
        Version = version;
    }
}

/// <summary>
/// An inline skill bundle included directly in the request.
/// The skill content is provided as a base64-encoded zip.
/// </summary>
public class ResponseShellInlineSkill : ResponseShellSkill
{
    /// <summary>
    /// The type of the skill. Always <see cref="ResponseShellSkillTypes.Inline"/>.
    /// </summary>
    public override ResponseShellSkillTypes Type => ResponseShellSkillTypes.Inline;

    /// <summary>
    /// The name of the inline skill.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of the inline skill.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The source data for the inline skill.
    /// </summary>
    [JsonProperty("source")]
    public ResponseShellInlineSkillSource Source { get; set; } = new ResponseShellInlineSkillSource();
}

/// <summary>
/// Source data for an inline skill, typically a base64-encoded zip archive.
/// </summary>
public class ResponseShellInlineSkillSource
{
    /// <summary>
    /// The encoding type of the source data.
    /// </summary>
    [JsonProperty("type")]
    public ResponseShellInlineSkillSourceTypes Type { get; set; } = ResponseShellInlineSkillSourceTypes.Base64;

    /// <summary>
    /// The MIME type of the source data. Typically "application/zip".
    /// </summary>
    [JsonProperty("media_type")]
    public string MediaType { get; set; } = "application/zip";

    /// <summary>
    /// The base64-encoded source data.
    /// </summary>
    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Network policy controlling outbound access from a hosted container.
/// </summary>
public class ResponseNetworkPolicy
{
    /// <summary>
    /// The type of network policy.
    /// </summary>
    [JsonProperty("type")]
    public ResponseNetworkPolicyTypes Type { get; set; } = ResponseNetworkPolicyTypes.Allowlist;

    /// <summary>
    /// List of domains the container is allowed to access.
    /// Must be within your organization's allow list.
    /// </summary>
    [JsonProperty("allowed_domains")]
    public List<string> AllowedDomains { get; set; } = [];

    /// <summary>
    /// Optional domain secrets for authenticated access to allowed domains.
    /// Secret values are never visible to the model; only placeholder names are exposed.
    /// </summary>
    [JsonProperty("domain_secrets")]
    public List<ResponseDomainSecret>? DomainSecrets { get; set; }
}

/// <summary>
/// A domain secret for authenticated access to an allowed domain.
/// The raw secret value is applied by an auth-translation sidecar and never persisted
/// on API servers or visible in model context.
/// </summary>
public class ResponseDomainSecret
{
    /// <summary>
    /// The target domain for this secret.
    /// </summary>
    [JsonProperty("domain")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// A friendly name for the secret (e.g., "API_KEY"). This placeholder is visible to the model.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The actual secret value. Not visible to the model.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new empty domain secret.
    /// </summary>
    public ResponseDomainSecret()
    {
    }

    /// <summary>
    /// Creates a domain secret with the specified values.
    /// </summary>
    /// <param name="domain">The target domain.</param>
    /// <param name="name">The placeholder name for the secret.</param>
    /// <param name="value">The actual secret value.</param>
    public ResponseDomainSecret(string domain, string name, string value)
    {
        Domain = domain;
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Custom JSON converter for polymorphic deserialization of <see cref="ResponseShellEnvironment"/>.
/// </summary>
internal class ResponseShellEnvironmentConverter : JsonConverter<ResponseShellEnvironment>
{
    private static readonly StringEnumConverter EnumConverter = new StringEnumConverter();
    
    public override ResponseShellEnvironment? ReadJson(JsonReader reader, Type objectType, ResponseShellEnvironment? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();

        return type switch
        {
            "local" => jo.ToObject<ResponseShellEnvironmentLocal>(serializer),
            "container_auto" => jo.ToObject<ResponseShellEnvironmentContainerAuto>(serializer),
            "container_reference" => jo.ToObject<ResponseShellEnvironmentContainerReference>(serializer),
            _ => throw new JsonSerializationException($"Unknown shell environment type: {type}")
        };
    }

    public override void WriteJson(JsonWriter writer, ResponseShellEnvironment? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        EnumConverter.WriteJson(writer, value.Type, serializer);

        switch (value)
        {
            case ResponseShellEnvironmentLocal local:
                if (local.Skills is { Count: > 0 })
                {
                    writer.WritePropertyName("skills");
                    serializer.Serialize(writer, local.Skills);
                }
                break;

            case ResponseShellEnvironmentContainerAuto auto:
                if (auto.Skills is { Count: > 0 })
                {
                    writer.WritePropertyName("skills");
                    serializer.Serialize(writer, auto.Skills);
                }
                if (auto.NetworkPolicy is not null)
                {
                    writer.WritePropertyName("network_policy");
                    serializer.Serialize(writer, auto.NetworkPolicy);
                }
                break;

            case ResponseShellEnvironmentContainerReference containerRef:
                writer.WritePropertyName("container_id");
                writer.WriteValue(containerRef.ContainerId);
                break;
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// Custom JSON converter for polymorphic deserialization of <see cref="ResponseShellSkill"/>.
/// </summary>
internal class ResponseShellSkillConverter : JsonConverter<ResponseShellSkill>
{
    private static readonly StringEnumConverter EnumConverter = new StringEnumConverter();
    
    public override ResponseShellSkill? ReadJson(JsonReader reader, Type objectType, ResponseShellSkill? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();

        return type switch
        {
            "skill_reference" => jo.ToObject<ResponseShellSkillReference>(serializer),
            "inline" => jo.ToObject<ResponseShellInlineSkill>(serializer),
            _ => throw new JsonSerializationException($"Unknown shell skill type: {type}")
        };
    }

    public override void WriteJson(JsonWriter writer, ResponseShellSkill? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        EnumConverter.WriteJson(writer, value.Type, serializer);

        switch (value)
        {
            case ResponseShellSkillReference skillRef:
                writer.WritePropertyName("skill_id");
                writer.WriteValue(skillRef.SkillId);
                if (skillRef.Version is not null)
                {
                    writer.WritePropertyName("version");
                    if (skillRef.Version is string versionStr)
                    {
                        writer.WriteValue(versionStr);
                    }
                    else
                    {
                        writer.WriteValue(Convert.ToInt32(skillRef.Version));
                    }
                }
                break;

            case ResponseShellInlineSkill inline:
                writer.WritePropertyName("name");
                writer.WriteValue(inline.Name);
                if (inline.Description is not null)
                {
                    writer.WritePropertyName("description");
                    writer.WriteValue(inline.Description);
                }
                writer.WritePropertyName("source");
                serializer.Serialize(writer, inline.Source);
                break;
        }

        writer.WriteEndObject();
    }
}
