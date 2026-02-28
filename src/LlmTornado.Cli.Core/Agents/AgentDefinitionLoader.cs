using System.Text;
using System.Text.RegularExpressions;

namespace LlmTornado.Cli.Core.Agents;

/// <summary>
/// Stateless discovery and parsing of agent definitions from both
/// the project directory hierarchy (AGENTS.md files) and the agents
/// directory (persona .md files).
/// </summary>
public static partial class AgentDefinitionLoader
{
    /// <summary>
    /// Maximum number of parent directories to walk when scanning for AGENTS.md files.
    /// </summary>
    private const int MaxHierarchyDepth = 20;

    /// <summary>
    /// Maximum content size for any single file (100 KB).
    /// </summary>
    private const int MaxFileSize = 100 * 1024;

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex ValidNameRegex();

    [GeneratedRegex(@"--")]
    private static partial Regex ConsecutiveHyphensRegex();

    /// <summary>
    /// Resolve the custom agents directory. If <paramref name="agentsDirectoryOverride"/> is non-null
    /// and exists, use it. Otherwise fall back to ./agents/ relative to CWD.
    /// </summary>
    public static string ResolveAgentsDirectory(string? agentsDirectoryOverride)
    {
        if (!string.IsNullOrEmpty(agentsDirectoryOverride) && Directory.Exists(agentsDirectoryOverride))
            return Path.GetFullPath(agentsDirectoryOverride);

        return Path.GetFullPath("agents");
    }

