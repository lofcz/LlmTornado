# Phase 4 — Extract Managers to Core

## Objective

Move `CliSkillManager` and `AgentDefinitionManager` to Core. These are the **moderately coupled** types — they call `CliStorage.SaveJson()` for persistence and depend on `ToolApprovalManager` for capability curation. The refactoring strategy is **dependency injection**: replace concrete static calls with the `ISettingsPersistence` and `IToolApproval` interfaces created in Phase 1.

## Why Inject Instead of Inherit?

The managers currently call `CliStorage.SaveJson(CliStorage.SettingsPath, _settings)` directly. This is a static method call to a CLI-specific class with hardcoded filesystem paths. The ACP server:

1. Doesn't persist settings to disk (stateless across restarts)
2. Doesn't have interactive tool approval (IDE handles its own UX)

Rather than making the managers abstract or introducing conditional logic, we inject behaviors through interfaces. The callers provide the implementation appropriate to their context.

---

## Types to Extract

### 4.1 `CliSkillManager` → `SkillManager`

**Source:** `LlmTornado.Cli/Skills/CliSkillManager.cs`  
**Target:** `LlmTornado.Cli.Core/Skills/SkillManager.cs`

**Current code has 3 calls to `CliStorage.SaveJson()`:**

```csharp
// In EnableSkill():
CliStorage.SaveJson(CliStorage.SettingsPath, _settings);

// In DisableSkill():
CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
```

**Refactoring: inject `ISettingsPersistence` via constructor:**

```csharp
namespace LlmTornado.Cli.Core.Skills;

public sealed class SkillManager
{
    private readonly AgentSettings _settings;
    private readonly ISettingsPersistence _persistence;
    private readonly Dictionary<string, SkillDefinition> _skills = new(StringComparer.OrdinalIgnoreCase);

    public SkillManager(AgentSettings settings, ISettingsPersistence persistence)
    {
        _settings = settings;
        _persistence = persistence;
    }

    // ...

    public bool EnableSkill(string name)
    {
        if (!_skills.TryGetValue(name, out SkillDefinition? skill))
            return false;

        skill.Enabled = true;
        _settings.DisabledSkills.Remove(name);
        _persistence.SaveSettings(_settings);  // was: CliStorage.SaveJson(...)
        return true;
    }

    public bool DisableSkill(string name)
    {
        if (!_skills.TryGetValue(name, out SkillDefinition? skill))
            return false;

        skill.Enabled = false;
        skill.Activated = false;
        _settings.DisabledSkills.Add(name);
        _persistence.SaveSettings(_settings);  // was: CliStorage.SaveJson(...)
        return true;
    }

    // ... rest unchanged ...
}
```

**Full diff summary:**

| Change | Before | After |
|--------|--------|-------|
| Class name | `CliSkillManager` | `SkillManager` |
| Namespace | `LlmTornado.Cli.Skills` | `LlmTornado.Cli.Core.Skills` |
| Access | `internal sealed` | `public sealed` |
| Constructor | `(CliSettings settings)` | `(AgentSettings settings, ISettingsPersistence persistence)` |
| Persistence calls | `CliStorage.SaveJson(CliStorage.SettingsPath, _settings)` | `_persistence.SaveSettings(_settings)` |
| Skill types | `CliSkill` | `SkillDefinition` |
| Loader calls | `CliSkillLoader.DiscoverSkills(...)` | `SkillLoader.DiscoverSkills(...)` |
| Loader calls | `CliSkillLoader.LoadInstructions(...)` | `SkillLoader.LoadInstructions(...)` |

**No behavior change.** Same discovery, same enable/disable, same XML context building. Only the persistence mechanism is pluggable.

---

### 4.2 `AgentDefinitionManager`

**Source:** `LlmTornado.Cli/Agents/AgentDefinitionManager.cs`  
**Target:** `LlmTornado.Cli.Core/Agents/AgentDefinitionManager.cs`

This is the more complex extraction. It has **3 coupling points:**

