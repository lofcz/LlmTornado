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
    /// Resolve the project skills directory. If <paramref name="skillsDirectoryOverride"/> is non-empty,
    /// use it. Otherwise use <c>&lt;cwd&gt;/llmtornado/skills</c> (see <see cref="TornadoPaths"/>).
    /// </summary>
    public static string ResolveSkillsDirectory(string? skillsDirectoryOverride)
    {
        if (!string.IsNullOrWhiteSpace(skillsDirectoryOverride))
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(skillsDirectoryOverride));

        return TornadoPaths.ProjectSkillsDirectory();
    }

    /// <summary>
    /// Resolve the global skills directory — a per-user folder that lives outside the source tree, so
    /// the built CLI never depends on shipped source files. Uses the universal <c>TORNADO_HOME</c> root
    /// (else <c>&lt;app-data&gt;/llmtornado</c>) with the <c>skills</c> subfolder.
    /// </summary>
    public static string ResolveGlobalSkillsDirectory() => TornadoPaths.GlobalSkillsDirectory();

    /// <summary>
    /// Seed the built-in skills that ship alongside the binary (under <c>&lt;app&gt;/skills</c>) into the
    /// global user folder, so a published build never reads skills from the source tree. Existing skill
    /// folders are left untouched — user edits are never overwritten.
    /// </summary>
    /// <param name="globalSkillsDirectory">The destination global skills folder (created if missing).</param>
    /// <param name="onSeeded">Optional sink invoked with the name of each skill that was seeded.</param>
    public static void SeedBuiltInSkills(string globalSkillsDirectory, Action<string>? onSeeded = null)
    {
        SeedBuiltInSkills(Path.Combine(AppContext.BaseDirectory, "skills"), globalSkillsDirectory, onSeeded);
    }

    /// <summary>
    /// Seed skills from <paramref name="bundledDir"/> into <paramref name="globalSkillsDirectory"/>,
    /// skipping any skill folder that already exists at the destination. Exposed for testing the
    /// seed source explicitly; production code uses the parameterless-source overload.
    /// </summary>
    public static void SeedBuiltInSkills(string bundledDir, string globalSkillsDirectory, Action<string>? onSeeded)
    {
        if (!Directory.Exists(bundledDir))
            return;

        Directory.CreateDirectory(globalSkillsDirectory);

        foreach (string sourceDir in Directory.GetDirectories(bundledDir))
        {
            // Only treat folders that actually contain a SKILL.md as seedable skills.
            if (!File.Exists(Path.Combine(sourceDir, "SKILL.md")))
                continue;

            string name = Path.GetFileName(sourceDir);
            string destDir = Path.Combine(globalSkillsDirectory, name);

            // Never clobber a skill the user already has (seeded earlier or hand-authored).
            if (Directory.Exists(destDir))
                continue;

            CopyDirectory(sourceDir, destDir);
            onSeeded?.Invoke(name);
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: false);

        foreach (string subDir in Directory.GetDirectories(sourceDir))
            CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
    }

    /// <summary>
    /// Discover all valid skill directories under the given path.
    /// </summary>
    /// <param name="onWarning">Optional sink invoked with a human-readable reason whenever a skill folder is skipped.</param>
    public static List<Skill> DiscoverSkills(string skillsRootDirectory, Action<string>? onWarning = null)
    {
        return DiscoverSkills(skillsRootDirectory, SkillSource.Project, onWarning);
    }

    /// <summary>
    /// Discover all valid skill directories under the given path, tagging them with the given source.
    /// </summary>
    /// <param name="onWarning">Optional sink invoked with a human-readable reason whenever a skill folder is skipped.</param>
    public static List<Skill> DiscoverSkills(string skillsRootDirectory, SkillSource source, Action<string>? onWarning = null)
    {
        List<Skill> skills = [];

        if (!Directory.Exists(skillsRootDirectory))
            return skills;

        foreach (string dir in Directory.GetDirectories(skillsRootDirectory))
        {
            string skillMdPath = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillMdPath))
            {
                onWarning?.Invoke($"Skipped '{dir}': no SKILL.md file.");
                continue;
            }

            Skill? skill = ParseSkillMetadata(dir, onWarning);
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
    /// <param name="onWarning">Optional sink invoked with a human-readable reason whenever a skill folder is skipped.</param>
    public static List<Skill> DiscoverAllSkills(string projectSkillsDir, string? globalSkillsDir, Action<string>? onWarning = null)
    {
        Dictionary<string, Skill> merged = new(StringComparer.OrdinalIgnoreCase);

        // 1. Load global skills first (lower precedence)
        if (!string.IsNullOrEmpty(globalSkillsDir))
        {
            List<Skill> globalSkills = DiscoverSkills(globalSkillsDir, SkillSource.Global, onWarning);
            foreach (Skill skill in globalSkills)
                merged[skill.Name] = skill;
        }

        // 2. Load project-local skills — shadow global skills with same name
        List<Skill> projectSkills = DiscoverSkills(projectSkillsDir, SkillSource.Project, onWarning);
        foreach (Skill skill in projectSkills)
            merged[skill.Name] = skill;

        return [.. merged.Values];
    }

    /// <summary>
    /// Parse a SKILL.md file frontmatter and return a Skill with metadata loaded.
    /// </summary>
    /// <param name="onWarning">Optional sink invoked with a human-readable reason when the skill is rejected.</param>
    public static Skill? ParseSkillMetadata(string skillDirectory, Action<string>? onWarning = null)
    {
        string skillMdPath = Path.Combine(skillDirectory, "SKILL.md");
        if (!File.Exists(skillMdPath))
        {
            onWarning?.Invoke($"Skipped '{skillDirectory}': no SKILL.md file.");
            return null;
        }

        string dirName = Path.GetFileName(skillDirectory);
        string content = File.ReadAllText(skillMdPath);

        // Parse YAML frontmatter using YamlDotNet
        SkillFrontmatter? frontmatter = ParseFrontmatter(content, out string? parseError);
        if (frontmatter is null)
        {
            onWarning?.Invoke($"Skipped '{dirName}': {parseError ?? "no valid YAML frontmatter (missing or malformed --- block)."}");
            return null;
        }

        string name = frontmatter.Name ?? dirName;

        // Validate name per spec: 1-64 chars, lowercase alphanumeric + hyphens, no leading/trailing/consecutive hyphens
        if (name.Length is < 1 or > 64
            || !ValidSkillNameRegex().IsMatch(name)
            || ConsecutiveHyphensRegex().IsMatch(name))
        {
            onWarning?.Invoke($"Skipped '{dirName}': invalid skill name '{name}' (use 1-64 lowercase letters, digits, and single hyphens).");
            return null;
        }

        // Name must match directory name (case-insensitive for OS compatibility)
        if (!string.Equals(name, dirName, StringComparison.OrdinalIgnoreCase))
        {
            onWarning?.Invoke($"Skipped '{dirName}': frontmatter name '{name}' must match the directory name '{dirName}'.");
            return null;
        }

        // Description is required and must be 1-1024 characters
        string? description = frontmatter.Description;
        if (string.IsNullOrEmpty(description) || description.Length > 1024)
        {
            onWarning?.Invoke($"Skipped '{dirName}': description is required and must be 1-1024 characters.");
            return null;
        }

        // Compatibility, if provided, must be <= 500 characters
        string? compatibility = frontmatter.Compatibility;
        if (compatibility is not null && compatibility.Length > 500)
        {
            onWarning?.Invoke($"Skipped '{dirName}': compatibility must be <= 500 characters.");
            return null;
        }

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
    private static SkillFrontmatter? ParseFrontmatter(string content, out string? error)
    {
        error = null;
        string? yaml = ExtractYamlBlock(content);
        if (yaml is null)
            return null;

        try
        {
            return YamlDeserializer.Deserialize<SkillFrontmatter>(yaml);
        }
        catch (Exception ex)
        {
            error = $"YAML frontmatter could not be parsed: {ex.Message}";
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
