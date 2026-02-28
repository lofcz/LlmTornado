# Phase 3: Agent Builder Integration

## Goal

Modify `CliAgentBuilder` to incorporate the `AgentDefinitionManager` into the agent construction pipeline. This includes: (1) layering agent persona instructions and project AGENTS.md context into the system prompt, (2) filtering tools based on the active persona's curation rules, and (3) providing a rebuild method for agent switches.

---

## File to Modify

### `src/LlmTornado.Cli/CliAgentBuilder.cs`

---

## Current State (Before Modification)

The existing `CliAgentBuilder` has this structure:

```csharp
internal sealed class CliAgentBuilder
{
    // Dependencies
    private readonly TornadoApi _api;
    private readonly CliSkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly ToolApprovalManager _toolApproval;
    private readonly ConversationMemoryManager _memoryManager;

    // State
    private ChatModel _activeModel;
    private TornadoAgent? _agent;
    private ChatRuntime? _runtime;

    // Build pipeline
    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null);
    public ChatRuntime SetModel(ChatModel model, ...);
    public ChatRuntime RebuildForSkillChange(...);
    private string BuildSystemPrompt();
    private List<Tool> CollectTools();
    private Tool BuildLoadSkillTool();
    private Tool BuildListSkillsTool();
    private Tool BuildReadReferenceTool();
}
```

---

## Changes Required

### 1. Add AgentDefinitionManager Dependency

```csharp
internal sealed class CliAgentBuilder
{
    private readonly TornadoApi _api;
    private readonly CliSkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly ToolApprovalManager _toolApproval;
    private readonly ConversationMemoryManager _memoryManager;
    private readonly AgentDefinitionManager _agentManager;  // NEW

    public CliAgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        CliSkillManager skillManager,
        McpConfigLoader mcpLoader,
        ToolApprovalManager toolApproval,
        ConversationMemoryManager memoryManager,
        AgentDefinitionManager agentManager)         // NEW PARAMETER
    {
        _api = api;
        _activeModel = activeModel;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _toolApproval = toolApproval;
        _memoryManager = memoryManager;
        _agentManager = agentManager;                // NEW
    }
}
```

### 2. New RebuildForAgentChange Method

```csharp
/// <summary>
/// Rebuild the agent after an agent persona switch.
/// Applies the new persona's capability baseline before rebuilding.
/// </summary>
public ChatRuntime RebuildForAgentChange(
    Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
{
    // Apply the active persona's skill/tool curation
    _agentManager.ApplyCapabilityBaseline(_skillManager, _toolApproval);
    return Build(onRuntimeEvent);
}
```

**Note**: This is distinct from `RebuildForSkillChange()` which does NOT reapply the baseline. When the user manually enables/disables a skill via `/skill`, we rebuild without resetting to the agent baseline — that's the "user override" behavior.

```
/agent set code-reviewer  → RebuildForAgentChange()  → baseline applied, then build
/skill enable web-search  → RebuildForSkillChange()  → no baseline reset, just build
/agent clear              → RebuildForAgentChange()  → baseline cleared, then build
```

---

### 3. Modified BuildSystemPrompt

The system prompt is restructured with a layered architecture:

```csharp
private string BuildSystemPrompt()
{
    StringBuilder sb = new();

    // --- Layer 1: Agent Instructions (persona + project context) ---
    string agentInstructions = _agentManager.BuildInstructionsBlock();
    if (!string.IsNullOrEmpty(agentInstructions))
    {
        sb.Append(agentInstructions);
    }
    else
    {
        // Default prompt when no persona and no project context
        sb.AppendLine("You are a helpful CLI assistant with access to skills and tools.");
        sb.AppendLine("You can activate skills to gain specialized knowledge and capabilities.");
        sb.AppendLine();
    }

    // --- Layer 2: Skills Catalog (progressive disclosure) ---
    List<CliSkill> enabledSkills = _skillManager.GetEnabledSkills();
    if (enabledSkills.Count > 0)
    {
        sb.AppendLine(_skillManager.BuildSkillsContextXml());
        sb.AppendLine();
        sb.AppendLine("When a user's task matches a skill, use the `load_skill` tool to activate it.");
        sb.AppendLine("The skill's full instructions will be returned and you should follow them.");
        sb.AppendLine();
    }

    // --- Layer 3: Environment Context ---
    sb.AppendLine($"The user's current working directory is: {Environment.CurrentDirectory}");

    return sb.ToString();
}
```

### System Prompt Assembly Order & Rationale

