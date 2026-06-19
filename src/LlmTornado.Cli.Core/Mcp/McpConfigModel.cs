using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core.Mcp;

/// <summary>
/// Source origin of an MCP server entry (global vs project-local).
/// </summary>
public enum McpServerSource
{
    /// <summary>
    /// Built-in runtime-provided MCP server.
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Loaded from the global MCP config (%APPDATA%/llmtornado/mcp.json or TORNADO_MCP_GLOBAL_CONFIG).
    /// </summary>
    Global,

    /// <summary>
    /// Loaded from the project-local MCP config (./mcp.json or settings override).
    /// Local servers shadow global servers with the same name.
    /// </summary>
    Local
}

/// <summary>
/// Root model for mcp.json config file.
/// </summary>
[JsonConverter(typeof(McpConfigJsonConverter))]
public sealed class McpConfig
{
    [JsonIgnore]
    public List<McpServerEntry> Servers { get; set; } = [];
}

/// <summary>
/// A single MCP server definition.
/// </summary>
public sealed class McpServerEntry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "stdio";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; set; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("allowed_tools")]
    public List<string>? AllowedTools { get; set; }

    /// <summary>
    /// Runtime-only: whether this entry came from the global or local config.
    /// Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public McpServerSource Source { get; set; }
}

/// <summary>
/// Status of a configured MCP server.
/// </summary>
public sealed class McpServerStatus
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Description { get; init; }
    public required bool Connected { get; init; }
    public required int ToolCount { get; init; }
    public bool Enabled { get; init; } = true;
    public string? Error { get; init; }

    /// <summary>
    /// Whether this server came from the global or local config.
    /// </summary>
    public McpServerSource Source { get; init; }

    public bool IsReadOnly => Source == McpServerSource.BuiltIn;
}

internal sealed class McpConfigJsonConverter : JsonConverter<McpConfig>
{
    private const string McpServersProperty = "mcpServers";
    private const string LegacyServersProperty = "servers";

    public override McpConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("MCP config root must be a JSON object.");

        McpConfig config = new();
        bool sawMcpServers = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return config;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a property name in MCP config.");

            string propertyName = reader.GetString() ?? string.Empty;
            reader.Read();

            if (propertyName.Equals(McpServersProperty, StringComparison.Ordinal))
            {
                sawMcpServers = true;
                ReadMcpServersObject(ref reader, options, config.Servers);
                continue;
            }

            if (propertyName.Equals(LegacyServersProperty, StringComparison.Ordinal))
            {
                throw new JsonException("Legacy 'servers' array format is no longer supported. Use 'mcpServers' object format.");
            }

            reader.Skip();
        }

        if (!sawMcpServers)
            return config;

        throw new JsonException("Unexpected end of MCP config JSON.");
    }

    public override void Write(Utf8JsonWriter writer, McpConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(McpServersProperty);
        writer.WriteStartObject();

        foreach (McpServerEntry entry in value.Servers)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                continue;

            writer.WritePropertyName(entry.Name);
            JsonSerializer.Serialize(writer, McpServerJsonEntry.FromRuntime(entry), options);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void ReadMcpServersObject(ref Utf8JsonReader reader, JsonSerializerOptions options, List<McpServerEntry> servers)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("'mcpServers' must be a JSON object.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Each MCP server entry must be keyed by server name.");

            string serverName = reader.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(serverName))
                throw new JsonException("MCP server name cannot be empty.");

            reader.Read();
            McpServerJsonEntry? jsonEntry = JsonSerializer.Deserialize<McpServerJsonEntry>(ref reader, options);
            if (jsonEntry is null)
                throw new JsonException($"MCP server '{serverName}' has invalid configuration.");

            servers.Add(jsonEntry.ToRuntime(serverName));
        }

        throw new JsonException("Unexpected end of MCP servers JSON.");
    }
}

internal sealed class McpServerJsonEntry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; set; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("allowed_tools")]
    public List<string>? AllowedTools { get; set; }

    public McpServerEntry ToRuntime(string name)
    {
        return new McpServerEntry
        {
            Name = name,
            Type = InferType(Type, Url, Headers),
            Command = Command,
            Args = Args,
            Env = Env,
            Cwd = Cwd,
            Url = Url,
            Headers = Headers,
            AllowedTools = AllowedTools
        };
    }

    public static McpServerJsonEntry FromRuntime(McpServerEntry entry)
    {
        return new McpServerJsonEntry
        {
            Type = ShouldWriteType(entry.Type, entry.Url),
            Command = entry.Command,
            Args = entry.Args,
            Env = entry.Env,
            Cwd = entry.Cwd,
            Url = entry.Url,
            Headers = entry.Headers,
            AllowedTools = entry.AllowedTools
        };
    }

    private static string InferType(string? configuredType, string? url, Dictionary<string, string>? headers)
    {
        if (!string.IsNullOrWhiteSpace(configuredType))
            return configuredType;

        return string.IsNullOrWhiteSpace(url) && (headers is null || headers.Count == 0)
            ? "stdio"
            : "http";
    }

    private static string? ShouldWriteType(string? configuredType, string? url)
    {
        if (!string.IsNullOrWhiteSpace(configuredType))
        {
            if (!configuredType.Equals("stdio", StringComparison.OrdinalIgnoreCase))
                return configuredType;

            if (!string.IsNullOrWhiteSpace(url))
                return configuredType;

            return null;
        }

        if (!string.IsNullOrWhiteSpace(url))
            return "http";

        return null;
    }
}
