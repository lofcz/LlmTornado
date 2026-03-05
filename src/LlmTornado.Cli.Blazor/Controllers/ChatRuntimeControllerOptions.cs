namespace LlmTornado.Cli.Blazor.Controllers;

/// <summary>
/// Configuration for ChatRuntimeController.
/// All paths are optional — the controller uses sensible defaults
/// (environment variables, standard directories) when not specified.
/// </summary>
public sealed class ChatRuntimeControllerOptions
{
    /// <summary>
    /// Directory for conversation persistence files.
    /// Default: {AppData}/llmtornado/conversations/
    /// </summary>
    public string? ConversationsDirectory { get; set; }

    /// <summary>
    /// Directory for project-local skills.
    /// Default: ./skills/ (from CWD)
    /// </summary>
    public string? SkillsDirectory { get; set; }

    /// <summary>
    /// Directory for global skills.
    /// Default: TORNADO_SKILLS_DIR env var → {AppData}/llmtornado/skills/
    /// </summary>
    public string? GlobalSkillsDirectory { get; set; }

    /// <summary>
    /// Directory for custom agent persona .md files.
    /// Default: ./agents/ (from CWD)
    /// </summary>
    public string? AgentsDirectory { get; set; }

    /// <summary>
    /// Directory for global agent persona .md files.
    /// Default: TORNADO_AGENTS_DIR env var → {AppData}/llmtornado/agents/
    /// </summary>
    public string? GlobalAgentsDirectory { get; set; }

    /// <summary>
    /// Path to project-local mcp.json config file.
    /// Default: TORNADO_MCP_CONFIG env var → ./mcp.json (from CWD)
    /// </summary>
    public string? McpConfigPath { get; set; }

    /// <summary>
    /// Path to global mcp.json config file.
    /// Default: TORNADO_MCP_GLOBAL_CONFIG env var → {AppData}/llmtornado/mcp.json
    /// </summary>
    public string? GlobalMcpConfigPath { get; set; }

    /// <summary>
    /// Override working directory for the agent's system prompt.
    /// Default: Environment.CurrentDirectory
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Optional API key overrides. Keys are environment variable names
    /// (e.g., "OPENAI_API_KEY"), values are the API keys.
    /// These are set as environment variables before provider detection,
    /// supplementing (not replacing) existing env vars.
    /// </summary>
    public Dictionary<string, string>? ApiKeyOverrides { get; set; }

    /// <summary>
    /// Path for persisting AgentSettings JSON.
    /// Default: {AppData}/llmtornado/settings.json
    /// </summary>
    public string? SettingsPath { get; set; }

    /// <summary>
    /// Additional tools to register beyond skills and MCP tools.
    /// Allows the host app to inject app-specific tools.
    /// </summary>
    public List<LlmTornado.Common.Tool>? AdditionalTools { get; set; }
}