```
┌──────────────────────────────────────────────────────────┐
│ Layer 1a: <agent_persona>                                │
│   Persona behavior instructions (from built-in/custom)   │
│   Sets: tone, workflow, response style, domain focus     │
│                                                          │
│   Example: "You are a meticulous code reviewer.          │
│   Focus on security, error handling, performance..."     │
├──────────────────────────────────────────────────────────┤
│ Layer 1b: <project_context>                              │
│   AGENTS.md from CWD hierarchy (auto-detected)           │
│   Sets: build commands, code style, testing procedures   │
│                                                          │
│   Example: "Run tests with `dotnet test`. Use C# 12     │
│   features. Follow the existing naming conventions..."   │
├──────────────────────────────────────────────────────────┤
│ Layer 2: <available_skills>                              │
│   Skills catalog XML (from CliSkillManager)              │
│   Provides: tool discovery metadata for the LLM         │
├──────────────────────────────────────────────────────────┤
│ Layer 3: Environment                                     │
│   CWD path                                               │
└──────────────────────────────────────────────────────────┘
```

**Why this order?** 
- **Persona first**: The persona defines the agent's identity and approach. It should be the primary behavioral anchor.
- **Project context second**: Repository-specific instructions add detail about the current project. These complement the persona — "be a code reviewer" (persona) + "for a .NET project that uses xUnit" (project context).
- **Skills third**: The tool catalog is functional/mechanical. The LLM needs to know what tools are available, but this shouldn't override behavioral instructions.
- **Environment last**: Purely factual context (CWD path).

---

### 4. Modified CollectTools

The tool collection now filters based on the active persona's tool curation rules:

```csharp
private List<Tool> CollectTools()
{
    List<Tool> tools = [];

    // 1. Built-in skill management tools (always included)
    tools.Add(BuildLoadSkillTool());
    tools.Add(BuildListSkillsTool());
    tools.Add(BuildReadReferenceTool());

    // 2. Script tools from enabled skills
    List<Tool> scriptTools = ScriptToolBuilder.BuildScriptTools(_skillManager.GetEnabledSkills());
    tools.AddRange(scriptTools);

    // 3. MCP tools
    tools.AddRange(_mcpLoader.AllTools);

    // 4. Filter by active agent persona's tool curation    // NEW
    tools = FilterToolsByAgent(tools);                      // NEW

    return tools;
}

/// <summary>
/// Remove tools that the active agent persona has blocked.
/// Built-in agent management tools (load_skill, list_skills, read_reference)
/// are never filtered — they're exempt from persona curation.
/// </summary>
private List<Tool> FilterToolsByAgent(List<Tool> tools)     // NEW
{
    // No filtering if no agent manager or no active persona
    List<Tool> filtered = [];
    foreach (Tool tool in tools)
    {
        string? toolName = tool.Function?.Name;
        if (toolName is null)
        {
            filtered.Add(tool);
            continue;
        }
        if (_agentManager.IsToolAllowed(toolName))
            filtered.Add(tool);
    }
    return filtered;
}
```

### Filtering Example

```
Active persona: code-reviewer
  enabled-skills: [file-analyzer]
  disabled-tools: [web-search:ddg-search, web-search:fetch-url]

Available tools before filtering:
  load_skill                       → always allowed
  list_skills                      → always allowed
  read_reference                   → always allowed
  file-analyzer:line-count         → allowed (skill enabled, tool not blocked)
  file-analyzer:find-todos         → allowed
  file-analyzer:detect-encoding    → allowed
  file-analyzer:find-duplicates    → allowed
  file-analyzer:tree-summary       → allowed
  web-search:ddg-search            → BLOCKED (in disabled-tools list)
  web-search:fetch-url             → BLOCKED
  web-search:extract-text          → ??? depends on skill state
  note-taker:add-note              → ??? depends on skill state

Since enabled-skills: [file-analyzer], skills web-search and note-taker are 
disabled by ApplyCapabilityBaseline(). Their script tools are not generated 
by ScriptToolBuilder (which only builds for enabled skills).

So the web-search and note-taker tools never reach CollectTools() in the 
first place. The disabled-tools filter is a second layer of defense for 
cases where the tool might come from MCP or other sources.

Tools after filtering:
  load_skill
  list_skills
  read_reference
  file-analyzer:line-count
  file-analyzer:find-todos
  file-analyzer:detect-encoding
  file-analyzer:find-duplicates
  file-analyzer:tree-summary
```

---

### 5. Updated Build Method

Minor change: the `Build()` method itself doesn't change structurally, but it now benefits from the modified `BuildSystemPrompt()` and `CollectTools()`:

