namespace LlmTornado.Acp.Server.Skills;

/// <summary>
/// Loads agent skills from SKILL.md files. Each file uses YAML front matter for metadata
/// and a markdown body for agent instructions, following the standard skill format.
/// </summary>
internal static class SkillLoader
{
    /// <summary>
    /// Loads all .skill.md files from the given directory and returns a dictionary keyed by skill name.
    /// </summary>
    public static Dictionary<string, AgentSkill> LoadFromDirectory(string directory)
    {
        Dictionary<string, AgentSkill> skills = new(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(directory))
        {
            return skills;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*.skill.md"))
        {
            AgentSkill? skill = ParseSkillFile(File.ReadAllText(file));

            if (skill is not null)
            {
                skills[skill.Name] = skill;
            }
        }

        return skills;
    }

    /// <summary>
    /// Loads skills from embedded string content (for when skills are compiled into the assembly).
    /// </summary>
    public static Dictionary<string, AgentSkill> LoadFromEmbedded(Dictionary<string, string> skillContents)
    {
        Dictionary<string, AgentSkill> skills = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> kvp in skillContents)
        {
            AgentSkill? skill = ParseSkillFile(kvp.Value);

            if (skill is not null)
            {
                skills[skill.Name] = skill;
            }
        }

        return skills;
    }

    /// <summary>
    /// Parses a SKILL.md file content into an AgentSkill.
    /// Expected format:
    /// ---
    /// name: skill-name
    /// display_name: Display Name
    /// description: Short description
    /// use_tools: true
    /// orchestrated: false
    /// ---
    /// ## Instructions
    /// Markdown body...
    /// </summary>
    internal static AgentSkill? ParseSkillFile(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        ReadOnlySpan<char> span = content.AsSpan().Trim();

        if (!span.StartsWith("---"))
        {
            return null;
        }

        int endFrontMatter = content.IndexOf("\n---", 3, StringComparison.Ordinal);

        if (endFrontMatter < 0)
        {
            return null;
        }

        string frontMatter = content.Substring(3, endFrontMatter - 3).Trim();
        string body = content.Substring(endFrontMatter + 4).Trim();

        AgentSkill skill = new() { Instructions = body };

        foreach (string line in frontMatter.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int colonIndex = line.IndexOf(':');

            if (colonIndex < 0)
            {
                continue;
            }

            string key = line.Substring(0, colonIndex).Trim().ToLowerInvariant();
            string value = line.Substring(colonIndex + 1).Trim();

            switch (key)
            {
                case "name":
                    skill.Name = value;
                    break;
                case "display_name":
                    skill.DisplayName = value;
                    break;
                case "description":
                    skill.Description = value;
                    break;
                case "use_tools":
                    skill.UseTools = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "orchestrated":
                    skill.Orchestrated = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        if (string.IsNullOrEmpty(skill.Name))
        {
            return null;
        }

        if (string.IsNullOrEmpty(skill.DisplayName))
        {
            skill.DisplayName = skill.Name;
        }

        // Parse stage instructions from named sections (## stage:name)
        ParseStageInstructions(skill, body);

        return skill;
    }

    /// <summary>
    /// Extracts stage-specific instructions from sections marked with ## stage:name headers.
    /// Used by orchestrated skills (refactor) to provide per-stage prompts.
    /// </summary>
    private static void ParseStageInstructions(AgentSkill skill, string body)
    {
        const string stagePrefix = "## stage:";
        string[] lines = body.Split('\n');
        string? currentStage = null;
        List<string> currentLines = [];

        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith(stagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (currentStage is not null && currentLines.Count > 0)
                {
                    skill.StageInstructions[currentStage] = string.Join('\n', currentLines).Trim();
                }

                currentStage = line.TrimStart().Substring(stagePrefix.Length).Trim().ToLowerInvariant();
                currentLines.Clear();
            }
            else if (currentStage is not null)
            {
                currentLines.Add(line);
            }
        }

        if (currentStage is not null && currentLines.Count > 0)
        {
            skill.StageInstructions[currentStage] = string.Join('\n', currentLines).Trim();
        }
    }
}
