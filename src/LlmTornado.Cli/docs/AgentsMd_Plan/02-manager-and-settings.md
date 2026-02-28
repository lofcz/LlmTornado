# Phase 2: Manager & Settings

## Goal

Implement the `AgentDefinitionManager` that owns the lifecycle of agent personas and project context — discovery, selection, capability baseline application, and settings persistence. Extend `CliSettings` with the new agent-related fields.

---

## Files to Create/Modify

### Create: `src/LlmTornado.Cli/Agents/AgentDefinitionManager.cs`
### Modify: `src/LlmTornado.Cli/CliSettings.cs`

---

## CliSettings Extensions

Add three new fields to the existing `CliSettings` class:

```csharp
internal sealed class CliSettings
{
    // ... existing fields ...

    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("disabled_skills")]
    public HashSet<string> DisabledSkills { get; set; } = [];

    [JsonPropertyName("skills_directory")]
    public string? SkillsDirectory { get; set; }

    [JsonPropertyName("mcp_config_path")]
    public string? McpConfigPath { get; set; }

    [JsonPropertyName("max_turns_before_summary")]
    public int MaxTurnsBeforeSummary { get; set; }

    // --- NEW FIELDS ---

    /// <summary>
    /// Currently selected agent persona name. Null = default (no persona).
    /// Persisted across sessions. Restored on startup if the persona still exists.
    /// </summary>
    [JsonPropertyName("active_agent")]
    public string? ActiveAgent { get; set; }

    /// <summary>
    /// Custom path to the agents directory for persona discovery.
    /// Null = use default ./agents/ relative to CWD.
    /// </summary>
    [JsonPropertyName("agents_directory")]
    public string? AgentsDirectory { get; set; }

    /// <summary>
    /// Whether to auto-detect and inject project AGENTS.md files from the CWD hierarchy.
    /// Default: true. Can be toggled via /agent project on|off.
    /// </summary>
    [JsonPropertyName("project_agents_enabled")]
    public bool ProjectAgentsEnabled { get; set; } = true;
}
```

### Settings JSON Example

After a user runs `/agent set code-reviewer` and `/agent project off`:

```json
{
  "active_model": "claude-sonnet-4-20250514",
  "disabled_skills": [],
  "skills_directory": null,
  "mcp_config_path": null,
  "max_turns_before_summary": 0,
  "active_agent": "code-reviewer",
  "agents_directory": null,
  "project_agents_enabled": false
}
```

### Migration Note

Existing `settings.json` files without the new fields will deserialize cleanly because:
- `ActiveAgent` defaults to `null` (no persona)
- `AgentsDirectory` defaults to `null` (use default path)
- `ProjectAgentsEnabled` defaults to `true` (enabled)

System.Text.Json ignores unknown fields during deserialization by default, so old settings files work without migration.

---

## AgentDefinitionManager — Lifecycle Manager

```csharp
namespace LlmTornado.Cli.Agents;

using LlmTornado.Cli.Skills;

/// <summary>
/// Manages the lifecycle of agent personas and project context:
/// - Discovery of persona agents (built-in + custom) and project AGENTS.md
/// - Active persona selection with persistence
/// - Capability baseline application (skill/tool curation)
/// - System prompt context assembly
/// 
/// This class coordinates between AgentDefinitionLoader (parsing),
/// CliSkillManager (skill state), and ToolApprovalManager (tool approval).
/// It does NOT own the rebuild trigger — that's CliAgentBuilder's responsibility.
/// </summary>
internal sealed class AgentDefinitionManager
{
    private readonly CliSettings _settings;

    // Discovered agents
    private readonly Dictionary<string, CliAgentDefinition> _personas = new(StringComparer.OrdinalIgnoreCase);
    private CliAgentDefinition? _projectContext;

    // Active state
    private string? _activePersonaName;

    // Tool filtering state (computed when baseline is applied)
    private HashSet<string> _blockedTools = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _allowedToolsWhitelist = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasToolWhitelist;

    public AgentDefinitionManager(CliSettings settings)
    {
        _settings = settings;
    }

    // --- Discovery ---

    /// <summary>
    /// Load all agent definitions from filesystem.
    /// Call once at startup after CliSkillManager is initialized.
    /// </summary>
    public void LoadAll(string builtInDirectory, string customDirectory, string cwd);

    // --- Persona Selection ---

    /// <summary>
    /// Set the active persona by name. Returns the definition, or null if not found.
    /// Does NOT apply the capability baseline — caller must call ApplyCapabilityBaseline().
    /// Persists selection to settings.
    /// </summary>
    public CliAgentDefinition? SetActivePersona(string name);

    /// <summary>
    /// Clear the active persona (revert to default — all capabilities available).
    /// Persists to settings.
    /// </summary>
    public void ClearActivePersona();

    // --- Queries ---

    public CliAgentDefinition? GetActivePersona();
    public CliAgentDefinition? GetProjectContext();
    public List<CliAgentDefinition> GetAllPersonas();
    public CliAgentDefinition? GetPersona(string name);
    public string? ActivePersonaName => _activePersonaName;

    // --- Capability Baseline ---

    /// <summary>
    /// Apply the active persona's skill/tool curation as the baseline state.
    /// Resets all skills to enabled, then applies whitelist/blacklist.
    /// Call after SetActivePersona() or ClearActivePersona().
    /// </summary>
    public void ApplyCapabilityBaseline(CliSkillManager skillManager, ToolApprovalManager toolApproval);

    /// <summary>
    /// Check if a specific tool is allowed by the active persona.
    /// Used by CliAgentBuilder.CollectTools() to filter the tool list.
    /// Returns true if no persona is active or if the tool passes filtering.
    /// </summary>
    public bool IsToolAllowed(string toolName);

    // --- System Prompt ---

    /// <summary>
    /// Build the combined instructions block for injection into the system prompt.
    /// Includes active persona instructions + project AGENTS.md context.
    /// Returns empty string if neither is available.
    /// </summary>
    public string BuildInstructionsBlock();
}
```