```csharp
public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
{
    string systemPrompt = BuildSystemPrompt();    // now includes persona + project context
    List<Tool> allTools = CollectTools();          // now filtered by agent tool curation

    _agent = new TornadoAgent(
        client: _api,
        model: _activeModel,
        name: "CLI-Agent",
        instructions: systemPrompt,
        streaming: true);

    foreach (Tool tool in allTools)
        _agent.AddTool(tool);

    // Tool permissions
    foreach (Tool tool in allTools)
    {
        string? toolName = tool.Function?.Name;
        if (toolName is not null)
            _agent.ToolPermissionRequired[toolName] = true;
    }

    // Pre-approve from skill allowed-tools
    foreach (CliSkill skill in _skillManager.GetEnabledSkills())
    {
        if (skill.AllowedTools.Count > 0)
            _toolApproval.PreApproveSkillTools(skill.AllowedTools);
    }

    // NOTE: Agent persona auto-approve is handled in ApplyCapabilityBaseline(),
    // which runs before Build() in the RebuildForAgentChange() flow.

    SingletonRuntimeConfiguration runtimeConfig = new(_agent);
    runtimeConfig.OnRuntimeEvent = onRuntimeEvent;
    runtimeConfig.OnRuntimeRequestEvent = _toolApproval.HandleToolPermissionRequest;

    _runtime = new ChatRuntime(runtimeConfig);
    return _runtime;
}
```

---

## Complete Modified Builder — Full Code

```csharp
using System.Text;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Agents;           // NEW
using LlmTornado.Cli.Mcp;
using LlmTornado.Cli.Memory;
using LlmTornado.Cli.Skills;
using LlmTornado.Common;

namespace LlmTornado.Cli;

internal sealed class CliAgentBuilder
{
    private readonly TornadoApi _api;
    private readonly CliSkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly ToolApprovalManager _toolApproval;
    private readonly ConversationMemoryManager _memoryManager;
    private readonly AgentDefinitionManager _agentManager;  // NEW

    private ChatModel _activeModel;
    private TornadoAgent? _agent;
    private ChatRuntime? _runtime;

    public TornadoAgent Agent => _agent ?? throw new InvalidOperationException("Agent not built");
    public ChatRuntime Runtime => _runtime ?? throw new InvalidOperationException("Runtime not built");
    public ChatModel ActiveModel => _activeModel;

    public CliAgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        CliSkillManager skillManager,
        McpConfigLoader mcpLoader,
        ToolApprovalManager toolApproval,
        ConversationMemoryManager memoryManager,
        AgentDefinitionManager agentManager)         // NEW
    {
        _api = api;
        _activeModel = activeModel;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _toolApproval = toolApproval;
        _memoryManager = memoryManager;
        _agentManager = agentManager;                // NEW
    }

    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        string systemPrompt = BuildSystemPrompt();
        List<Tool> allTools = CollectTools();

        _agent = new TornadoAgent(
            client: _api,
            model: _activeModel,
            name: "CLI-Agent",
            instructions: systemPrompt,
            streaming: true);

        foreach (Tool tool in allTools)
            _agent.AddTool(tool);

        foreach (Tool tool in allTools)
        {
            string? toolName = tool.Function?.Name;
            if (toolName is not null)
                _agent.ToolPermissionRequired[toolName] = true;
        }

        foreach (CliSkill skill in _skillManager.GetEnabledSkills())
        {
            if (skill.AllowedTools.Count > 0)
                _toolApproval.PreApproveSkillTools(skill.AllowedTools);
        }

        SingletonRuntimeConfiguration runtimeConfig = new(_agent);
        runtimeConfig.OnRuntimeEvent = onRuntimeEvent;
        runtimeConfig.OnRuntimeRequestEvent = _toolApproval.HandleToolPermissionRequest;

        _runtime = new ChatRuntime(runtimeConfig);
        return _runtime;
    }

    public ChatRuntime SetModel(ChatModel model, Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        _activeModel = model;
        _memoryManager.UpdateModel(model, model.ContextTokens);
        return Build(onRuntimeEvent);
    }

    public ChatRuntime RebuildForSkillChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
        => Build(onRuntimeEvent);

    /// <summary>                                                   // NEW
    /// Rebuild after agent persona switch.                         // NEW
    /// Applies capability baseline before rebuilding.              // NEW
    /// </summary>                                                  // NEW
    public ChatRuntime RebuildForAgentChange(                       // NEW
        Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)  // NEW
    {                                                               // NEW
        _agentManager.ApplyCapabilityBaseline(_skillManager, _toolApproval); // NEW
        return Build(onRuntimeEvent);                               // NEW
    }                                                               // NEW

    private string BuildSystemPrompt()
    {
        StringBuilder sb = new();

        // Layer 1: Agent instructions (persona + project context)      // MODIFIED
        string agentInstructions = _agentManager.BuildInstructionsBlock(); // MODIFIED
        if (!string.IsNullOrEmpty(agentInstructions))                     // MODIFIED
        {                                                                  // MODIFIED
            sb.Append(agentInstructions);                                  // MODIFIED
        }                                                                  // MODIFIED
        else                                                               // MODIFIED
        {
            sb.AppendLine("You are a helpful CLI assistant with access to skills and tools.");
            sb.AppendLine("You can activate skills to gain specialized knowledge and capabilities.");
            sb.AppendLine();
        }

        // Layer 2: Skills catalog
        List<CliSkill> enabledSkills = _skillManager.GetEnabledSkills();
        if (enabledSkills.Count > 0)
        {
            sb.AppendLine(_skillManager.BuildSkillsContextXml());
            sb.AppendLine();
            sb.AppendLine("When a user's task matches a skill, use the `load_skill` tool to activate it.");
            sb.AppendLine("The skill's full instructions will be returned and you should follow them.");
            sb.AppendLine();
        }

        // Layer 3: Environment
        sb.AppendLine($"The user's current working directory is: {Environment.CurrentDirectory}");
        return sb.ToString();
    }

    private List<Tool> CollectTools()
    {
        List<Tool> tools = [];
        tools.Add(BuildLoadSkillTool());
        tools.Add(BuildListSkillsTool());
        tools.Add(BuildReadReferenceTool());
        tools.AddRange(ScriptToolBuilder.BuildScriptTools(_skillManager.GetEnabledSkills()));
        tools.AddRange(_mcpLoader.AllTools);

        // Filter by active agent persona                                 // NEW
        return tools.Where(t =>                                           // NEW
        {                                                                  // NEW
            string? name = t.Function?.Name;                               // NEW
            return name is null || _agentManager.IsToolAllowed(name);      // NEW
        }).ToList();                                                       // NEW
    }

    // ... BuildLoadSkillTool, BuildListSkillsTool, BuildReadReferenceTool unchanged ...
}
```

