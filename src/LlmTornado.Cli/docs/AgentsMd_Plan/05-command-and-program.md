# Phase 5: Command & Program Wiring

## Goal

Create the `/agent` slash command for agent persona management, wire `AgentDefinitionManager` into the `Program.cs` initialization pipeline, and update the existing `/skill` and `/tools` commands to surface agent-imposed capability state.

---

## Files to Create/Modify

### Create: `src/LlmTornado.Cli/Commands/AgentCommand.cs`
### Modify: `src/LlmTornado.Cli/Program.cs`
### Modify: `src/LlmTornado.Cli/Commands/SkillCommand.cs`
### Modify: `src/LlmTornado.Cli/Commands/ToolsCommand.cs`

---

## AgentCommand — `/agent` Slash Command

```csharp
using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Agents;
using LlmTornado.Cli.Skills;

namespace LlmTornado.Cli.Commands;

internal sealed class AgentCommand : ICliCommand
{
    public string Name => "agent";
    public string Description => "Manage agent personas (list, set, clear, info, project)";
    public string Usage => "/agent [list | set <name> | clear | info <name> | project [on|off]]";

    private readonly AgentDefinitionManager _agentManager;
    private readonly CliSkillManager _skillManager;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public AgentCommand(
        AgentDefinitionManager agentManager,
        CliSkillManager skillManager,
        CliAgentBuilder builder,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _agentManager = agentManager;
        _skillManager = skillManager;
        _builder = builder;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public Task<bool> ExecuteAsync(string[] args);
}
```

### Subcommand Implementation

#### `/agent` (no args) — Status Summary

```csharp
if (args.Length == 0)
{
    string activeName = _agentManager.ActivePersonaName ?? "default (none)";
    CliAgentDefinition? active = _agentManager.GetActivePersona();
    CliAgentDefinition? project = _agentManager.GetProjectContext();

    ConsoleRenderer.WriteInfo($"Active agent: {activeName}");

    if (active is not null)
    {
        ConsoleRenderer.WriteInfo($"  Source:  {active.Source}");
        if (active.EnabledSkills.Count > 0)
            ConsoleRenderer.WriteInfo($"  Skills:  {string.Join(", ", active.EnabledSkills)}");
        if (active.DisabledTools.Count > 0)
            ConsoleRenderer.WriteInfo($"  Blocked: {string.Join(", ", active.DisabledTools)}");
    }

    ConsoleRenderer.WriteInfo($"Project AGENTS.md: {(project is not null ? "detected" : "not found")}");
    ConsoleRenderer.WriteInfo($"Total personas: {_agentManager.GetAllPersonas().Count}");
    return Task.FromResult(true);
}
```

**Example output:**
```
Active agent: code-reviewer
  Source:  BuiltIn
  Skills:  file-analyzer
  Blocked: note-taker:add-note, note-taker:delete-note, ...
Project AGENTS.md: detected
Total personas: 5
```

#### `/agent list` — List All Personas

```csharp
case "list":
    List<CliAgentDefinition> agents = _agentManager.GetAllPersonas();
    if (agents.Count == 0)
    {
        ConsoleRenderer.WriteInfo("No agent personas found.");
        break;
    }

    foreach (CliAgentDefinition agent in agents)
    {
        string marker = agent.Name == _agentManager.ActivePersonaName ? "→ " : "  ";
        string sourceTag = agent.Source switch
        {
            AgentSource.BuiltIn => "[built-in]",
            AgentSource.Custom => "[custom]",
            _ => ""
        };

        string curation = "";
        if (agent.HasCapabilityCuration)
        {
            List<string> parts = [];
            if (agent.EnabledSkills.Count > 0)
                parts.Add($"{agent.EnabledSkills.Count} skills");
            if (agent.DisabledTools.Count > 0)
                parts.Add($"{agent.DisabledTools.Count} blocked tools");
            if (parts.Count > 0)
                curation = $" ({string.Join(", ", parts)})";
        }

        ConsoleRenderer.WriteInfo(
            $"{marker}{agent.Name,-20} {sourceTag,-12} {agent.Description}{curation}");
    }
    break;
```

