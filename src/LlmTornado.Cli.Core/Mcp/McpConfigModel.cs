using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core.Mcp;

/// <summary>
/// Root model for mcp.json config file.
/// </summary>
internal sealed class McpConfig
{
    [JsonPropertyName("servers")]
    public List<McpServerEntry> Servers { get; set; } = [];
}

/// <summary>
/// A single MCP server definition.
/// </summary>
internal sealed class McpServerEntry
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

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("allowed_tools")]
    public List<string>? AllowedTools { get; set; }
}

/// <summary>
/// Status of a configured MCP server.
/// </summary>
internal sealed class McpServerStatus
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool Connected { get; init; }
    public required int ToolCount { get; init; }
    public string? Error { get; init; }
}
