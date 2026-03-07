using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Blazor;

/// <summary>
/// Interface for settings management operations.
/// Implemented alongside <see cref="IChatUiController"/> by the runtime controller.
/// Provides read/write access to MCP servers, skills, and agent definitions.
/// Supports both global and project-local (scoped) operations.
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
    /// Get current status of all configured MCP servers (name, type, connected, tool count, scope).
    /// </summary>
    IReadOnlyList<McpServerStatus> GetMcpServerStatuses();

    /// <summary>
    /// Get the resolved path to the project-local mcp.json config file (whether it exists or not).
    /// </summary>
    string GetMcpConfigPath();

    /// <summary>
    /// Get the resolved path to the global mcp.json config file (whether it exists or not).
    /// </summary>
    string GetGlobalMcpConfigPath();

    /// <summary>
    /// Open the mcp.json file in the system's default text editor.
    /// Creates the file with a default empty config if it doesn't exist.
    /// </summary>
    Task OpenMcpConfigInEditorAsync();

    /// <summary>
    /// Reload MCP servers from both config files and rebuild the agent's tool set.
    /// Call this after the user edits the config file externally.
    /// </summary>
    Task ReloadMcpConfigAsync();

    /// <summary>
    /// Get the deserialized MCP config for a given scope.
    /// Returns null if the file doesn't exist or can't be parsed.
    /// </summary>
    McpConfig? GetMcpConfig(McpServerSource scope = McpServerSource.Local);

    /// <summary>
    /// Save the MCP config to disk for the specified scope.
    /// </summary>
    Task SaveMcpConfigAsync(McpConfig config, McpServerSource scope = McpServerSource.Local);

    /// <summary>
    /// Add a new MCP server entry to the config for the specified scope, save, and reload.
    /// </summary>
    Task AddMcpServerAsync(McpServerEntry entry, McpServerSource scope = McpServerSource.Local);

    /// <summary>
    /// Update an existing MCP server entry (matched by original name), save, and reload.
    /// Optionally move it to a different scope.
    /// </summary>
    Task UpdateMcpServerAsync(string originalName, McpServerEntry entry, McpServerSource? newScope = null);

    /// <summary>
    /// Remove an MCP server entry by name from whichever config it belongs to, save, and reload.
    /// </summary>
    Task RemoveMcpServerAsync(string serverName);

    /// <summary>
    /// Move an MCP server entry between global and local scope.
    /// </summary>
    Task MoveMcpServerAsync(string serverName, McpServerSource targetScope);

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
    /// Import a skill from an uploaded file (.md or .zip) into the specified scope directory.
    /// For .md files, creates a new skill folder with the file as SKILL.md.
    /// For .zip files, extracts the archive into the skills directory.
    /// Refreshes skills after import.
    /// </summary>
    Task ImportSkillAsync(string fileName, Stream content, SkillSource scope = SkillSource.Project);

    /// <summary>
    /// Move a skill between project-local and global scope.
    /// Copies the skill directory to the target location and removes the source.
    /// </summary>
    Task MoveSkillAsync(string skillName, SkillSource targetScope);

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
    /// Create a new agent .md file in the specified scope directory, save to disk, and reload.
    /// </summary>
    Task CreateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null,
        AgentSource scope = AgentSource.Custom);

    /// <summary>
    /// Update an existing custom/global agent .md file and reload.
    /// Only custom and global agents can be updated.
    /// </summary>
    Task UpdateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null);

    /// <summary>
    /// Delete a custom/global agent .md file and reload.
    /// Only custom and global agents can be deleted.
    /// </summary>
    Task DeleteAgentAsync(string name);

    /// <summary>
    /// Get the resolved custom agents directory path.
    /// </summary>
    string GetAgentsDirectory();

    /// <summary>
    /// Get the resolved global agents directory path.
    /// </summary>
    string GetGlobalAgentsDirectory();

    /// <summary>
    /// Move an agent between project-local and global scope.
    /// Copies the .md file to the target location and removes the source.
    /// </summary>
    Task MoveAgentAsync(string agentName, AgentSource targetScope);

    /// <summary>
    /// Re-discover agents from disk and update the UI agent list.
    /// </summary>
    void RefreshAgents();

    /// <summary>
    /// Get all currently available tools (MCP + skill tools) with name and description.
    /// Useful for the agent capability curation tool picker.
    /// </summary>
    List<ToolInfo> GetAvailableTools();

    /// <summary>
    /// Analyze an agent's skill and tool dependencies against currently loaded resources.
    /// Useful for warning users about broken references before moving an agent or after activation.
    /// </summary>
    AgentDependencyReport AnalyzeAgentDependencies(string agentName);
}

/// <summary>
/// Lightweight descriptor for a tool exposed to the settings UI.
/// </summary>
/// <param name="Name">Tool function name.</param>
/// <param name="Description">Human-readable description (may be empty).</param>
public sealed record ToolInfo(string Name, string Description);