    /// <summary>
    /// Resolve the global agents directory.
    /// Checks the <c>TORNADO_AGENTS_DIR</c> environment variable first; if set and the directory exists, uses it.
    /// Otherwise falls back to <c>%APPDATA%/llmtornado/agents/</c> (or platform equivalent).
    /// </summary>
    public static string ResolveGlobalAgentsDirectory()
    {
        string? envDir = Environment.GetEnvironmentVariable("TORNADO_AGENTS_DIR");
        if (!string.IsNullOrEmpty(envDir) && Directory.Exists(envDir))
            return Path.GetFullPath(envDir);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "llmtornado", "agents");
    }

    /// <summary>
    /// Resolve the built-in agents directory relative to the application binary.
    /// </summary>
    public static string ResolveBuiltInDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
    }

    /// <summary>
    /// Walk from the given directory toward the filesystem root, collecting
    /// every AGENTS.md file found. Returns a merged <see cref="AgentDefinition"/> with
    /// <see cref="AgentSource.Project"/>, or null if no AGENTS.md files exist.
    /// Files are ordered nearest-first (closest to startDirectory takes precedence).
    /// </summary>
    public static AgentDefinition? DiscoverProjectAgents(string startDirectory)
    {
        List<(string path, string content)> found = [];
        string? current = Path.GetFullPath(startDirectory);
        int depth = 0;

        while (current is not null && depth < MaxHierarchyDepth)
        {
            string agentsMdPath = Path.Combine(current, "AGENTS.md");
            try
            {
                if (File.Exists(agentsMdPath))
                {
                    string content = ReadFileSafe(agentsMdPath);
                    if (!string.IsNullOrWhiteSpace(content))
                        found.Add((agentsMdPath, content));
                }
            }
            catch (IOException)
            {
                // Permission error or other IO issue — skip and continue
            }

            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == current) break; // filesystem root
            current = parent;
            depth++;
        }

        if (found.Count == 0) return null;

        StringBuilder merged = new();
        for (int i = 0; i < found.Count; i++)
        {
            if (i > 0) merged.AppendLine();
            merged.AppendLine($"<!-- AGENTS.md from: {found[i].path} -->");
            merged.AppendLine(found[i].content.TrimEnd());
        }

        return new AgentDefinition
        {
            Name = "project-context",
            Description = $"Project context from {found.Count} AGENTS.md file(s)",
            Source = AgentSource.Project,
            FilePath = found[0].path,
            Instructions = merged.ToString()
        };
    }

    /// <summary>
    /// Scan the built-in and custom agent directories for persona .md files.
    /// Custom agents shadow built-in agents with the same name.
    /// </summary>
    public static List<AgentDefinition> DiscoverPersonaAgents(
        string builtInDirectory, string customDirectory)
    {
        return DiscoverPersonaAgents(builtInDirectory, null, customDirectory);
    }

    /// <summary>
    /// Scan the built-in, global, and custom agent directories for persona .md files.
    /// Precedence: built-in → global → custom/project-local (most specific wins).
    /// </summary>
    public static List<AgentDefinition> DiscoverPersonaAgents(
        string builtInDirectory, string? globalDirectory, string customDirectory)
    {
        Dictionary<string, AgentDefinition> agents = new(StringComparer.OrdinalIgnoreCase);

        // 1. Load built-in agents first (lowest precedence)
        if (Directory.Exists(builtInDirectory))
        {
            foreach (string file in Directory.GetFiles(builtInDirectory, "*.md"))
            {
                AgentDefinition? agent = ParsePersonaFile(file, AgentSource.BuiltIn);
                if (agent is not null)
                    agents[agent.Name] = agent;
            }
        }

        // 2. Load global agents — shadow built-ins with same name
        if (!string.IsNullOrEmpty(globalDirectory) && Directory.Exists(globalDirectory))
        {
            foreach (string file in Directory.GetFiles(globalDirectory, "*.md"))
            {
                AgentDefinition? agent = ParsePersonaFile(file, AgentSource.Global);
                if (agent is not null)
                    agents[agent.Name] = agent;
            }
        }

        // 3. Load custom agents — shadow both built-in and global
        if (Directory.Exists(customDirectory))
        {
            foreach (string file in Directory.GetFiles(customDirectory, "*.md"))
            {
                AgentDefinition? agent = ParsePersonaFile(file, AgentSource.Custom);
                if (agent is not null)
                    agents[agent.Name] = agent;
            }
        }

        return [.. agents.Values];
    }

    /// <summary>
    /// Parse a single persona .md file into a <see cref="AgentDefinition"/>.
    /// </summary>
    internal static AgentDefinition? ParsePersonaFile(string filePath, AgentSource source)
    {
        string fileName = Path.GetFileName(filePath);
        string? slug = FileNameToSlug(fileName);
        if (slug is null) return null;

        string content;
        try
        {
            content = ReadFileSafe(filePath);
        }
        catch (IOException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(content)) return null;

        Dictionary<string, object> frontmatter = ParseFrontmatter(content);
        string instructions = ExtractBody(content);

        string name = frontmatter.GetValueOrDefault("name") as string ?? slug;

        // Validate the name from frontmatter too
        if (name.Length is < 1 or > 64) return null;
        if (!ValidNameRegex().IsMatch(name)) return null;
        if (ConsecutiveHyphensRegex().IsMatch(name)) return null;

        string description = frontmatter.GetValueOrDefault("description") as string
                             ?? ExtractDescriptionFromMarkdown(instructions);

        return new AgentDefinition
        {
            Name = name,
            Description = description,
            Source = source,
            FilePath = Path.GetFullPath(filePath),
            Instructions = instructions,
            EnabledSkills = ParseSpaceDelimitedList(frontmatter, "enabled-skills"),
            DisabledSkills = ParseSpaceDelimitedList(frontmatter, "disabled-skills"),
            EnabledTools = ParseSpaceDelimitedList(frontmatter, "enabled-tools"),
            DisabledTools = ParseSpaceDelimitedList(frontmatter, "disabled-tools"),
            AutoApproveTools = ParseSpaceDelimitedList(frontmatter, "auto-approve-tools"),
        };
    }

    /// <summary>
    /// Parse YAML-like frontmatter between --- delimiters.
    /// </summary>
    internal static Dictionary<string, object> ParseFrontmatter(string content)
    {
        Dictionary<string, object> result = new();

        int firstDash = content.IndexOf("---", StringComparison.Ordinal);
        if (firstDash < 0) return result;

        int secondDash = content.IndexOf("---", firstDash + 3, StringComparison.Ordinal);
        if (secondDash < 0) return result;

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
            if (idx <= 0) continue;

            string k = line[..idx].Trim();
            string v = line[(idx + 1)..].Trim().Trim('"');

            if (string.IsNullOrEmpty(v))
            {
                currentKey = k;
                currentMap = new Dictionary<string, string>();
            }
            else
            {
                result[k] = v;
            }
        }

        if (currentKey is not null && currentMap is not null)
            result[currentKey] = currentMap;

        return result;
    }

    /// <summary>
    /// Extract the markdown body after frontmatter (or the full content if no frontmatter).
    /// </summary>
    private static string ExtractBody(string content)
    {
        int firstDash = content.IndexOf("---", StringComparison.Ordinal);
        if (firstDash < 0) return content.Trim();

        int secondDash = content.IndexOf("---", firstDash + 3, StringComparison.Ordinal);
        if (secondDash < 0) return content.Trim();

        return content[(secondDash + 3)..].Trim();
    }

    /// <summary>
    /// Parse a space-delimited string value into a list.
    /// </summary>
    private static List<string> ParseSpaceDelimitedList(
        Dictionary<string, object> frontmatter, string key)
    {
        if (frontmatter.GetValueOrDefault(key) is not string value) return [];
        return [.. value.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    /// <summary>
    /// Extract description from the markdown body when no frontmatter description exists.
    /// Uses the first non-empty, non-heading paragraph.
    /// </summary>
    private static string ExtractDescriptionFromMarkdown(string markdown)
    {
        string[] lines = markdown.Split('\n');
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith('#')) continue;
            return line.Length > 200 ? line[..200] + "..." : line;
        }
        return "";
    }

    /// <summary>
    /// Convert a filename to a slug: "code-reviewer.md" -> "code-reviewer".
    /// Returns null if the filename doesn't produce a valid slug.
    /// </summary>
    private static string? FileNameToSlug(string fileName)
    {
        string slug = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();

        if (slug.Length is < 1 or > 64) return null;
        if (!ValidNameRegex().IsMatch(slug)) return null;
        if (ConsecutiveHyphensRegex().IsMatch(slug)) return null;

        return slug;
    }

    /// <summary>
    /// Read a file with size limit protection.
    /// </summary>
    private static string ReadFileSafe(string path)
    {
        string content = File.ReadAllText(path);
        if (content.Length > MaxFileSize)
            content = content[..MaxFileSize] + "\n[TRUNCATED]";
        return content;
    }
}
