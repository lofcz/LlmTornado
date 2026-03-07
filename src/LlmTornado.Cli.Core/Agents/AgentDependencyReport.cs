namespace LlmTornado.Cli.Core.Agents;

/// <summary>
/// Result of analyzing an agent's dependencies (skills, MCP tools) against available resources.
/// </summary>
public sealed class AgentDependencyReport
{
    /// <summary>
    /// Skills referenced by the agent that exist only in local/project scope.
    /// Moving the agent to global without these skills will leave it broken.
    /// </summary>
    public List<string> LocalOnlySkills { get; init; } = [];

    /// <summary>
    /// Skills referenced by the agent that don't exist in any scope.
    /// </summary>
    public List<string> MissingSkills { get; init; } = [];

    /// <summary>
    /// MCP tools referenced by the agent that exist only on local MCP servers.
    /// </summary>
    public List<string> LocalOnlyTools { get; init; } = [];

    /// <summary>
    /// MCP tools referenced by the agent that don't exist on any loaded MCP server.
    /// </summary>
    public List<string> MissingTools { get; init; } = [];

    /// <summary>
    /// True if all referenced dependencies are satisfied.
    /// </summary>
    public bool IsFullySatisfied =>
        LocalOnlySkills.Count == 0 &&
        MissingSkills.Count == 0 &&
        LocalOnlyTools.Count == 0 &&
        MissingTools.Count == 0;

    /// <summary>
    /// True if there are local-only dependencies that won't be available globally.
    /// </summary>
    public bool HasLocalOnlyDependencies =>
        LocalOnlySkills.Count > 0 || LocalOnlyTools.Count > 0;

    /// <summary>
    /// True if there are references to resources that don't exist at all.
    /// </summary>
    public bool HasMissingDependencies =>
        MissingSkills.Count > 0 || MissingTools.Count > 0;
}