**Example output:**
```
→ code-reviewer       [built-in]   Focused code review agent emphasizing security, quality, and best practices (1 skills, 5 blocked tools)
  debugger             [built-in]   Systematic debugging agent using hypothesis-driven investigation (2 skills)
  docs-writer          [built-in]   Documentation agent that writes clear, audience-aware technical documentation (2 skills)
  architect            [built-in]   Software architecture agent focused on design, trade-offs, and system structure (2 skills)
  default              [built-in]   General-purpose assistant with all skills and tools available
```

#### `/agent set <name>` — Select Persona

```csharp
case "set" when args.Length >= 2:
    string targetName = args[1];
    CliAgentDefinition? selected = _agentManager.SetActivePersona(targetName);
    if (selected is null)
    {
        ConsoleRenderer.WriteError($"Agent '{targetName}' not found. Use /agent list to see available agents.");
        break;
    }

    // Apply baseline and rebuild
    _builder.RebuildForAgentChange(_runtimeEventHandler);

    // Summary output
    List<CliSkill> enabledSkills = _skillManager.GetEnabledSkills();
    List<CliSkill> allSkills = _skillManager.GetAllSkills();
    ConsoleRenderer.WriteSuccess(
        $"Activated agent: {selected.Name} ({enabledSkills.Count}/{allSkills.Count} skills enabled)");

    if (selected.HasCapabilityCuration)
    {
        if (selected.EnabledSkills.Count > 0)
            ConsoleRenderer.WriteInfo($"  Enabled skills: {string.Join(", ", selected.EnabledSkills)}");
        if (selected.DisabledSkills.Count > 0)
            ConsoleRenderer.WriteInfo($"  Disabled skills: {string.Join(", ", selected.DisabledSkills)}");
        if (selected.DisabledTools.Count > 0)
            ConsoleRenderer.WriteInfo($"  Blocked tools: {string.Join(", ", selected.DisabledTools)}");
        if (selected.AutoApproveTools.Count > 0)
            ConsoleRenderer.WriteInfo($"  Auto-approved: {string.Join(", ", selected.AutoApproveTools)}");
    }
    break;
```

**Example output:**
```
✓ Activated agent: code-reviewer (1/3 skills enabled)
  Enabled skills: file-analyzer
  Blocked tools: note-taker:add-note, note-taker:delete-note, note-taker:search-notes, note-taker:list-notes, note-taker:view-note
  Auto-approved: file-analyzer:line-count, file-analyzer:find-todos, file-analyzer:tree-summary
```

#### `/agent clear` — Revert to Default

```csharp
case "clear":
    _agentManager.ClearActivePersona();
    _builder.RebuildForAgentChange(_runtimeEventHandler);

    List<CliSkill> nowEnabled = _skillManager.GetEnabledSkills();
    List<CliSkill> nowAll = _skillManager.GetAllSkills();
    ConsoleRenderer.WriteSuccess(
        $"Agent cleared. All capabilities restored ({nowEnabled.Count}/{nowAll.Count} skills enabled).");
    break;
```

**Example output:**
```
✓ Agent cleared. All capabilities restored (3/3 skills enabled).
```

#### `/agent info <name>` — Detailed Information

