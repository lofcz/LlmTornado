using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core;

/// <summary>
/// Serializable user settings. Shared between CLI and ACP server.
/// </summary>
public sealed class AgentSettings
{
    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("disabled_skills")]
    public HashSet<string> DisabledSkills { get; set; } = [];

    [JsonPropertyName("disabled_mcp_servers")]
    public HashSet<string> DisabledMcpServers { get; set; } = [];

    [JsonPropertyName("disabled_mcp_tools")]
    public Dictionary<string, HashSet<string>> DisabledMcpTools { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("filesystem_whitelist")]
    public HashSet<string> FilesystemWhitelist { get; set; } = [];

    [JsonPropertyName("terminal_directory_whitelist")]
    public HashSet<string> TerminalDirectoryWhitelist { get; set; } = [];

    [JsonPropertyName("allowed_commands")]
    public HashSet<string> AllowedCommands { get; set; } = [];

    [JsonPropertyName("blocked_commands")]
    public HashSet<string> BlockedCommands { get; set; } = [];

    [JsonPropertyName("skills_directory")]
    public string? SkillsDirectory { get; set; }

    [JsonPropertyName("mcp_config_path")]
    public string? McpConfigPath { get; set; }

    [JsonPropertyName("max_turns_before_summary")]
    public int MaxTurnsBeforeSummary { get; set; }

    /// <summary>
    /// Currently selected agent persona name. Null = default (no persona).
    /// Persisted across sessions. Restored on startup if the persona still exists.
    /// </summary>
    [JsonPropertyName("active_agent")]
    public string? ActiveAgent { get; set; }

    /// <summary>
    /// Custom path to the agents directory for persona discovery.
    /// Null = use default ./agents/ relative to CWD.
    /// </summary>
    [JsonPropertyName("agents_directory")]
    public string? AgentsDirectory { get; set; }

    /// <summary>
    /// Whether to auto-detect and inject project AGENTS.md files from the CWD hierarchy.
    /// Default: true.
    /// </summary>
    [JsonPropertyName("project_agents_enabled")]
    public bool ProjectAgentsEnabled { get; set; } = true;

    /// <summary>
    /// Maximum number of tools to send per LLM request.
    /// When total tools exceed this limit, an LLM-based optimizer selects the most relevant subset.
    /// Default: 25.
    /// </summary>
    [JsonPropertyName("max_tools")]
    public int MaxTools { get; set; } = 25;

    /// <summary>
    /// Whether the LLM-based tool optimizer is enabled.
    /// When disabled, all tools are sent regardless of count.
    /// Default: true.
    /// </summary>
    [JsonPropertyName("tool_optimizer_enabled")]
    public bool ToolOptimizerEnabled { get; set; } = true;
}
