namespace LlmTornado.Cli.Core.Agents;

/// <summary>
/// Source origin of an agent definition.
/// </summary>
public enum AgentSource
{
    /// <summary>
    /// Shipped with the binary (from Agents/built-in/ directory).
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Loaded from the global agents directory (%APPDATA%/llmtornado/agents/ or TORNADO_AGENTS_DIR).
    /// Global agents shadow built-in agents with the same name.
    /// </summary>
    Global,

    /// <summary>
    /// User-created agent from the agents/ directory.
    /// Custom agents shadow built-in and global agents with the same name.
    /// </summary>
    Custom,

    /// <summary>
    /// Auto-detected AGENTS.md from the project's directory hierarchy.
    /// These provide context only — no capability curation.
    /// </summary>
    Project
}

/// <summary>
/// An agent definition representing either a selectable persona (with capability curation)
/// or auto-detected project context (instructions only).
/// 
/// Persona agents use YAML frontmatter for structured configuration.
/// Project agents are pure markdown per the AGENTS.md specification.
/// </summary>
public sealed class AgentDefinition
{
    // --- Identity ---

    /// <summary>
    /// Unique identifier for this agent. For personas, derived from frontmatter <c>name</c> field
    /// or filename slug. For project context, always "project-context".
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description. For personas, from frontmatter <c>description</c> field
    /// or first non-empty paragraph of the markdown body. For project context, 
    /// describes the source path(s).
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Where this agent definition came from.
    /// </summary>
    public required AgentSource Source { get; init; }

    /// <summary>
    /// Absolute path to the source file (.md for personas, AGENTS.md for project context).
    /// For merged project context (multiple AGENTS.md files), this is the nearest file's path.
    /// </summary>
    public required string FilePath { get; init; }

    // --- Instructions ---

    /// <summary>
    /// The markdown instructions body (everything after YAML frontmatter for personas,
    /// or the full file content for project AGENTS.md files).
    /// For merged project context, this contains all AGENTS.md contents concatenated
    /// with source path delimiters.
    /// </summary>
    public string Instructions { get; init; } = "";

    // --- Capability Curation (persona agents only, empty for project agents) ---

    /// <summary>
    /// Skill whitelist. If non-empty, only these skills should be enabled when this agent
    /// is active. If empty, all skills are available (subject to <see cref="DisabledSkills"/> blacklist).
    /// </summary>
    public List<string> EnabledSkills { get; init; } = [];

    /// <summary>
    /// Skill blacklist. These skills are force-disabled when this agent is active.
    /// Applied after <see cref="EnabledSkills"/> whitelist.
    /// </summary>
    public List<string> DisabledSkills { get; init; } = [];

    /// <summary>
    /// Tool whitelist. If non-empty, only these tools are registered when this agent is active.
    /// If empty, all tools from active skills are available (subject to <see cref="DisabledTools"/> blacklist).
    /// </summary>
    public List<string> EnabledTools { get; init; } = [];

    /// <summary>
    /// Tool blacklist. These specific tools are excluded from registration.
    /// Applied after <see cref="EnabledTools"/> whitelist.
    /// </summary>
    public List<string> DisabledTools { get; init; } = [];

    /// <summary>
    /// Tools to pre-approve (skip the confirmation prompt on first use).
    /// </summary>
    public List<string> AutoApproveTools { get; init; } = [];

    // --- Derived Properties ---

    /// <summary>
    /// Whether this agent has any capability curation configured.
    /// Project agents always return false.
    /// </summary>
    public bool HasCapabilityCuration =>
        EnabledSkills.Count > 0 ||
        DisabledSkills.Count > 0 ||
        EnabledTools.Count > 0 ||
        DisabledTools.Count > 0 ||
        AutoApproveTools.Count > 0;

    /// <summary>
    /// Whether this is a persona agent (BuiltIn or Custom).
    /// </summary>
    public bool IsPersona => Source is AgentSource.BuiltIn or AgentSource.Global or AgentSource.Custom;
}