```csharp
case "info" when args.Length >= 2:
    CliAgentDefinition? info = _agentManager.GetPersona(args[1]);
    if (info is null)
    {
        ConsoleRenderer.WriteError($"Agent '{args[1]}' not found.");
        break;
    }

    ConsoleRenderer.WriteInfo($"  Name:            {info.Name}");
    ConsoleRenderer.WriteInfo($"  Description:     {info.Description}");
    ConsoleRenderer.WriteInfo($"  Source:           {info.Source}");
    ConsoleRenderer.WriteInfo($"  File:            {info.FilePath}");
    ConsoleRenderer.WriteInfo($"  Active:          {(info.Name == _agentManager.ActivePersonaName ? "yes" : "no")}");
    ConsoleRenderer.WriteInfo($"  Has curation:    {info.HasCapabilityCuration}");

    if (info.EnabledSkills.Count > 0)
        ConsoleRenderer.WriteInfo($"  Enabled skills:  {string.Join(", ", info.EnabledSkills)}");
    if (info.DisabledSkills.Count > 0)
        ConsoleRenderer.WriteInfo($"  Disabled skills: {string.Join(", ", info.DisabledSkills)}");
    if (info.EnabledTools.Count > 0)
        ConsoleRenderer.WriteInfo($"  Enabled tools:   {string.Join(", ", info.EnabledTools)}");
    if (info.DisabledTools.Count > 0)
        ConsoleRenderer.WriteInfo($"  Disabled tools:  {string.Join(", ", info.DisabledTools)}");
    if (info.AutoApproveTools.Count > 0)
        ConsoleRenderer.WriteInfo($"  Auto-approve:    {string.Join(", ", info.AutoApproveTools)}");

    // Show instruction preview (first 10 lines)
    if (!string.IsNullOrWhiteSpace(info.Instructions))
    {
        ConsoleRenderer.WriteInfo("  Instructions:");
        string[] lines = info.Instructions.Split('\n');
        int previewLines = Math.Min(10, lines.Length);
        for (int i = 0; i < previewLines; i++)
            ConsoleRenderer.WriteInfo($"    {lines[i].TrimEnd()}");
        if (lines.Length > previewLines)
            ConsoleRenderer.WriteInfo($"    ... ({lines.Length - previewLines} more lines)");
    }
    break;
```

**Example output:**
```
  Name:            code-reviewer
  Description:     Focused code review agent emphasizing security, quality, and best practices
  Source:           BuiltIn
  File:            C:\app\Agents\built-in\code-reviewer.md
  Active:          yes
  Has curation:    True
  Enabled skills:  file-analyzer
  Disabled tools:  note-taker:add-note, note-taker:delete-note, ...
  Auto-approve:    file-analyzer:line-count, file-analyzer:find-todos, file-analyzer:tree-summary
  Instructions:
    # Code Reviewer Agent
    
    You are a meticulous code reviewer. When reviewing code, you focus on
    correctness, security, maintainability, and performance — in that
    order of priority.
    
    ## Review Methodology
    
    1. **Understand structure first**: Use `file-analyzer:tree-summary` to
    get an overview of the codebase or the area being reviewed
    ... (42 more lines)
```

#### `/agent project [on|off]` — Toggle Project AGENTS.md

```csharp
case "project":
    if (args.Length >= 2)
    {
        bool enable = args[1].ToLowerInvariant() switch
        {
            "on" or "true" or "1" => true,
            "off" or "false" or "0" => false,
            _ => _settings.ProjectAgentsEnabled // no change on invalid input
        };

        if (enable != _settings.ProjectAgentsEnabled)
        {
            _settings.ProjectAgentsEnabled = enable;
            CliStorage.SaveJson(CliStorage.SettingsPath, _settings);

            // Re-discover project context and rebuild
            _agentManager.RefreshProjectContext(Environment.CurrentDirectory);
            _builder.RebuildForAgentChange(_runtimeEventHandler);

            ConsoleRenderer.WriteSuccess(
                $"Project AGENTS.md: {(enable ? "enabled" : "disabled")}");
        }
        else
        {
            ConsoleRenderer.WriteInfo(
                $"Project AGENTS.md already {(enable ? "enabled" : "disabled")}.");
        }
    }
    else
    {
        CliAgentDefinition? project = _agentManager.GetProjectContext();
        ConsoleRenderer.WriteInfo(
            $"Project AGENTS.md: {(_settings.ProjectAgentsEnabled ? "enabled" : "disabled")}");
        if (project is not null)
            ConsoleRenderer.WriteInfo($"  Detected: {project.FilePath}");
        else
            ConsoleRenderer.WriteInfo("  No AGENTS.md found in current directory hierarchy.");
    }
    break;
```

