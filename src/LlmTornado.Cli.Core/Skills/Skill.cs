namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Source origin of a skill.
/// </summary>
internal enum SkillSource
{
    /// <summary>
    /// Loaded from the global skills directory (%APPDATA%/llmtornado/skills/ or TORNADO_SKILLS_DIR).
    /// </summary>
    Global,

    /// <summary>
    /// Loaded from the project-local skills directory (./skills/ or settings override).
    /// Project skills shadow global skills with the same name.
    /// </summary>
    Project
}

/// <summary>
/// Represents a loaded skill following the Agent Skills standard (agentskills.io).
/// </summary>
internal sealed class Skill
{
    // --- Frontmatter (always loaded) ---
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? License { get; init; }
    public string? Compatibility { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public List<string> AllowedTools { get; init; } = [];

    // --- Paths ---
    public required string DirectoryPath { get; init; }
    public required string SkillMdPath { get; init; }

    // --- Body (loaded on demand via progressive disclosure) ---
    public string? Instructions { get; set; }

    // --- Discovered Resources ---
    public List<SkillScript> Scripts { get; init; } = [];
    public List<string> References { get; init; } = [];
    public List<string> Assets { get; init; } = [];

    // --- Runtime State ---
    public bool Enabled { get; set; } = true;
    public bool Activated { get; set; }

    /// <summary>
    /// Where this skill was loaded from (global directory vs project-local).
    /// </summary>
    public SkillSource Source { get; set; } = SkillSource.Project;
}

/// <summary>
/// A script file found in a skill's scripts/ directory.
/// </summary>
internal sealed class SkillScript
{
    public required string FileName { get; init; }
    public required string AbsolutePath { get; init; }
    public required string Extension { get; init; }
    public required string Command { get; init; }
}
