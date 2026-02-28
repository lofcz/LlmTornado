namespace LlmTornado.Cli.Core.Skills;

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