**Note**: `RefreshProjectContext()` is a convenience method on `AgentDefinitionManager` that re-runs the CWD hierarchy scan:

```csharp
public void RefreshProjectContext(string cwd)
{
    _projectContext = _settings.ProjectAgentsEnabled
        ? AgentDefinitionLoader.DiscoverProjectAgents(cwd)
        : null;
}
```

---

## Program.cs Integration

### Current Initialization Flow (Steps 1-11)

```
1. Storage
2. Settings
3. Provider Detection
4. Skills
5. MCP
6. Tool Approval
7. Conversation Memory
8. Agent Build
9. Command Registration
10. Banner
11. REPL Loop
```

### Modified Flow (Step 4b Added)

```
1. Storage
2. Settings
3. Provider Detection
4. Skills
4b. Agent Discovery (NEW)          ← AgentDefinitionManager.LoadAll()
5. MCP
6. Tool Approval
7. Conversation Memory
8. Agent Build (MODIFIED)           ← CliAgentBuilder now takes AgentDefinitionManager
8b. Apply Agent Baseline (NEW)      ← If active_agent in settings, apply baseline
9. Command Registration (MODIFIED)  ← Register AgentCommand
10. Banner (MODIFIED)               ← Show active agent name
11. REPL Loop
```

### Code Changes

```csharp
static async Task<int> RunAsync(string[] args)
{
    // Steps 1-3 unchanged...

    // Step 4: Skills
    CliSkillManager skillManager = new(settings);
    string skillsDir = CliSkillLoader.ResolveSkillsDirectory(settings);
    skillManager.LoadSkills(skillsDir);
    ConsoleRenderer.WriteInfo(
        $"Skills: {skillManager.GetEnabledSkills().Count} enabled, " +
        $"{skillManager.GetAllSkills().Count} total (from {skillsDir})");

    // Step 4b: Agent Discovery                             // NEW
    AgentDefinitionManager agentManager = new(settings);    // NEW
    string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings);
    string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
    agentManager.LoadAll(builtInDir, agentsDir, Environment.CurrentDirectory);
    
    CliAgentDefinition? projectContext = agentManager.GetProjectContext();
    ConsoleRenderer.WriteInfo(
        $"Agents: {agentManager.GetAllPersonas().Count} personas available" +
        $"{(projectContext is not null ? ", project AGENTS.md detected" : "")}");

    // Steps 5-6 unchanged...

    // Step 7: Conversation Memory
    _memoryManager = new ConversationMemoryManager(...);
    _conversationStore = new ConversationStore();

    // Step 8: Build Agent (MODIFIED — passes agentManager)
    _agentBuilder = new CliAgentBuilder(
        providerResult.Api,
        providerResult.ActiveModel,
        skillManager,
        _mcpLoader,
        toolApproval,
        _memoryManager,
        agentManager);                                           // NEW

    Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler = HandleRuntimeEvent;

    // Step 8b: Apply saved agent baseline (if any)             // NEW
    if (agentManager.ActivePersonaName is not null)              // NEW
    {                                                            // NEW
        agentManager.ApplyCapabilityBaseline(skillManager, toolApproval);
        ConsoleRenderer.WriteInfo(
            $"Restored agent: {agentManager.ActivePersonaName}");
    }

    ChatRuntime runtime = _agentBuilder.Build(runtimeEventHandler);

    // Step 9: Register Commands (MODIFIED — adds AgentCommand)
    CommandDispatcher dispatcher = new();
    dispatcher.Register(new HelpCommand(dispatcher));
    dispatcher.Register(new ModelCommand(providerResult, _agentBuilder, runtimeEventHandler));
    dispatcher.Register(new SkillCommand(skillManager, _agentBuilder, runtimeEventHandler));
    dispatcher.Register(new AgentCommand(                        // NEW
        agentManager, skillManager, _agentBuilder,               // NEW
        runtimeEventHandler));                                    // NEW
    dispatcher.Register(new ConversationCommand(_memoryManager, _conversationStore, _agentBuilder));
    dispatcher.Register(new ToolsCommand(toolApproval, _agentBuilder));
    dispatcher.Register(new McpCommand(_mcpLoader, _agentBuilder));
    dispatcher.Register(new ClearCommand());
    dispatcher.Register(new ExitCommand(_memoryManager, _conversationStore, _agentBuilder));

    // Step 10: Banner (MODIFIED — shows agent name)
    string agentLabel = agentManager.ActivePersonaName is not null
        ? $" [{agentManager.ActivePersonaName}]" : "";
    ConsoleRenderer.WriteInfo($"\nActive model: {providerResult.ActiveModel.Name}{agentLabel}");
    ConsoleRenderer.WriteInfo("Type /help for commands. Start chatting!\n");

    return await ReplLoop(runtime, dispatcher, _memoryManager, _agentBuilder, runtimeEventHandler);
}
```