---

### LoadAll — Discovery at Startup

```csharp
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
        _activePersonaName = _settings.ActiveAgent;
    else if (_settings.ActiveAgent is not null)
    {
        // Saved persona no longer exists — clear it
        _settings.ActiveAgent = null;
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
    }
}
```

**Startup sequence integration** (performed in `Program.cs` after skill loading):
```
Step 4:  Skills loaded → CliSkillManager populated
Step 4b: Agents loaded → AgentDefinitionManager.LoadAll()
         If active_agent in settings → restore it
Step 5:  MCP loaded
...
Step 8:  Agent built → CliAgentBuilder.Build()
         (internally calls manager.ApplyCapabilityBaseline() + BuildInstructionsBlock())
```

---

### SetActivePersona / ClearActivePersona

```csharp
public CliAgentDefinition? SetActivePersona(string name)
{
    if (!_personas.TryGetValue(name, out CliAgentDefinition? persona))
        return null;

    _activePersonaName = name;
    _settings.ActiveAgent = name;
    CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
    return persona;
}

public void ClearActivePersona()
{
    _activePersonaName = null;
    _settings.ActiveAgent = null;
    _blockedTools.Clear();
    _allowedToolsWhitelist.Clear();
    _hasToolWhitelist = false;
    CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
}
```

**Important**: These methods only update state and persist. They do NOT apply the capability baseline or rebuild the agent. The caller (typically `AgentCommand` or `Program.cs`) must:
1. Call `SetActivePersona()` or `ClearActivePersona()`
2. Call `ApplyCapabilityBaseline(skillManager, toolApproval)`
3. Call `agentBuilder.RebuildForAgentChange()`

This separation keeps concerns clean: the manager handles state, the builder handles construction.

---

### ApplyCapabilityBaseline — The Core Curation Logic

This is the key method that enforces an agent persona's skill/tool preferences:

```csharp
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
        return; // No persona or no curation — all capabilities available

    // Step 2: Apply skill whitelist (if non-empty, only these skills stay enabled)
    if (persona.EnabledSkills.Count > 0)
    {
        HashSet<string> whitelist = new(persona.EnabledSkills, StringComparer.OrdinalIgnoreCase);
        foreach (CliSkill skill in skillManager.GetAllSkills())
        {
            if (!whitelist.Contains(skill.Name))
                skillManager.DisableSkill(skill.Name);
        }
    }

    // Step 3: Apply skill blacklist (force-disable these skills)
    foreach (string skillName in persona.DisabledSkills)
        skillManager.DisableSkill(skillName);

    // Step 4: Compute tool filtering state (used by IsToolAllowed)
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
```

### ExecutionFlow: Baseline → Override → Rebuild

```
/agent set code-reviewer
  │
  ├─ AgentDefinitionManager.SetActivePersona("code-reviewer")
  │   → _activePersonaName = "code-reviewer"
  │   → settings.ActiveAgent = "code-reviewer" → saved to disk
  │
  ├─ AgentDefinitionManager.ApplyCapabilityBaseline(skillManager, toolApproval)
  │   │
  │   ├─ Reset: all skills → enabled
  │   ├─ Whitelist: enabled-skills: [file-analyzer]
  │   │   → file-analyzer stays enabled
  │   │   → web-search → disabled
  │   │   → note-taker → disabled
  │   ├─ Blacklist: disabled-skills: [] (empty)
  │   ├─ Tool state: disabled-tools: [web-search:ddg-search, web-search:fetch-url]
  │   │   → _blockedTools = {web-search:ddg-search, web-search:fetch-url}
  │   └─ Auto-approve: [file-analyzer:line-count, file-analyzer:find-todos]
  │       → toolApproval.PreApproveSkillTools(...)
  │
  └─ CliAgentBuilder.RebuildForAgentChange(runtimeEventHandler)
      → Build()
        ├─ BuildSystemPrompt() → includes persona instructions
        ├─ CollectTools() → filters out blocked tools via IsToolAllowed()
        └─ Creates new TornadoAgent + ChatRuntime

--- User session override ---

/skill enable web-search
  │
  ├─ CliSkillManager.EnableSkill("web-search")
  │   → web-search.Enabled = true (overrides agent baseline)
  │   → settings persisted
  │
  └─ CliAgentBuilder.RebuildForSkillChange(runtimeEventHandler)
      → Build()
        ├─ System prompt includes web-search in <available_skills>
        └─ web-search script tools now registered (but still filtered
           by _blockedTools — individual tools may still be blocked)

--- Next agent switch resets everything ---

/agent set debugger
  → All capabilities reset to debugger's baseline
  → User's web-search override is gone
```

