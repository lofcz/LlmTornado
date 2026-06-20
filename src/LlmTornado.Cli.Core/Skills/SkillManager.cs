using System.Text;

namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Manages the lifecycle of skills: discover, enable/disable, activate, build context.
/// </summary>
public sealed class SkillManager
{
    private readonly AgentSettings _settings;
    private readonly ISettingsPersistence _persistence;
    private readonly Dictionary<string, Skill> _skills = new(StringComparer.OrdinalIgnoreCase);

    public SkillManager(AgentSettings settings, ISettingsPersistence persistence)
    {
        _settings = settings;
        _persistence = persistence;
    }

    /// <summary>
    /// Discover and load all skill metadata from the given directory.
    /// </summary>
    public void LoadSkills(string skillsDirectory)
    {
        LoadSkills(skillsDirectory, null);
    }

    /// <summary>
    /// Discover and load all skill metadata from global and project-local directories.
    /// Project-local skills shadow global skills with the same name.
    /// </summary>
    public void LoadSkills(string projectSkillsDirectory, string? globalSkillsDirectory)
    {
        _skills.Clear();
        List<Skill> discovered = SkillLoader.DiscoverAllSkills(projectSkillsDirectory, globalSkillsDirectory);

        foreach (Skill skill in discovered)
        {
            if (_settings.DisabledSkills.Contains(skill.Name))
                skill.Enabled = false;

            _skills[skill.Name] = skill;
        }
    }

    public List<Skill> GetAllSkills() => [.. _skills.Values];

    public List<Skill> GetEnabledSkills() => [.. _skills.Values.Where(s => s.Enabled)];

    public Skill? GetSkill(string name) =>
        _skills.GetValueOrDefault(name);

    public bool EnableSkill(string name)
    {
        if (!_skills.TryGetValue(name, out Skill? skill))
            return false;

        skill.Enabled = true;
        _settings.DisabledSkills.Remove(name);
        _persistence.SaveSettings(_settings);
        return true;
    }

    public bool DisableSkill(string name)
    {
        if (!_skills.TryGetValue(name, out Skill? skill))
            return false;

        skill.Enabled = false;
        skill.Activated = false;
        _settings.DisabledSkills.Add(name);
        _persistence.SaveSettings(_settings);
        return true;
    }

    /// <summary>
    /// Activate a skill: load full instructions. Returns the instructions text or null if not found.
    /// </summary>
    public string? ActivateSkill(string name)
    {
        if (!_skills.TryGetValue(name, out Skill? skill))
            return null;

        if (!skill.Enabled)
            return null;

        SkillLoader.LoadInstructions(skill);
        skill.Activated = true;
        return skill.Instructions;
    }

    /// <summary>
    /// Create a new skill on disk under <paramref name="rootDirectory"/> (project or global skills dir).
    /// Does NOT reload the in-memory catalog — the caller is expected to re-run <see cref="LoadSkills(string, string?)"/>.
    /// Returns the absolute path to the written SKILL.md.
    /// </summary>
    public string CreateSkill(string rootDirectory, string name, string description, string instructions,
        string? license = null, string? compatibility = null, List<string>? allowedTools = null,
        bool fullSkeleton = true)
    {
        return SkillLoader.WriteSkillMd(rootDirectory, name, description, instructions,
            license, compatibility, allowedTools, fullSkeleton);
    }

    /// <summary>
    /// Rewrite an existing skill's SKILL.md in place, preserving its directory/slug and skeleton.
    /// Returns the SKILL.md path, or null if no skill with the given name is loaded.
    /// Does NOT reload the in-memory catalog.
    /// </summary>
    public string? UpdateSkill(string name, string description, string instructions,
        string? license = null, string? compatibility = null, List<string>? allowedTools = null)
    {
        if (!_skills.TryGetValue(name, out Skill? existing))
            return null;

        string? root = Directory.GetParent(existing.DirectoryPath)?.FullName;
        if (root is null)
            return null;

        return SkillLoader.WriteSkillMd(root, existing.Name, description, instructions,
            license, compatibility, allowedTools, fullSkeleton: false);
    }

    /// <summary>
    /// Delete a skill's directory from disk and drop it from the in-memory catalog.
    /// Returns false if no skill with the given name is loaded or the directory could not be removed.
    /// Does NOT rebuild the runtime — the caller is expected to do that.
    /// </summary>
    public bool DeleteSkill(string name)
    {
        if (!_skills.TryGetValue(name, out Skill? skill))
            return false;

        try
        {
            if (Directory.Exists(skill.DirectoryPath))
                Directory.Delete(skill.DirectoryPath, recursive: true);

            _skills.Remove(name);
            if (_settings.DisabledSkills.Remove(name))
                _persistence.SaveSettings(_settings);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Build XML context for the system prompt listing available skills (metadata only).
    /// </summary>
    public string BuildSkillsContextXml()
    {
        List<Skill> enabled = GetEnabledSkills();
        if (enabled.Count == 0)
            return string.Empty;

        StringBuilder sb = new();
        sb.AppendLine("<available_skills>");

        foreach (Skill skill in enabled)
        {
            sb.AppendLine("  <skill>");
            sb.AppendLine($"    <name>{skill.Name}</name>");
            sb.AppendLine($"    <description>{skill.Description}</description>");
            sb.AppendLine($"    <location>{skill.SkillMdPath}</location>");

            if (skill.Scripts.Count > 0)
            {
                sb.AppendLine($"    <scripts>{string.Join(", ", skill.Scripts.Select(s => s.FileName))}</scripts>");
            }

            sb.AppendLine("  </skill>");
        }

        sb.AppendLine("</available_skills>");
        return sb.ToString();
    }
}
