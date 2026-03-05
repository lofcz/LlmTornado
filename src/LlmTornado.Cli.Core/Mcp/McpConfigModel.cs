using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core.Mcp;

/// <summary>
/// Source origin of an MCP server entry (global vs project-local).
/// </summary>
public enum McpServerSource
{
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
public sealed class McpConfig
{
    [JsonPropertyName("servers")]
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
    public required bool Connected { get; init; }
    public required int ToolCount { get; init; }
    public string? Error { get; init; }

    /// <summary>
    /// Whether this server came from the global or local config.
    /// </summary>
    public McpServerSource Source { get; init; }
}