---

## SkillCommand Modifications

Update `/skill list` to show when a skill's state was set by the active agent persona:

### Current Output
```
  ✓ file-analyzer              Analyzes source files...
  ✗ web-search                 Search the web...
  ✗ note-taker                 Manage notes...
```

### Modified Output (with active persona)
```
  ✓ file-analyzer              Analyzes source files...
  ✗ web-search                 Search the web... [agent: code-reviewer]
  ✗ note-taker                 Manage notes... [agent: code-reviewer]
```

### Implementation

The `SkillCommand` needs access to `AgentDefinitionManager` to check if a skill's disabled state was imposed by the persona:

```csharp
internal sealed class SkillCommand : ICliCommand
{
    // ... existing fields ...
    private readonly AgentDefinitionManager _agentManager;  // NEW

    public SkillCommand(
        CliSkillManager skillManager,
        CliAgentBuilder builder,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler,
        AgentDefinitionManager agentManager)                // NEW
    {
        // ...
        _agentManager = agentManager;
    }

    // In the "list" case:
    case "list":
        List<CliSkill> skills = _skillManager.GetAllSkills();
        CliAgentDefinition? activePersona = _agentManager.GetActivePersona();

        foreach (CliSkill skill in skills)
        {
            string status = skill.Enabled ? "✓" : "✗";
            string activated = skill.Activated ? " [active in context]" : "";

            // Check if the disabled state was imposed by the agent
            string agentTag = "";
            if (!skill.Enabled && activePersona is not null)
            {
                bool agentDisabled = false;
                if (activePersona.EnabledSkills.Count > 0 &&
                    !activePersona.EnabledSkills.Contains(skill.Name))
                    agentDisabled = true;
                if (activePersona.DisabledSkills.Contains(skill.Name))
                    agentDisabled = true;
                if (agentDisabled)
                    agentTag = $" [agent: {activePersona.Name}]";
            }

            ConsoleRenderer.WriteInfo(
                $"  {status} {skill.Name,-25} {skill.Description}{activated}{agentTag}");
        }
        break;
```

**Behavior**: When the user runs `/skill enable web-search` to override the agent baseline, the `[agent: ...]` tag disappears because the skill is now enabled (the tag only shows for disabled skills that the agent imposed).

---

## ToolsCommand Modifications

Update `/tools list` to show tools blocked by the active agent persona:

### Current Output
```
  load_skill          always-allow
  list_skills         always-allow
  read_reference      always-allow
  file-analyzer:line-count    allow
  file-analyzer:find-todos    allow
  web-search:ddg-search       unknown
```

### Modified Output (with active persona blocking tools)
```
  load_skill                  always-allow
  list_skills                 always-allow
  read_reference              always-allow
  file-analyzer:line-count    allow (auto-approved by agent)
  file-analyzer:find-todos    allow (auto-approved by agent)
  web-search:ddg-search       [blocked by agent: code-reviewer]
```

