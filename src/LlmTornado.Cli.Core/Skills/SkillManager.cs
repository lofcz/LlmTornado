using System.Text;

namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Manages the lifecycle of skills: discover, enable/disable, activate, build context.
/// </summary>
internal sealed class SkillManager
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