---

## Generated System Prompt Examples

### With Persona + Project Context + Skills

```
<agent_persona>
# Code Reviewer Agent

You are a meticulous code reviewer. Your primary focus is on:
- Security vulnerabilities (SQL injection, XSS, CSRF, etc.)
- Error handling completeness
- Performance implications
- Naming and code style consistency

## Workflow
1. When asked to review code, first use file-analyzer to understand the codebase structure
2. Look for common vulnerability patterns
3. Check error handling paths
...
</agent_persona>

<project_context>
<!-- AGENTS.md from: C:\repos\myapp\src\AGENTS.md -->
# MyApp Agent Instructions

## Build
- dotnet build src/MyApp.sln
- dotnet test src/MyApp.Tests/

## Code Style
- Use file-scoped namespaces
- Prefer pattern matching
- All public APIs must have XML doc comments
</project_context>

<available_skills>
  <skill>
    <name>file-analyzer</name>
    <description>Analyzes source files for line counts, TODOs, encoding, duplicates, and directory structure.</description>
    <scripts>line-count.py, find-todos.py, detect-encoding.py, find-duplicates.py, tree-summary.py</scripts>
  </skill>
</available_skills>

When a user's task matches a skill, use the `load_skill` tool to activate it.
The skill's full instructions will be returned and you should follow them.

The user's current working directory is: C:\repos\myapp\src
```

### Default (No Persona, No Project Context)

```
You are a helpful CLI assistant with access to skills and tools.
You can activate skills to gain specialized knowledge and capabilities.

<available_skills>
  <skill>
    <name>file-analyzer</name>
    <description>Analyzes source files...</description>
  </skill>
  <skill>
    <name>web-search</name>
    <description>Search the web...</description>
  </skill>
  <skill>
    <name>note-taker</name>
    <description>Manage notes...</description>
  </skill>
</available_skills>

When a user's task matches a skill, use the `load_skill` tool to activate it.
The skill's full instructions will be returned and you should follow them.

The user's current working directory is: C:\Users\john
```

---

## Rebuild Method Comparison

| Method | Trigger | Baseline Reset | Use Case |
|--------|---------|---------------|----------|
| `Build()` | Internal (called by other methods) | No | Raw build, used by rebuild methods |
| `SetModel()` | `/model set <name>` | No | Model change preserves persona + skill state |
| `RebuildForSkillChange()` | `/skill enable\|disable` | No | User override of individual skill |
| `RebuildForAgentChange()` | `/agent set\|clear` | **Yes** | Agent switch resets capabilities to baseline |

---

## Backward Compatibility

The only breaking change is the `CliAgentBuilder` constructor signature gaining the `AgentDefinitionManager` parameter. All call sites in `Program.cs` must be updated. If no persona is set and no project AGENTS.md exists:
- `BuildInstructionsBlock()` returns `""` → falls through to default prompt
- `IsToolAllowed()` returns `true` for all tools → no filtering
- Behavior is identical to the pre-integration state
