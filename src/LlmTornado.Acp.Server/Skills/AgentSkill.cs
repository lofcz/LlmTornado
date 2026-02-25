namespace LlmTornado.Acp.Server.Skills;

/// <summary>
/// Represents a loaded agent skill definition parsed from a SKILL.md file.
/// Skills follow the standard format: YAML front matter with metadata, followed by
/// markdown instructions that serve as the agent's system prompt.
/// </summary>
internal sealed class AgentSkill
{
    /// <summary>
    /// Unique identifier for the skill, used as the ACP mode ID.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the skill.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Short description of what the skill does, shown in the ACP mode selector.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this skill requires filesystem tools to be available.
    /// </summary>
    public bool UseTools { get; set; }

    /// <summary>
    /// Whether this skill uses the orchestrated refactoring pipeline instead of a simple singleton agent.
    /// </summary>
    public bool Orchestrated { get; set; }

    /// <summary>
    /// The full markdown instructions body that becomes the agent's system prompt.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Optional sub-instructions for orchestration stages (analyze, plan, edit, verify).
    /// Parsed from named sections in the instructions body.
    /// </summary>
    public Dictionary<string, string> StageInstructions { get; set; } = new();
}