1. **`CliStorage.SaveJson()`** — 3 call sites (in `LoadAll`, `SetActivePersona`, `ClearActivePersona`)
2. **`ToolApprovalManager`** — used in `ApplyCapabilityBaseline()` for pre-approving tools
3. **`CliSkillManager`** — used in `ApplyCapabilityBaseline()` to enable/disable skills

**Refactoring strategy:**

| Coupling | Fix |
|----------|-----|
| `CliStorage.SaveJson()` | Replace with `ISettingsPersistence.SaveSettings()` |
| `ToolApprovalManager` | Replace with `IToolApproval` interface |
| `CliSkillManager` | Already becoming `SkillManager` — no interface needed, use directly |

**Before (CLI):**
```csharp
internal sealed class AgentDefinitionManager
{
    private readonly CliSettings _settings;

    public AgentDefinitionManager(CliSettings settings)
    {
        _settings = settings;
    }

    public void LoadAll(string builtInDir, string customDir, string cwd)
    {
        // ...
        if (_settings.ActiveAgent is not null)
        {
            _settings.ActiveAgent = null;
            CliStorage.SaveJson(CliStorage.SettingsPath, _settings);  // <-- coupling
        }
    }

    public CliAgentDefinition? SetActivePersona(string name)
    {
        // ...
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);  // <-- coupling
        return persona;
    }

    public void ClearActivePersona()
    {
        // ...
        CliStorage.SaveJson(CliStorage.SettingsPath, _settings);  // <-- coupling
    }

    public void ApplyCapabilityBaseline(CliSkillManager skillManager, ToolApprovalManager toolApproval)
    {
        // ...
        if (persona.AutoApproveTools.Count > 0)
            toolApproval.PreApproveSkillTools(persona.AutoApproveTools);  // <-- coupling
    }
}
```

**After (Core):**
```csharp
namespace LlmTornado.Cli.Core.Agents;

public sealed class AgentDefinitionManager
{
    private readonly AgentSettings _settings;
    private readonly ISettingsPersistence _persistence;

    private readonly Dictionary<string, AgentDefinition> _personas = new(StringComparer.OrdinalIgnoreCase);
    private AgentDefinition? _projectContext;
    private string? _activePersonaName;

    // Tool filtering state
    private HashSet<string> _blockedTools = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _allowedToolsWhitelist = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasToolWhitelist;

    public string? ActivePersonaName => _activePersonaName;

    public AgentDefinitionManager(AgentSettings settings, ISettingsPersistence persistence)
    {
        _settings = settings;
        _persistence = persistence;
    }

    public void LoadAll(string builtInDirectory, string customDirectory, string cwd)
    {
        _personas.Clear();
        _projectContext = null;

        List<AgentDefinition> personas = AgentDefinitionLoader.DiscoverPersonaAgents(
            builtInDirectory, customDirectory);
        foreach (AgentDefinition persona in personas)
            _personas[persona.Name] = persona;

        if (_settings.ProjectAgentsEnabled)
            _projectContext = AgentDefinitionLoader.DiscoverProjectAgents(cwd);

        if (_settings.ActiveAgent is not null && _personas.ContainsKey(_settings.ActiveAgent))
        {
            _activePersonaName = _settings.ActiveAgent;
        }
        else if (_settings.ActiveAgent is not null)
        {
            _settings.ActiveAgent = null;
            _persistence.SaveSettings(_settings);  // <-- now injected
        }
    }

    public AgentDefinition? SetActivePersona(string name)
    {
        if (!_personas.TryGetValue(name, out AgentDefinition? persona))
            return null;

        _activePersonaName = persona.Name;
        _settings.ActiveAgent = persona.Name;
        _persistence.SaveSettings(_settings);  // <-- now injected
        return persona;
    }

    public void ClearActivePersona()
    {
        _activePersonaName = null;
        _settings.ActiveAgent = null;
        _blockedTools.Clear();
        _allowedToolsWhitelist.Clear();
        _hasToolWhitelist = false;
        _persistence.SaveSettings(_settings);  // <-- now injected
    }

    /// <summary>
    /// Apply the active persona's skill/tool curation as the baseline state.
    /// </summary>
    public void ApplyCapabilityBaseline(SkillManager skillManager, IToolApproval toolApproval)
    {
        _blockedTools.Clear();
        _allowedToolsWhitelist.Clear();
        _hasToolWhitelist = false;

        AgentDefinition? persona = GetActivePersona();

        // Step 1: Reset ALL skills to enabled
        foreach (SkillDefinition skill in skillManager.GetAllSkills())
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
            foreach (SkillDefinition skill in skillManager.GetAllSkills())
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
            toolApproval.PreApproveTools(persona.AutoApproveTools);  // <-- now via interface
    }

    // IsToolAllowed, BuildInstructionsBlock, GetActivePersona, etc. — unchanged
    // (just rename CliAgentDefinition → AgentDefinition, CliSkill → SkillDefinition)
}
```

