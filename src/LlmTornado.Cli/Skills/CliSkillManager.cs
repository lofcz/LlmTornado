using System.Text;

namespace LlmTornado.Cli.Skills;

/// <summary>
/// Manages the lifecycle of skills: discover, enable/disable, activate, build context.
/// </summary>
internal sealed class CliSkillManager
{
    private readonly CliSettings _settings;
    private readonly Dictionary<string, CliSkill> _skills = new(StringComparer.OrdinalIgnoreCase);

    public CliSkillManager(CliSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Discover and load all skill metadata from the given directory.
    /// </summary>
    public void LoadSkills(string skillsDirectory)
    {
        _skills.Clear();
        List<CliSkill> discovered = CliSkillLoader.DiscoverSkills(skillsDirectory);

        foreach (CliSkill skill in discovered)
        {
            if (_settings.DisabledSkills.Contains(skill.Name))
                skill.Enabled = false;

            _skills[skill.Name] = skill;
        }
    }

    public List<CliSkill> GetAllSkills() => [.. _skills.Values];

    public List<CliSkill> GetEnabledSkills() => [.. _skills.Values.Where(s => s.Enabled)];

    public CliSkill? GetSkill(string name) =>
        _skills.GetValueOrDefault(name);

    public bool EnableSkill(string name)
    {
        if (!_skills.TryGetValue(name, out CliSkill? skill))
            return false;

        skill.Enabled = true;
        _settings.DisabledSkills.Remove(name);
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
        return true;
    }

    public bool DisableSkill(string name)
    {
        if (!_skills.TryGetValue(name, out CliSkill? skill))
            return false;

        skill.Enabled = false;
        skill.Activated = false;
        _settings.DisabledSkills.Add(name);
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
        return true;
    }

    /// <summary>
    /// Activate a skill: load full instructions. Returns the instructions text or null if not found.
    /// </summary>
    public string? ActivateSkill(string name)
    {
        if (!_skills.TryGetValue(name, out CliSkill? skill))
            return null;

        if (!skill.Enabled)
            return null;

        CliSkillLoader.LoadInstructions(skill);
        skill.Activated = true;
        return skill.Instructions;
    }

    /// <summary>
    /// Build XML context for the system prompt listing available skills (metadata only).
    /// </summary>
    public string BuildSkillsContextXml()
    {
        List<CliSkill> enabled = GetEnabledSkills();
        if (enabled.Count == 0)
            return string.Empty;

        StringBuilder sb = new();
        sb.AppendLine("<available_skills>");

        foreach (CliSkill skill in enabled)
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
