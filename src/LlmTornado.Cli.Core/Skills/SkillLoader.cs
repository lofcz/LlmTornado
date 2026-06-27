using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Discovers skill directories and parses SKILL.md files per the Agent Skills standard.
/// </summary>
public static partial class SkillLoader
{
    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex ValidSkillNameRegex();

    [GeneratedRegex(@"--")]
    private static partial Regex ConsecutiveHyphensRegex();

    /// <summary>
    /// Matches the opening/closing <c>---</c> frontmatter delimiters on their own line.
    /// </summary>
    [GeneratedRegex(@"^---\s*$", RegexOptions.Multiline)]
    private static partial Regex FrontmatterDelimiterRegex();

    /// <summary>
    /// Matches Markdown links with relative paths (not http(s):// or mailto:).
    /// </summary>
    [GeneratedRegex(@"\[([^\]]+)\]\((?!https?://|mailto:)([^)]+)\)")]
    private static partial Regex RelativeLinkRegex();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Resolve the skills directory. If <paramref name="skillsDirectoryOverride"/> is non-null
    /// and exists, use it. Otherwise fall back to ./skills/ relative to CWD.
    /// </summary>
    public static string ResolveSkillsDirectory(string? skillsDirectoryOverride)
    {
        if (!string.IsNullOrEmpty(skillsDirectoryOverride) && Directory.Exists(skillsDirectoryOverride))
            return Path.GetFullPath(skillsDirectoryOverride);

        return Path.GetFullPath("skills");
    }