**Key observation:** The `ApplyCapabilityBaseline` method signature changes from:
```csharp
// Before
void ApplyCapabilityBaseline(CliSkillManager skillManager, ToolApprovalManager toolApproval)

// After  
void ApplyCapabilityBaseline(SkillManager skillManager, IToolApproval toolApproval)
```

This is the critical decoupling point. The CLI's `ToolApprovalManager` will implement `IToolApproval` (it already has the `PreApproveSkillTools` method — just rename to `PreApproveTools`). The ACP server provides a no-op implementation.

---

## `IToolApproval` Implementations

### For CLI (stays in `LlmTornado.Cli`):

The existing `ToolApprovalManager` already has the right methods. Just implement the interface:

```csharp
// In LlmTornado.Cli/ToolApprovalManager.cs — add interface implementation
internal sealed class ToolApprovalManager : IToolApproval
{
    // Existing code unchanged...
    
    // Rename: PreApproveSkillTools → PreApproveTools (matches interface)
    public void PreApproveTools(IEnumerable<string> toolNames)
    {
        foreach (string name in toolNames)
            _approvals.TryAdd(name, ToolApprovalState.AlwaysAllow);
        SaveToDisk();
    }

    public bool IsAutoApproved(string toolName)
    {
        return _approvals.TryGetValue(toolName, out ToolApprovalState state) 
               && state == ToolApprovalState.AlwaysAllow;
    }

    // HandleToolPermissionRequest already exists with correct signature
}
```

### For ACP Server (created in Phase 7):

```csharp
// In LlmTornado.Acp.Server — auto-approve everything
internal sealed class AutoApproveToolApproval : IToolApproval
{
    public void PreApproveTools(IEnumerable<string> toolNames) { }
    public bool IsAutoApproved(string toolName) => true;
    public ValueTask<bool> HandleToolPermissionRequest(string requestMessage) 
        => ValueTask.FromResult(true);
}
```

---

## `ISettingsPersistence` Implementations

### For CLI (stays in `LlmTornado.Cli`):

```csharp
// In LlmTornado.Cli — wraps CliStorage
internal sealed class DiskSettingsPersistence : ISettingsPersistence
{
    public void SaveSettings(AgentSettings settings)
    {
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
    }
}
```

### For ACP Server (created in Phase 7):

```csharp
// In LlmTornado.Acp.Server — in-memory no-op
internal sealed class NoOpSettingsPersistence : ISettingsPersistence
{
    public void SaveSettings(AgentSettings settings) { }
}
```

---

## Verification

```powershell
cd src
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
```

Dependencies at this point:
- `AgentSettings` (Phase 2)
- `AgentDefinition`, `AgentDefinitionLoader` (Phase 2-3)
- `SkillDefinition`, `SkillLoader` (Phase 2-3)
- `ISettingsPersistence`, `IToolApproval` (Phase 1)

All are within the Core project — no circular dependencies.

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Cli.Core/Skills/SkillManager.cs` | Create (from CliSkillManager, inject persistence) |
| `LlmTornado.Cli.Core/Agents/AgentDefinitionManager.cs` | Create (from CLI, inject persistence + IToolApproval) |
| `LlmTornado.Cli.Core/IToolApproval.cs` | Update (finalize interface, if not done in Phase 1) |
