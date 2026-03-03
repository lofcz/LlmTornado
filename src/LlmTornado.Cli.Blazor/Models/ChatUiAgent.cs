namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// Source origin of an agent definition, mirroring AgentSource from Cli.Core.
/// </summary>
public enum ChatUiAgentSource
{
    BuiltIn,
    Global,
    Custom,
    Project
}

/// <summary>
/// A selectable agent persona for the agent dropdown.
/// </summary>
public sealed class ChatUiAgent
{
    /// <summary>
    /// Unique name identifier (e.g., "code-reviewer", "debugger").
    /// Used as the value when selecting.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Where this agent was loaded from.
    /// </summary>
    public ChatUiAgentSource Source { get; set; }

    /// <summary>
    /// Whether this agent has custom capability curation
    /// (skill/tool whitelists or blacklists).
    /// </summary>
    public bool HasCapabilityCuration { get; set; }
}
