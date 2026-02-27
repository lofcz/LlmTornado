using System.Text.RegularExpressions;

namespace LlmTornado.Cli.Skills;

/// <summary>
/// Discovers skill directories and parses SKILL.md files per the Agent Skills standard.
/// </summary>
internal static partial class CliSkillLoader
{
    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex ValidSkillNameRegex();

    [GeneratedRegex(@"--")]
    private static partial Regex ConsecutiveHyphensRegex();

    /// <summary>
    /// Resolve the skills directory from settings or fall back to ./skills/ relative to CWD.
    /// </summary>
    public static string ResolveSkillsDirectory(CliSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.SkillsDirectory) && Directory.Exists(settings.SkillsDirectory))
            return Path.GetFullPath(settings.SkillsDirectory);

        return Path.GetFullPath("skills");
    }

    /// <summary>
    /// Discover all valid skill directories under the given path.
    /// </summary>
    public static List<CliSkill> DiscoverSkills(string skillsRootDirectory)
    {
        List<CliSkill> skills = [];

        if (!Directory.Exists(skillsRootDirectory))
            return skills;

        foreach (string dir in Directory.GetDirectories(skillsRootDirectory))
        {
            string skillMdPath = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillMdPath))
                continue;

            CliSkill? skill = ParseSkillMetadata(dir);
            if (skill is not null)
                skills.Add(skill);
        }

        return skills;
    }

    /// <summary>
    /// Parse a SKILL.md file frontmatter and return a CliSkill with metadata loaded.
    /// </summary>
    public static CliSkill? ParseSkillMetadata(string skillDirectory)
    {
        string skillMdPath = Path.Combine(skillDirectory, "SKILL.md");
        if (!File.Exists(skillMdPath))
            return null;

        string dirName = Path.GetFileName(skillDirectory);
        string content = File.ReadAllText(skillMdPath);

        // Parse YAML frontmatter
        Dictionary<string, object> frontmatter = ParseFrontmatter(content);

        string name = frontmatter.GetValueOrDefault("name") as string ?? dirName;

        // Validate name
        if (name.Length is < 1 or > 64)
            return null;
        if (!ValidSkillNameRegex().IsMatch(name))
            return null;
        if (ConsecutiveHyphensRegex().IsMatch(name))
            return null;
        if (name != dirName)
            return null;

        string description = frontmatter.GetValueOrDefault("description") as string ?? "";

        List<string> allowedTools = [];
        if (frontmatter.GetValueOrDefault("allowed-tools") is string toolsStr)
        {
            allowedTools = [..toolsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
        }

        Dictionary<string, string> metadata = new();
        if (frontmatter.GetValueOrDefault("metadata") is Dictionary<string, string> meta)
        {
            metadata = meta;
        }

        return new CliSkill
        {
            Name = name,
            Description = description,
            License = frontmatter.GetValueOrDefault("license") as string,
            Compatibility = frontmatter.GetValueOrDefault("compatibility") as string,
            Metadata = metadata,
            AllowedTools = allowedTools,
            DirectoryPath = Path.GetFullPath(skillDirectory),
            SkillMdPath = Path.GetFullPath(skillMdPath),
            Scripts = DiscoverScripts(skillDirectory),
            References = DiscoverReferences(skillDirectory),
            Assets = DiscoverAssets(skillDirectory),
        };
    }

    /// <summary>
    /// Load the full instructions body of a SKILL.md file.
    /// </summary>
    public static void LoadInstructions(CliSkill skill)
    {
        if (skill.Instructions is not null)
            return;

        string content = File.ReadAllText(skill.SkillMdPath);
        int secondDash = content.IndexOf("---", content.IndexOf("---", StringComparison.Ordinal) + 3, StringComparison.Ordinal);

        skill.Instructions = secondDash >= 0
            ? content[(secondDash + 3)..].Trim()
            : content;
    }

    /// <summary>
    /// Simple YAML frontmatter parser. Handles flat key: value pairs and one level of metadata nesting.
    /// </summary>
    private static Dictionary<string, object> ParseFrontmatter(string content)
    {
        Dictionary<string, object> result = new();

        int firstDash = content.IndexOf("---", StringComparison.Ordinal);
        if (firstDash < 0)
            return result;

        int secondDash = content.IndexOf("---", firstDash + 3, StringComparison.Ordinal);
        if (secondDash < 0)
            return result;

        string yaml = content[(firstDash + 3)..secondDash].Trim();
        string[] lines = yaml.Split('\n');
        string? currentKey = null;
        Dictionary<string, string>? currentMap = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r');

            // Indented line (part of a map)
            if (line.StartsWith("  ") && currentKey is not null && currentMap is not null)
            {
                string trimmed = line.TrimStart();
                int colonIdx = trimmed.IndexOf(':');
                if (colonIdx > 0)
                {
                    string key = trimmed[..colonIdx].Trim();
                    string value = trimmed[(colonIdx + 1)..].Trim().Trim('"');
                    currentMap[key] = value;
                }
                continue;
            }

            // Save previous map
            if (currentKey is not null && currentMap is not null)
            {
                result[currentKey] = currentMap;
                currentKey = null;
                currentMap = null;
            }

            // Top-level key: value
            int idx = line.IndexOf(':');
            if (idx <= 0)
                continue;

            string k = line[..idx].Trim();
            string v = line[(idx + 1)..].Trim().Trim('"');

            if (string.IsNullOrEmpty(v))
            {
                // Map start (e.g., "metadata:")
                currentKey = k;
                currentMap = new Dictionary<string, string>();
            }
            else
            {
                result[k] = v;
            }
        }

        // Save final map if any
        if (currentKey is not null && currentMap is not null)
        {
            result[currentKey] = currentMap;
        }

        return result;
    }

    private static List<SkillScript> DiscoverScripts(string skillDirectory)
    {
        string scriptsDir = Path.Combine(skillDirectory, "scripts");
        if (!Directory.Exists(scriptsDir))
            return [];

        List<SkillScript> scripts = [];
        foreach (string file in Directory.GetFiles(scriptsDir))
        {
            string ext = Path.GetExtension(file).TrimStart('.');
            if (string.IsNullOrEmpty(ext))
                continue;

            string? command = DetectScriptCommand(ext);
            if (command is null)
                continue;

            scripts.Add(new SkillScript
            {
                FileName = Path.GetFileName(file),
                AbsolutePath = Path.GetFullPath(file),
                Extension = ext,
                Command = command,
            });
        }
        return scripts;
    }

    private static List<string> DiscoverReferences(string skillDirectory)
    {
        string dir = Path.Combine(skillDirectory, "references");
        return !Directory.Exists(dir) ? [] : [..Directory.GetFiles(dir).Select(Path.GetFullPath)];
    }

    private static List<string> DiscoverAssets(string skillDirectory)
    {
        string dir = Path.Combine(skillDirectory, "assets");
        return !Directory.Exists(dir) ? [] : [..Directory.GetFiles(dir).Select(Path.GetFullPath)];
    }

    private static string? DetectScriptCommand(string extension) => extension.ToLowerInvariant() switch
    {
        "py" => OperatingSystem.IsWindows() ? "python" : "python3",
        "sh" => "bash",
        "ps1" => "pwsh",
        "js" => "node",
        "ts" => "npx tsx",
        "rb" => "ruby",
        _ => null,
    };
}
