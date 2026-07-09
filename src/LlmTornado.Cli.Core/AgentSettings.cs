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

    /// <summary>
    /// Optional absolute cap for the context window used by compression/budget enforcement.
    /// Null = use the active model's full context window.
    /// </summary>
    [JsonPropertyName("compression_context_token_cap")]
    public int? CompressionContextTokenCap { get; set; }

    /// <summary>
    /// Context utilization (0..1) at which compression triggers. Null = built-in default (0.80).
    /// Each compression rewrites history (invalidating any server-side prompt cache), so higher =
    /// rarer rewrites.
    /// </summary>
    [JsonPropertyName("compression_trigger_utilization")]
    public double? CompressionTriggerUtilization { get; set; }

    /// <summary>
    /// Target context utilization (0..1) after compression. Null = built-in default (0.40).
    /// </summary>
    [JsonPropertyName("compression_target_utilization")]
    public double? CompressionTargetUtilization { get; set; }

    /// <summary>
    /// Whether large tool results are truncated (head + tail) before entering the context.
    /// Default: true — protects small local-model windows from a single huge read/grep result.
    /// </summary>
    [JsonPropertyName("tool_result_truncation")]
    public bool ToolResultTruncationEnabled { get; set; } = true;

    /// <summary>
    /// Maximum estimated tokens a single tool result may occupy before truncation.
    /// The effective cap is min(this, context window / 8). Default: 4000.
    /// </summary>
    [JsonPropertyName("tool_result_max_tokens")]
    public int ToolResultMaxTokens { get; set; } = 4000;

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

    /// <summary>
    /// Reasoning effort level for models that support extended thinking.
    /// Null = provider/model default. Valid values match <c>ChatReasoningEfforts</c> enum:
    /// "none", "minimal", "low", "medium", "high", "xhigh", "max", "default".
    /// Persisted across sessions.
    /// </summary>
    [JsonPropertyName("reasoning_effort")]
    public string? ReasoningEffort { get; set; }

    /// <summary>
    /// Automatically resume the most recent conversation on startup (equivalent to --continue).
    /// Default: false — a fresh conversation per run, matching industry CLIs.
    /// </summary>
    [JsonPropertyName("auto_resume")]
    public bool AutoResume { get; set; }

    /// <summary>
    /// Register the built-in C#-native file/shell tools (read_file, write_file, edit_file, glob,
    /// grep, list_dir, shell). Default: true — the agent works offline without Node/npx.
    /// </summary>
    [JsonPropertyName("native_tools")]
    public bool NativeToolsEnabled { get; set; } = true;

    /// <summary>
    /// Launch the built-in Desktop Commander MCP server (requires Node/npx). Default: false —
    /// opt-in now that native tools cover file/shell work; enable for its richer process tools.
    /// </summary>
    [JsonPropertyName("builtin_desktop_commander")]
    public bool BuiltInDesktopCommanderEnabled { get; set; }

    /// <summary>
    /// Pre-approve the read-only native tools (read_file, glob, grep, list_dir) so they don't
    /// prompt. Writes and shell always go through approval. Default: true.
    /// </summary>
    [JsonPropertyName("auto_approve_native_read_tools")]
    public bool AutoApproveNativeReadTools { get; set; } = true;

    /// <summary>
    /// Sampling temperature sent with every request. Null = provider/model default.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum output tokens per response. Null = provider/model default.
    /// </summary>
    [JsonPropertyName("max_output_tokens")]
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Path to a file whose content replaces the default persona block of the system prompt.
    /// Skills/tools layers are still appended (they are functionally required). Null = default.
    /// </summary>
    [JsonPropertyName("system_prompt_file")]
    public string? SystemPromptFile { get; set; }

    /// <summary>
    /// Whether streamed reasoning/thinking tokens should be shown in CLI output.
    /// Default: true.
    /// </summary>
    [JsonPropertyName("show_thinking")]
    public bool ShowThinking { get; set; } = true;

    /// <summary>
    /// Whether a [timestamp ...] line is prefixed onto every user and assistant message.
    /// Default: true.
    /// </summary>
    [JsonPropertyName("show_timestamps")]
    public bool ShowTimestamps { get; set; } = true;

    /// <summary>
    /// User-configured OpenAI-compatible endpoints (LM Studio, llama.cpp, vLLM, …).
    /// Merged with <c>TORNADO_OPENAI_COMPAT</c> env entries; settings win by name.
    /// </summary>
    [JsonPropertyName("openai_compat_endpoints")]
    public List<Providers.OpenAiCompatEndpoint>? OpenAiCompatEndpoints { get; set; }
}