### Implementation

The `ToolsCommand` needs access to `AgentDefinitionManager`:

```csharp
internal sealed class ToolsCommand : ICliCommand
{
    private readonly ToolApprovalManager _toolApproval;
    private readonly CliAgentBuilder _builder;
    private readonly AgentDefinitionManager _agentManager;  // NEW

    public ToolsCommand(
        ToolApprovalManager toolApproval,
        CliAgentBuilder builder,
        AgentDefinitionManager agentManager)                // NEW
    {
        _toolApproval = toolApproval;
        _builder = builder;
        _agentManager = agentManager;
    }

    // In the "list" case, after showing registered tools:
    // Also show tools that are blocked by the active persona
    CliAgentDefinition? persona = _agentManager.GetActivePersona();
    if (persona is not null && persona.DisabledTools.Count > 0)
    {
        ConsoleRenderer.WriteInfo("\n  Blocked by active agent:");
        foreach (string blocked in persona.DisabledTools)
            ConsoleRenderer.WriteInfo($"    ✗ {blocked}");
    }
```

---

## Updated Command Table

| Command | Description |
|---------|-------------|
| `/help [command]` | Show available commands or details |
| `/model [list \| set <name>]` | View/switch LLM models |
| `/skill [list \| enable \| disable \| info]` | Manage skills (shows agent state) |
| **`/agent [list \| set \| clear \| info \| project]`** | **Manage agent personas (NEW)** |
| `/conversation [save \| load \| list \| delete \| new]` | Conversation management |
| `/tools [list \| reset [tool]]` | View/reset tool approvals (shows agent blocks) |
| `/mcp [status \| reload]` | View MCP server status |
| `/clear` | Clear console |
| `/exit` | Auto-save and exit |

---

## Console Prompt Enhancement

Optionally, when an agent persona is active, the REPL prompt could show the agent name:

### Current Prompt
```
[claude-sonnet-4-20250514] > 
```

### With Active Agent
```
[code-reviewer | claude-sonnet-4-20250514] > 
```

This requires a minor change in `ConsoleRenderer.WritePrompt()` or in the REPL loop where the prompt is displayed. The `_agentManager.ActivePersonaName` can be passed through or the builder can expose it.

```csharp
// In Program.cs REPL loop:
ConsoleRenderer.WritePrompt(builder.ActiveModel.Name, agentManager.ActivePersonaName);

// In ConsoleRenderer:
public static void WritePrompt(string modelName, string? agentName = null)
{
    string label = agentName is not null
        ? $"[{agentName} | {modelName}]"
        : $"[{modelName}]";
    Console.Write($"{label} > ");
}
```

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| `/agent set` with no name argument | Show usage: `/agent [list \| set <name> \| ...]` |
| `/agent set nonexistent` | Error: "Agent 'nonexistent' not found. Use /agent list to see available agents." |
| `/agent info nonexistent` | Error: "Agent 'nonexistent' not found." |
| `/agent project` with no argument | Show current state |
| `/agent project invalid` | No change, show current state |
| No agents/ directory and no built-in agents found | `/agent list` shows "No agent personas found." |
| Startup with saved `active_agent` that no longer exists | `LoadAll()` clears it from settings, startup continues with no persona |

---

## Startup Console Output Example

```
LlmTornado CLI v1.0

Detecting providers...
  ✓ Anthropic (claude-sonnet-4-20250514, claude-haiku-3-5-20241022)
  ✓ OpenAI (gpt-4o, gpt-4o-mini)
Skills: 3 enabled, 3 total (from C:\repos\myapp\skills)
Agents: 5 personas available, project AGENTS.md detected
MCP: 1 server connected (2 tools)
Restored agent: code-reviewer

Active model: claude-sonnet-4-20250514 [code-reviewer]
Type /help for commands. Start chatting!

[code-reviewer | claude-sonnet-4-20250514] > 
```
