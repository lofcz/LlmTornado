using System.Text;
using LlmTornado.Cli.Skills;

namespace LlmTornado.Cli.Agents;

/// <summary>
/// Manages the lifecycle of agent personas and project context:
/// discovery, selection, capability baseline application, and settings persistence.
/// </summary>
internal sealed class AgentDefinitionManager
{
    private readonly CliSettings _settings;

    private readonly Dictionary<string, CliAgentDefinition> _personas = new(StringComparer.OrdinalIgnoreCase);
    private CliAgentDefinition? _projectContext;

    private string? _activePersonaName;

    // Tool filtering state (computed when baseline is applied)
    private HashSet<string> _blockedTools = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _allowedToolsWhitelist = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasToolWhitelist;

    public string? ActivePersonaName => _activePersonaName;

    public AgentDefinitionManager(CliSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Load all agent definitions from filesystem.
    /// Call once at startup after CliSkillManager is initialized.
    /// </summary>
    public void LoadAll(string builtInDirectory, string customDirectory, string cwd)
    {
        _personas.Clear();
        _projectContext = null;

        // 1. Discover persona agents (built-in + custom, custom shadows built-in)
        List<CliAgentDefinition> personas = AgentDefinitionLoader.DiscoverPersonaAgents(
            builtInDirectory, customDirectory);
        foreach (CliAgentDefinition persona in personas)
            _personas[persona.Name] = persona;

        // 2. Discover project AGENTS.md from CWD hierarchy
        if (_settings.ProjectAgentsEnabled)
            _projectContext = AgentDefinitionLoader.DiscoverProjectAgents(cwd);

        // 3. Restore active persona from settings (if it still exists)
        if (_settings.ActiveAgent is not null && _personas.ContainsKey(_settings.ActiveAgent))
        {
            _activePersonaName = _settings.ActiveAgent;
        }
        else if (_settings.ActiveAgent is not null)
        {
            // Saved persona no longer exists — clear it
            _settings.ActiveAgent = null;
            CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
        }
    }

    /// <summary>
    /// Set the active persona by name. Returns the definition, or null if not found.
    /// Does NOT apply the capability baseline — caller must call <see cref="ApplyCapabilityBaseline"/>.
    /// </summary>
    public CliAgentDefinition? SetActivePersona(string name)
    {
        if (!_personas.TryGetValue(name, out CliAgentDefinition? persona))
            return null;

        _activePersonaName = persona.Name; // use canonical casing from definition
        _settings.ActiveAgent = persona.Name;
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
        return persona;
    }

    /// <summary>
    /// Clear the active persona (revert to default — all capabilities available).
    /// </summary>
    public void ClearActivePersona()
    {
        _activePersonaName = null;
        _settings.ActiveAgent = null;
        _blockedTools.Clear();
        _allowedToolsWhitelist.Clear();
        _hasToolWhitelist = false;
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
    }

    public CliAgentDefinition? GetActivePersona()
    {
        if (_activePersonaName is null) return null;
        return _personas.GetValueOrDefault(_activePersonaName);
    }

    public CliAgentDefinition? GetProjectContext() => _projectContext;

    public List<CliAgentDefinition> GetAllPersonas() => [.. _personas.Values];

    public CliAgentDefinition? GetPersona(string name) =>
        _personas.GetValueOrDefault(name);

    /// <summary>
    /// Re-scan for project AGENTS.md from the given CWD.
    /// </summary>
    public void RefreshProjectContext(string cwd)
    {
        _projectContext = _settings.ProjectAgentsEnabled
            ? AgentDefinitionLoader.DiscoverProjectAgents(cwd)
            : null;
    }

    /// <summary>
    /// Apply the active persona's skill/tool curation as the baseline state.
    /// Resets all skills to enabled, then applies whitelist/blacklist.
    /// </summary>
    public void ApplyCapabilityBaseline(CliSkillManager skillManager, ToolApprovalManager toolApproval)
    {
        // Reset tool filtering state
        _blockedTools.Clear();
        _allowedToolsWhitelist.Clear();
        _hasToolWhitelist = false;

        CliAgentDefinition? persona = GetActivePersona();

        // Step 1: Reset ALL skills to enabled (clean slate)
        foreach (CliSkill skill in skillManager.GetAllSkills())
        {
            if (!skill.Enabled)
                skillManager.EnableSkill(skill.Name);
        }

        if (persona is null || !persona.HasCapabilityCuration)
            return;

        // Step 2: Apply skill whitelist
        if (persona.EnabledSkills.Count > 0)
        {
            HashSet<string> whitelist = new(persona.EnabledSkills, StringComparer.OrdinalIgnoreCase);
            foreach (CliSkill skill in skillManager.GetAllSkills())
            {
                if (!whitelist.Contains(skill.Name))
                    skillManager.DisableSkill(skill.Name);
            }
        }

        // Step 3: Apply skill blacklist
        foreach (string skillName in persona.DisabledSkills)
            skillManager.DisableSkill(skillName);

        // Step 4: Compute tool filtering state
        if (persona.EnabledTools.Count > 0)
        {
            _hasToolWhitelist = true;
            _allowedToolsWhitelist = new HashSet<string>(
                persona.EnabledTools, StringComparer.OrdinalIgnoreCase);
        }
        _blockedTools = new HashSet<string>(
            persona.DisabledTools, StringComparer.OrdinalIgnoreCase);

        // Step 5: Pre-approve tools specified by the persona
        if (persona.AutoApproveTools.Count > 0)
            toolApproval.PreApproveSkillTools(persona.AutoApproveTools);
    }

    /// <summary>
    /// Check if a specific tool is allowed by the active persona.
    /// Returns true if no persona is active or if the tool passes filtering.
    /// </summary>
    public bool IsToolAllowed(string toolName)
    {
        // Built-in agent management tools are always allowed
        if (toolName is "load_skill" or "list_skills" or "read_reference")
            return true;

        if (_activePersonaName is null) return true;
        if (!_hasToolWhitelist && _blockedTools.Count == 0) return true;

        // Check whitelist first
        if (_hasToolWhitelist && !_allowedToolsWhitelist.Contains(toolName))
            return false;

        // Check blacklist
        if (_blockedTools.Contains(toolName))
            return false;

        return true;
    }

    /// <summary>
    /// Build the combined instructions block for injection into the system prompt.
    /// </summary>
    public string BuildInstructionsBlock()
    {
        StringBuilder sb = new();

        // 1. Active persona instructions
        CliAgentDefinition? persona = GetActivePersona();
        if (persona is not null && !string.IsNullOrWhiteSpace(persona.Instructions))
        {
            sb.AppendLine("<agent_persona>");
            sb.AppendLine(persona.Instructions);
            sb.AppendLine("</agent_persona>");
            sb.AppendLine();
        }

        // 2. Project AGENTS.md context
        if (_projectContext is not null && _settings.ProjectAgentsEnabled)
        {
            sb.AppendLine("<project_context>");
            sb.AppendLine(_projectContext.Instructions);
            sb.AppendLine("</project_context>");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
