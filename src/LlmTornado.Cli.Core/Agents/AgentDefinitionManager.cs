using System.Text;

namespace LlmTornado.Cli.Core.Agents;

/// <summary>
/// Manages the lifecycle of agent personas and project context:
/// discovery, selection, capability baseline application, and settings persistence.
/// </summary>
public sealed class AgentDefinitionManager
{
    private readonly AgentSettings _settings;
    private readonly ISettingsPersistence _persistence;

    private readonly Dictionary<string, AgentDefinition> _personas = new(StringComparer.OrdinalIgnoreCase);
    private AgentDefinition? _projectContext;

    private string? _activePersonaName;

    // Tool filtering state (computed when baseline is applied)
    private HashSet<string> _blockedTools = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _allowedToolsWhitelist = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasToolWhitelist;

    public string? ActivePersonaName => _activePersonaName;

    public AgentDefinitionManager(AgentSettings settings, ISettingsPersistence persistence)
    {
        _settings = settings;
        _persistence = persistence;
    }

    /// <summary>
    /// Load all agent definitions from filesystem.
    /// Call once at startup after SkillManager is initialized.
    /// </summary>
    public void LoadAll(string builtInDirectory, string customDirectory, string cwd)
    {
        LoadAll(builtInDirectory, null, customDirectory, cwd);
    }

    /// <summary>
    /// Load all agent definitions from filesystem, including an optional global directory.
    /// Call once at startup after SkillManager is initialized.
    /// Precedence: built-in → global → custom/project-local (most specific wins).
    /// </summary>
    public void LoadAll(string builtInDirectory, string? globalDirectory, string customDirectory, string cwd)
    {
        _personas.Clear();
        _projectContext = null;

        // 1. Discover persona agents (built-in + global + custom, each layer shadows previous)
        List<AgentDefinition> personas = AgentDefinitionLoader.DiscoverPersonaAgents(
            builtInDirectory, globalDirectory, customDirectory);
        foreach (AgentDefinition persona in personas)
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
            _persistence.SaveSettings(_settings);
        }
    }

    /// <summary>
    /// Set the active persona by name. Returns the definition, or null if not found.
    /// Does NOT apply the capability baseline — caller must call <see cref="ApplyCapabilityBaseline"/>.
    /// </summary>
    public AgentDefinition? SetActivePersona(string name)
    {
        if (!_personas.TryGetValue(name, out AgentDefinition? persona))
            return null;

        _activePersonaName = persona.Name; // use canonical casing from definition
        _settings.ActiveAgent = persona.Name;
        _persistence.SaveSettings(_settings);
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
        _persistence.SaveSettings(_settings);
    }

    public AgentDefinition? GetActivePersona()
    {
        if (_activePersonaName is null) return null;
        return _personas.GetValueOrDefault(_activePersonaName);
    }

    public AgentDefinition? GetProjectContext() => _projectContext;

    public List<AgentDefinition> GetAllPersonas() => [.. _personas.Values];

    public AgentDefinition? GetPersona(string name) =>
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
    public void ApplyCapabilityBaseline(Skills.SkillManager skillManager, IToolApproval toolApproval)
    {
        // Reset tool filtering state
        _blockedTools.Clear();
        _allowedToolsWhitelist.Clear();
        _hasToolWhitelist = false;

        AgentDefinition? persona = GetActivePersona();

        // Step 1: Reset ALL skills to enabled (clean slate)
        foreach (Skills.Skill skill in skillManager.GetAllSkills())
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
            foreach (Skills.Skill skill in skillManager.GetAllSkills())
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
        AgentDefinition? persona = GetActivePersona();
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

    /// <summary>
    /// Create a new custom agent .md file in the given directory and reload.
    /// </summary>
    public void CreateAgent(string agentsDirectory, string name, string description,
        string instructions, List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null)
    {
        string slug = name.ToLowerInvariant().Replace(' ', '-');
        string filePath = Path.Combine(agentsDirectory, $"{slug}.md");
        AgentDefinitionLoader.WriteAgentMd(filePath, name, description, instructions,
            enabledSkills, disabledSkills, enabledTools, disabledTools);
    }

    /// <summary>
    /// Update an existing custom agent .md file and reload.
    /// Returns false if the agent is not found or is not custom (i.e., built-in).
    /// </summary>
    public bool UpdateAgent(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null)
    {
        if (!_personas.TryGetValue(name, out AgentDefinition? existing))
            return false;

        if (existing.Source is AgentSource.BuiltIn)
            return false; // Cannot modify built-in agents

        AgentDefinitionLoader.WriteAgentMd(existing.FilePath, name, description, instructions,
            enabledSkills, disabledSkills, enabledTools, disabledTools);
        return true;
    }

    /// <summary>
    /// Delete a custom agent .md file. Returns false if not found or built-in.
    /// </summary>
    public bool DeleteAgent(string name)
    {
        if (!_personas.TryGetValue(name, out AgentDefinition? existing))
            return false;

        if (existing.Source is AgentSource.BuiltIn)
            return false;

        try
        {
            if (File.Exists(existing.FilePath))
                File.Delete(existing.FilePath);

            _personas.Remove(name);

            // If this was the active persona, clear it
            if (_activePersonaName is not null &&
                _activePersonaName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                ClearActivePersona();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
