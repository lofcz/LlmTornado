using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Blazor;

/// <summary>
/// Interface for settings management operations.
/// Implemented alongside <see cref="IChatUiController"/> by the runtime controller.
/// Provides read/write access to MCP servers, skills, and agent definitions.
/// </summary>
public interface ISettingsController
{
    // ─────────────────────────────────────────────
    // Working Directory
    // ─────────────────────────────────────────────

    /// <summary>
    /// Get the current effective working directory.
    /// Returns the explicitly configured working directory, or Environment.CurrentDirectory if none.
    /// </summary>
    string GetWorkingDirectory();

    /// <summary>
    /// Change the agent's working directory and reload all CWD-dependent resources.
    /// This re-resolves project-local skills, custom agents, MCP config, and the agent's
    /// system prompt CWD context — but only for paths that were not explicitly overridden
    /// via ChatRuntimeControllerOptions at startup.
    /// Throws DirectoryNotFoundException if the path does not exist.
    /// </summary>
    Task ChangeWorkingDirectoryAsync(string path);

    // ─────────────────────────────────────────────
    // MCP Servers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Get current status of all configured MCP servers (name, type, connected, tool count).
    /// </summary>
    IReadOnlyList<McpServerStatus> GetMcpServerStatuses();

    /// <summary>
    /// Get the resolved path to the mcp.json config file (whether it exists or not).
    /// </summary>
    string GetMcpConfigPath();

    /// <summary>
    /// Open the mcp.json file in the system's default text editor.
    /// Creates the file with a default empty config if it doesn't exist.
    /// </summary>
    Task OpenMcpConfigInEditorAsync();

    /// <summary>
    /// Reload MCP servers from the config file and rebuild the agent's tool set.
    /// Call this after the user edits the config file externally.
    /// </summary>
    Task ReloadMcpConfigAsync();

    /// <summary>
    /// Get the deserialized MCP config for in-app editing.
    /// Returns null if the file doesn't exist or can't be parsed.
    /// </summary>
    McpConfig? GetMcpConfig();

    /// <summary>
    /// Save the MCP config to disk.
    /// </summary>
    Task SaveMcpConfigAsync(McpConfig config);

    /// <summary>
    /// Add a new MCP server entry to the config, save, and reload.
    /// </summary>
    Task AddMcpServerAsync(McpServerEntry entry);

    /// <summary>
    /// Update an existing MCP server entry (matched by original name), save, and reload.
    /// </summary>
    Task UpdateMcpServerAsync(string originalName, McpServerEntry entry);

    /// <summary>
    /// Remove an MCP server entry by name, save, and reload.
    /// </summary>
    Task RemoveMcpServerAsync(string serverName);

    /// <summary>
    /// Test connectivity to an MCP server entry without adding it to the config.
    /// Returns a status result with connected state and tool count or error.
    /// </summary>
    Task<McpServerStatus> TestMcpConnectionAsync(McpServerEntry entry);

    // ─────────────────────────────────────────────
    // Skills
    // ─────────────────────────────────────────────

    /// <summary>
    /// Get all discovered skills (global + project-local).
    /// </summary>
    List<Skill> GetAllSkills();

    /// <summary>
    /// Enable or disable a skill by name. Persists to settings and rebuilds the agent.
    /// </summary>
    void SetSkillEnabled(string name, bool enabled);

    /// <summary>
    /// Get the resolved project-local skills directory path.
    /// </summary>
    string GetSkillsDirectory();

    /// <summary>
    /// Get the resolved global skills directory path.
    /// </summary>
    string GetGlobalSkillsDirectory();

    /// <summary>
    /// Read the raw SKILL.md content for a skill, for inline editing.
    /// Returns null if the skill or file is not found.
    /// </summary>
    string? ReadSkillFile(string skillName);

    /// <summary>
    /// Save updated SKILL.md content for a skill.
    /// </summary>
    Task SaveSkillFileAsync(string skillName, string content);

    /// <summary>
    /// Re-discover skills from disk and rebuild the agent's tool set.
    /// </summary>
    void RefreshSkills();

    /// <summary>
    /// Import a skill from an uploaded file (.md or .zip) into the project-local skills directory.
    /// For .md files, creates a new skill folder with the file as SKILL.md.
    /// For .zip files, extracts the archive into the skills directory.
    /// Refreshes skills after import.
    /// </summary>
    Task ImportSkillAsync(string fileName, Stream content);

    // ─────────────────────────────────────────────
    // Agents
    // ─────────────────────────────────────────────

    /// <summary>
    /// Get all discovered agent definitions (built-in + global + custom).
    /// </summary>
    List<AgentDefinition> GetAllAgentDefinitions();

    /// <summary>
    /// Get the name of the currently active agent persona, or null if none.
    /// </summary>
    string? GetActiveAgentName();

    /// <summary>
    /// Create a new custom agent .md file, save to disk, and reload.
    /// </summary>
    Task CreateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null);

    /// <summary>
    /// Update an existing custom agent .md file and reload.
    /// Only custom agents can be updated.
    /// </summary>
    Task UpdateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null);

    /// <summary>
    /// Delete a custom agent .md file and reload.
    /// Only custom agents can be deleted.
    /// </summary>
    Task DeleteAgentAsync(string name);

    /// <summary>
    /// Get the resolved custom agents directory path.
    /// </summary>
    string GetAgentsDirectory();

    /// <summary>
    /// Re-discover agents from disk and update the UI agent list.
    /// </summary>
    void RefreshAgents();

    /// <summary>
    /// Get all currently available tools (MCP + skill tools) with name and description.
    /// Useful for the agent capability curation tool picker.
    /// </summary>
    List<ToolInfo> GetAvailableTools();
}

/// <summary>
/// Lightweight descriptor for a tool exposed to the settings UI.
/// </summary>
/// <param name="Name">Tool function name.</param>
/// <param name="Description">Human-readable description (may be empty).</param>
public sealed record ToolInfo(string Name, string Description);