---

### IsToolAllowed — Tool Filtering Predicate

```csharp
public bool IsToolAllowed(string toolName)
{
    // Built-in agent management tools are always allowed
    if (toolName is "load_skill" or "list_skills" or "read_reference")
        return true;

    // No persona or no tool curation → all tools allowed
    if (_activePersonaName is null) return true;
    if (!_hasToolWhitelist && _blockedTools.Count == 0) return true;

    // Check whitelist first (if active, tool must be in it)
    if (_hasToolWhitelist && !_allowedToolsWhitelist.Contains(toolName))
        return false;

    // Check blacklist
    if (_blockedTools.Contains(toolName))
        return false;

    return true;
}
```

**Called by `CliAgentBuilder.CollectTools()`** — see [Phase 3: Builder Integration](03-builder-integration.md).

---

### BuildInstructionsBlock — System Prompt Content

```csharp
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
```

**Why XML tags?** The existing system already uses XML for the skill catalog (`<available_skills>`). Using `<agent_persona>` and `<project_context>` tags provides clear delimiters that help the LLM understand the structure and precedence of different instruction sources. The model can distinguish between "who I am" (persona), "what project I'm working on" (project context), and "what tools I have" (skills).

---

## State Management

### Persisted State (survives CLI restart)

| Field | Storage | Set by |
|-------|---------|--------|
| `active_agent` | `settings.json` | `/agent set <name>`, `/agent clear` |
| `agents_directory` | `settings.json` | Manual edit (future: `/agent config` command) |
| `project_agents_enabled` | `settings.json` | `/agent project on\|off` |

### Session State (reset on agent switch)

| State | Owner | Reset trigger |
|-------|-------|---------------|
| Skill enable/disable overrides | `CliSkillManager` | `ApplyCapabilityBaseline()` resets all, then applies persona baseline |
| Tool blocking sets | `AgentDefinitionManager._blockedTools` | `ApplyCapabilityBaseline()` recomputes |
| Tool auto-approvals | `ToolApprovalManager` | Not reset (approvals accumulate intentionally) |

### Why tool approvals don't reset:

The `ToolApprovalManager` uses a `TryAdd` pattern — it only sets approval if no decision exists yet. This means:
- If the user manually denied a tool, switching agents won't re-approve it
- Auto-approve from a persona only applies to tools without existing decisions
- This is intentional: user's explicit approval/denial decisions should be sticky

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| `SetActivePersona()` with unknown name | Returns `null`, no state change |
| Active persona references non-existent skill in `enabled-skills` | Silently ignored during `ApplyCapabilityBaseline()` — only existing skills affected |
| Active persona references non-existent tool in `disabled-tools` | Stored in `_blockedTools`, `IsToolAllowed()` returns false for it — harmless if tool doesn't exist |
| Settings file has `active_agent: "deleted-agent"` | `LoadAll()` detects it's gone, clears `active_agent`, saves settings |
| Multiple calls to `ApplyCapabilityBaseline()` | Idempotent — always resets to clean slate first |

---

## Interaction with Existing `/skill` Command

The existing `SkillCommand` calls `CliSkillManager.EnableSkill()`/`DisableSkill()` directly. These still work after an agent sets the baseline:

```
Agent baseline: file-analyzer=enabled, web-search=disabled, note-taker=disabled

/skill enable web-search
  → CliSkillManager.EnableSkill("web-search") → true
  → CliAgentBuilder.RebuildForSkillChange() → web-search added to system prompt
  → User's override is active until next /agent set or /agent clear

/skill disable file-analyzer
  → CliSkillManager.DisableSkill("file-analyzer") → true
  → CliAgentBuilder.RebuildForSkillChange() → file-analyzer removed
  → User has now disabled the agent's only whitelisted skill

/agent set code-reviewer  (or /agent clear)
  → ApplyCapabilityBaseline() → resets everything to baseline
  → User's overrides are gone
```

This is the intended "agent sets baseline, user overrides per-session" model. The user always has the last say within a session.
