using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// Remote sandbox environment configuration for managed agent interactions.
/// </summary>
public class InteractionEnvironmentConfig
{
    /// <summary>
    /// Always <c>remote</c> for managed agents.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "remote";

    /// <summary>
    /// Files and repositories to mount into the sandbox.
    /// </summary>
    [JsonProperty("sources", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionEnvironmentSource>? Sources { get; set; }

    /// <summary>
    /// Outbound network rules for the sandbox.
    /// </summary>
    [JsonProperty("network", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionEnvironmentNetwork? Network { get; set; }
}

/// <summary>
/// A file or repository source mounted into a managed agent environment.
/// </summary>
public class InteractionEnvironmentSource
{
    /// <summary>
    /// Source kind: <c>inline</c>, <c>repository</c>, or <c>gcs</c>.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Path inside the environment (e.g. <c>.agents/AGENTS.md</c>).
    /// </summary>
    [JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
    public string? Target { get; set; }

    /// <summary>
    /// Inline file content when <see cref="Type"/> is <c>inline</c>.
    /// </summary>
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    /// <summary>
    /// Optional encoding for inline content (e.g. <c>base64</c>).
    /// </summary>
    [JsonProperty("encoding", NullValueHandling = NullValueHandling.Ignore)]
    public string? Encoding { get; set; }

    /// <summary>
    /// GCS path or Git repository URL when <see cref="Type"/> is <c>gcs</c> or <c>repository</c>.
    /// </summary>
    [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
    public string? Source { get; set; }
}

/// <summary>
/// Network egress configuration for a managed agent environment.
/// </summary>
public class InteractionEnvironmentNetwork
{
    /// <summary>
    /// Allowed outbound domains (supports wildcards).
    /// </summary>
    [JsonProperty("allowlist", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionEnvironmentNetworkRule>? Allowlist { get; set; }
}

/// <summary>
/// A single domain allowlist entry with optional header transforms for credentials.
/// </summary>
public class InteractionEnvironmentNetworkRule
{
    /// <summary>
    /// Domain or wildcard pattern (e.g. <c>*.github.com</c>).
    /// </summary>
    [JsonProperty("domain")]
    public string? Domain { get; set; }

    /// <summary>
    /// Headers injected on outbound requests to this domain.
    /// </summary>
    [JsonProperty("transform", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Transform { get; set; }
}

/// <summary>
/// Environment reference for an interaction: fresh remote sandbox, existing environment ID, or full config.
/// </summary>
[JsonConverter(typeof(InteractionEnvironmentReferenceConverter))]
public class InteractionEnvironmentReference
{
    /// <summary>
    /// When set, serializes as the string <c>remote</c> or an existing environment ID.
    /// </summary>
    public string? EnvironmentId { get; set; }

    /// <summary>
    /// When set, serializes as a full <see cref="InteractionEnvironmentConfig"/> object.
    /// </summary>
    public InteractionEnvironmentConfig? Config { get; set; }

    /// <summary>
    /// Provisions a fresh remote sandbox.
    /// </summary>
    public static InteractionEnvironmentReference Remote { get; } = new() { EnvironmentId = "remote" };

    /// <summary>
    /// Reuses an existing environment by ID.
    /// </summary>
    public static InteractionEnvironmentReference FromId(string environmentId) => new() { EnvironmentId = environmentId };

    /// <summary>
    /// Uses a full environment configuration.
    /// </summary>
    public static InteractionEnvironmentReference FromConfig(InteractionEnvironmentConfig config) => new() { Config = config };
}

internal sealed class InteractionEnvironmentReferenceConverter : JsonConverter<InteractionEnvironmentReference>
{
    public override InteractionEnvironmentReference? ReadJson(JsonReader reader, Type objectType, InteractionEnvironmentReference? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.String)
        {
            return new InteractionEnvironmentReference { EnvironmentId = reader.Value?.ToString() };
        }

        if (reader.TokenType is JsonToken.StartObject)
        {
            JObject obj = JObject.Load(reader);
            return new InteractionEnvironmentReference
            {
                Config = obj.ToObject<InteractionEnvironmentConfig>(serializer)
            };
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, InteractionEnvironmentReference? value, JsonSerializer serializer)
    {
        if (value?.Config is not null)
        {
            serializer.Serialize(writer, value.Config);
            return;
        }

        writer.WriteValue(value?.EnvironmentId ?? "remote");
    }
}