    /// <summary>
    /// Resolve the global skills directory.
    /// Checks the <c>TORNADO_SKILLS_DIR</c> environment variable first; if set and the directory exists, uses it.
    /// Otherwise falls back to <c>%APPDATA%/llmtornado/skills/</c> (or platform equivalent).
    /// </summary>
    public static string ResolveGlobalSkillsDirectory()
    {
        string? envDir = Environment.GetEnvironmentVariable("TORNADO_SKILLS_DIR");
        if (!string.IsNullOrEmpty(envDir) && Directory.Exists(envDir))
            return Path.GetFullPath(envDir);

        string configRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Some container/minimal Linux setups can return an empty ApplicationData path.
        if (string.IsNullOrWhiteSpace(configRoot))
            configRoot = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(configRoot))
        {
            string profileRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(profileRoot))
                configRoot = Path.Combine(profileRoot, ".config");
        }

        if (string.IsNullOrWhiteSpace(configRoot))
            configRoot = Path.GetFullPath(".config");

        return Path.GetFullPath(Path.Combine(configRoot, "llmtornado", "skills"));
    }

    /// <summary>
    /// Discover all valid skill directories under the given path.
    /// </summary>
    public static List<Skill> DiscoverSkills(string skillsRootDirectory)
    {
        return DiscoverSkills(skillsRootDirectory, SkillSource.Project);
    }

    /// <summary>
    /// Discover all valid skill directories under the given path, tagging them with the given source.
    /// </summary>
    public static List<Skill> DiscoverSkills(string skillsRootDirectory, SkillSource source)
    {
        List<Skill> skills = [];

        if (!Directory.Exists(skillsRootDirectory))
            return skills;

        foreach (string dir in Directory.GetDirectories(skillsRootDirectory))
        {
            string skillMdPath = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillMdPath))
                continue;

            Skill? skill = ParseSkillMetadata(dir);
            if (skill is not null)
            {
                skill.Source = source;
                skills.Add(skill);
            }
        }

        return skills;
    }

    /// <summary>
    /// Discover skills from both global and project-local directories.
    /// Project-local skills shadow global skills with the same name.
    /// </summary>
    public static List<Skill> DiscoverAllSkills(string projectSkillsDir, string? globalSkillsDir)
    {
        Dictionary<string, Skill> merged = new(StringComparer.OrdinalIgnoreCase);

        // 1. Load global skills first (lower precedence)
        if (!string.IsNullOrEmpty(globalSkillsDir))
        {
            List<Skill> globalSkills = DiscoverSkills(globalSkillsDir, SkillSource.Global);
            foreach (Skill skill in globalSkills)
                merged[skill.Name] = skill;
        }

        // 2. Load project-local skills — shadow global skills with same name
        List<Skill> projectSkills = DiscoverSkills(projectSkillsDir, SkillSource.Project);
        foreach (Skill skill in projectSkills)
            merged[skill.Name] = skill;

        return [.. merged.Values];
    }

    /// <summary>
    /// Parse a SKILL.md file frontmatter and return a Skill with metadata loaded.
    /// </summary>
    public static Skill? ParseSkillMetadata(string skillDirectory)
    {
        string skillMdPath = Path.Combine(skillDirectory, "SKILL.md");
        if (!File.Exists(skillMdPath))
            return null;

        string dirName = Path.GetFileName(skillDirectory);
        string content = File.ReadAllText(skillMdPath);

        // Parse YAML frontmatter using YamlDotNet
        SkillFrontmatter? frontmatter = ParseFrontmatter(content);
        if (frontmatter is null)
            return null;

        string name = frontmatter.Name ?? dirName;

        // Validate name per spec: 1-64 chars, lowercase alphanumeric + hyphens, no leading/trailing/consecutive hyphens
        if (name.Length is < 1 or > 64)
            return null;
        if (!ValidSkillNameRegex().IsMatch(name))
            return null;
        if (ConsecutiveHyphensRegex().IsMatch(name))
            return null;

        // Name must match directory name (case-insensitive for OS compatibility)
        if (!string.Equals(name, dirName, StringComparison.OrdinalIgnoreCase))
            return null;

        // Description is required and must be 1-1024 characters
        string? description = frontmatter.Description;
        if (string.IsNullOrEmpty(description) || description.Length > 1024)
            return null;

        // Compatibility, if provided, must be <= 500 characters
        string? compatibility = frontmatter.Compatibility;
        if (compatibility is not null && compatibility.Length > 500)
            return null;

        List<string> allowedTools = [];
        if (!string.IsNullOrEmpty(frontmatter.AllowedTools))
        {
            allowedTools = [..frontmatter.AllowedTools.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
        }

        Dictionary<string, string> metadata = frontmatter.Metadata ?? new();

        return new Skill
        {
            Name = name,
            Description = description,
            License = frontmatter.License,
            Compatibility = compatibility,
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
    /// Convert an arbitrary display name into a valid skill slug per the Agent Skills spec:
    /// lowercase, alphanumeric + single hyphens, no leading/trailing/consecutive hyphens, max 64 chars.
    /// Returns an empty string if nothing usable remains.
    /// </summary>
    public static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        string lowered = name.Trim().ToLowerInvariant();
        char[] chars = lowered.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        string collapsed = ConsecutiveHyphensRegex().Replace(new string(chars), "-");
        // ConsecutiveHyphensRegex only matches exactly "--"; collapse any remaining runs as well.
        while (collapsed.Contains("--"))
            collapsed = collapsed.Replace("--", "-");

        collapsed = collapsed.Trim('-');
        if (collapsed.Length > 64)
            collapsed = collapsed[..64].Trim('-');

        return collapsed;
    }

    /// <summary>
    /// Validate a skill name/slug against the Agent Skills spec rules.
    /// </summary>
    public static bool IsValidSkillName(string name) =>
        name.Length is >= 1 and <= 64
        && ValidSkillNameRegex().IsMatch(name)
        && !ConsecutiveHyphensRegex().IsMatch(name);

    /// <summary>
    /// Write a SKILL.md file (and, optionally, the standard skeleton subfolders) for a new or updated skill.
    /// The skill is created at <c>&lt;rootDirectory&gt;/&lt;slug&gt;/SKILL.md</c> where the slug is derived from
    /// <paramref name="name"/>. The frontmatter <c>name</c> is set to the slug so it matches the directory name,
    /// as required by <see cref="ParseSkillMetadata"/>. Returns the absolute path to the written SKILL.md.
    /// </summary>
    public static string WriteSkillMd(
        string rootDirectory,
        string name,
        string description,
        string instructions,
        string? license = null,
        string? compatibility = null,
        List<string>? allowedTools = null,
        bool fullSkeleton = true)
    {
        string slug = Slugify(name);
        if (!IsValidSkillName(slug))
            throw new ArgumentException($"'{name}' cannot be converted into a valid skill name.", nameof(name));

        string skillDir = Path.Combine(rootDirectory, slug);
        Directory.CreateDirectory(skillDir);

        if (fullSkeleton)
        {
            Directory.CreateDirectory(Path.Combine(skillDir, "scripts"));
            Directory.CreateDirectory(Path.Combine(skillDir, "references"));
            Directory.CreateDirectory(Path.Combine(skillDir, "assets"));
        }

        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine($"name: {slug}");
        sb.AppendLine($"description: \"{description.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");

        if (!string.IsNullOrWhiteSpace(license))
            sb.AppendLine($"license: {license}");
        if (!string.IsNullOrWhiteSpace(compatibility))
            sb.AppendLine($"compatibility: \"{compatibility.Replace("\"", "\\\"")}\"");
        if (allowedTools is { Count: > 0 })
            sb.AppendLine($"allowed-tools: {string.Join(' ', allowedTools)}");

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(instructions.TrimEnd());
        sb.AppendLine();

        string skillMdPath = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillMdPath, sb.ToString());
        return Path.GetFullPath(skillMdPath);
    }

    /// <summary>
    /// Load the full instructions body of a SKILL.md file.
    /// Resolves relative file links in the instructions to absolute paths.
    /// </summary>
    public static void LoadInstructions(Skill skill)
    {
        if (skill.Instructions is not null)
            return;

        string content = File.ReadAllText(skill.SkillMdPath);
        string body = ExtractBody(content);

        // Normalize relative file links to absolute paths so the agent can access referenced resources
        body = RelativeLinkRegex().Replace(body, match =>
        {
            string linkText = match.Groups[1].Value;
            string relativePath = match.Groups[2].Value;
            string absolutePath = Path.GetFullPath(Path.Combine(skill.DirectoryPath, relativePath));
            return $"[{linkText}]({absolutePath})";
        });

        skill.Instructions = body;
    }

    /// <summary>
    /// Extract the YAML string between the first pair of <c>---</c> delimiters at the start of the file.
    /// Returns null if no valid frontmatter block is found.
    /// </summary>
    private static string? ExtractYamlBlock(string content)
    {
        MatchCollection matches = FrontmatterDelimiterRegex().Matches(content);
        if (matches.Count < 2)
            return null;

        Match opening = matches[0];
        Match closing = matches[1];

        // The opening delimiter must be at the very start of the file (ignoring leading whitespace)
        if (content[..opening.Index].Trim().Length > 0)
            return null;

        int yamlStart = opening.Index + opening.Length;
        int yamlEnd = closing.Index;
        return content[yamlStart..yamlEnd];
    }

    /// <summary>
    /// Extract the Markdown body after the frontmatter block.
    /// </summary>
    private static string ExtractBody(string content)
    {
        MatchCollection matches = FrontmatterDelimiterRegex().Matches(content);
        if (matches.Count < 2)
            return content;

        Match closing = matches[1];
        return content[(closing.Index + closing.Length)..].Trim();
    }

    /// <summary>
    /// Parse YAML frontmatter using YamlDotNet into a strongly-typed model.
    /// Returns null if no valid frontmatter block is found.
    /// </summary>
    private static SkillFrontmatter? ParseFrontmatter(string content)
    {
        string? yaml = ExtractYamlBlock(content);
        if (yaml is null)
            return null;

        try
        {
            return YamlDeserializer.Deserialize<SkillFrontmatter>(yaml);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Strongly-typed model for SKILL.md YAML frontmatter.
    /// </summary>
    internal sealed class SkillFrontmatter
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? License { get; set; }
        public string? Compatibility { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? AllowedTools { get; set; }
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
