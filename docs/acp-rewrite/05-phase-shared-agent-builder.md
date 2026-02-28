# Phase 5 — Create Shared AgentBuilder

## Objective

Extract the core agent assembly logic from `CliAgentBuilder` into a reusable `AgentBuilder` in Core. This is the **composition root** for building a `TornadoAgent` + `ChatRuntime` from all the subsystems. The CLI and ACP server both need to do this, but with different tool sets and different event handlers.

## Why This Is the Hardest Extraction

`CliAgentBuilder` is currently the most coupled type. It depends on:

| Dependency | CLI-Specific? | Strategy |
|-----------|--------------|----------|
| `TornadoApi` | No | Pass through |
| `ChatModel` | No | Pass through |
| `CliSkillManager` → `SkillManager` | No (after Phase 4) | Pass through |
| `McpConfigLoader` | No (after Phase 3) | Pass through |
| `ToolApprovalManager` | **Yes** (console I/O) | Replace with `IToolApproval` |
| `ConversationMemoryManager` | **Yes** (CLI memory) | Make optional / remove |
| `AgentDefinitionManager` | No (after Phase 4) | Pass through |
| `CliSettings` → `AgentSettings` | No (after Phase 2) | Pass through |
| `ConsoleRenderer` | **Yes** (console output) | Replace with optional callbacks |
| `ToolOptimizer` | No (after Phase 2) | Pass through |

The key insight: the shared `AgentBuilder` handles the core work (system prompt construction, tool collection, agent creation). CLI-specific concerns (console rendering, interactive approval, memory) stay in a thin CLI wrapper.

---

## Shared `AgentBuilder` Design

### Constructor

```csharp
namespace LlmTornado.Cli.Core;

/// <summary>
/// Assembles a TornadoAgent and ChatRuntime from agent definitions, skills, and tools.
/// Shared between CLI and ACP server.
/// </summary>
public sealed class AgentBuilder
{
    private readonly TornadoApi _api;
    private readonly SkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly IToolApproval _toolApproval;
    private readonly AgentDefinitionManager _agentManager;
    private readonly AgentSettings _settings;

    private ChatModel _activeModel;
    private TornadoAgent? _agent;
    private ChatRuntime? _runtime;
    private ToolOptimizer? _toolOptimizer;
    private List<Tool>? _fullToolList;

    /// <summary>
    /// Additional tools provided by the host (e.g., ACP filesystem tools).
    /// </summary>
    private readonly List<Tool> _additionalTools;

    public TornadoAgent Agent => _agent ?? throw new InvalidOperationException("Agent not built");
    public ChatRuntime Runtime => _runtime ?? throw new InvalidOperationException("Runtime not built");
    public ChatModel ActiveModel => _activeModel;
    public bool NeedsOptimization => _toolOptimizer is not null && _fullToolList is not null
                                     && _fullToolList.Count > _settings.MaxTools;
    public int TotalToolCount => _fullToolList?.Count ?? 0;

    public AgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        SkillManager skillManager,
        McpConfigLoader mcpLoader,
        IToolApproval toolApproval,
        AgentDefinitionManager agentManager,
        AgentSettings settings,
        ChatModel? optimizerModel = null,
        List<Tool>? additionalTools = null)
    {
        _api = api;
        _activeModel = activeModel;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _toolApproval = toolApproval;
        _agentManager = agentManager;
        _settings = settings;
        _additionalTools = additionalTools ?? [];

        if (settings.ToolOptimizerEnabled && optimizerModel is not null)
            _toolOptimizer = new ToolOptimizer(api, optimizerModel, settings.MaxTools);
    }
}
```

**Key difference from `CliAgentBuilder`:** the `additionalTools` parameter. This is how the ACP server injects its filesystem tools (`list_dir`, `read_file`, etc.) without the Core needing to know about them.

---

### Build Method

```csharp
/// <summary>
/// Build or rebuild the agent and runtime.
/// </summary>
public ChatRuntime Build(
    Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null,
    Func<string, ValueTask<bool>>? onToolPermissionRequest = null)
{
    string systemPrompt = BuildSystemPrompt();
    List<Tool> allTools = CollectTools();
    _fullToolList = allTools;

    _agent = new TornadoAgent(
        client: _api,
        model: _activeModel,
        name: "Tornado-Agent",
        instructions: systemPrompt,
        streaming: true);

    foreach (Tool tool in allTools)
        _agent.AddTool(tool);

    // Configure tool permissions
    foreach (Tool tool in allTools)
    {
        string? toolName = tool.Function?.Name;
        if (toolName is not null)
            _agent.ToolPermissionRequired[toolName] = true;
    }

    // Pre-approve skill allowed-tools
    foreach (SkillDefinition skill in _skillManager.GetEnabledSkills())
    {
        if (skill.AllowedTools.Count > 0)
            _toolApproval.PreApproveTools(skill.AllowedTools);
    }

    SingletonRuntimeConfiguration runtimeConfig = new(_agent);
    runtimeConfig.OnRuntimeEvent = onRuntimeEvent;
    
    // Use provided handler, or fall back to tool approval interface
    runtimeConfig.OnRuntimeRequestEvent = onToolPermissionRequest 
        ?? _toolApproval.HandleToolPermissionRequest;

    _runtime = new ChatRuntime(runtimeConfig);
    return _runtime;
}
```

**What changed from `CliAgentBuilder.Build()`:**
1. `onToolPermissionRequest` is a parameter (CLI passes its approval handler, ACP passes auto-approve)
2. Agent name is `"Tornado-Agent"` instead of `"CLI-Agent"`
3. No `ConversationMemoryManager` dependency (memory management stays CLI-specific)
4. No `ConsoleRenderer` calls

---

### System Prompt Building

```csharp
private string BuildSystemPrompt()
{
    StringBuilder sb = new();

    // Layer 1: Agent instructions (persona + project context)
    string agentInstructions = _agentManager.BuildInstructionsBlock();
    if (!string.IsNullOrEmpty(agentInstructions))
    {
        sb.Append(agentInstructions);
    }
    else
    {
        sb.AppendLine("You are a helpful assistant with access to skills and tools.");
        sb.AppendLine("You can activate skills to gain specialized knowledge and capabilities.");
        sb.AppendLine();
    }

    // Layer 2: Skills catalog
    List<SkillDefinition> enabledSkills = _skillManager.GetEnabledSkills();
    if (enabledSkills.Count > 0)
    {
        sb.AppendLine(_skillManager.BuildSkillsContextXml());
        sb.AppendLine();
        sb.AppendLine("When a user's task matches a skill, use the `load_skill` tool to activate it.");
        sb.AppendLine("The skill's full instructions will be returned and you should follow them.");
        sb.AppendLine();
    }

    sb.AppendLine($"The user's current working directory is: {Environment.CurrentDirectory}");
    return sb.ToString();
}
```

**Identical to the CLI version** except:
- `CliSkill` → `SkillDefinition`
- No reference to `"CLI assistant"` — uses generic `"assistant"`

---

### Tool Collection

```csharp
private List<Tool> CollectTools()
{
    List<Tool> tools = [];

    // Built-in skill management tools
    tools.Add(BuildLoadSkillTool());
    tools.Add(BuildListSkillsTool());
    tools.Add(BuildReadReferenceTool());

    // Script tools from enabled skills
    List<SkillDefinition> enabledSkills = _skillManager.GetEnabledSkills();
    tools.AddRange(ScriptToolBuilder.BuildScriptTools(enabledSkills));

    // MCP tools
    tools.AddRange(_mcpLoader.AllTools);

    // Additional tools from the host (e.g., ACP filesystem tools)
    tools.AddRange(_additionalTools);

    // Filter by active agent persona's tool curation
    return tools.Where(t =>
    {
        string? name = t.Function?.Name;
        return name is null || _agentManager.IsToolAllowed(name);
    }).ToList();
}
```

**Key addition:** `_additionalTools` is merged into the tool list. This is how the ACP server's filesystem tools get included without the Core being aware of them.

---

### Tool Optimizer Methods

```csharp
/// <summary>
/// Run the LLM-based tool optimizer for the current user turn.
/// Returns the optimization result, or null if not needed.
/// </summary>
public async Task<ToolOptimizationResult?> OptimizeToolsForTurn(
    string userMessage, CancellationToken ct = default)
{
    if (_toolOptimizer is null || _agent is null || _fullToolList is null)
        return null;

    if (_fullToolList.Count <= _settings.MaxTools)
        return null;

    ToolOptimizationResult result = await _toolOptimizer.OptimizeAsync(
        _fullToolList, userMessage, ct);

    if (result.WasOptimized)
    {
        _agent.ClearTools();
        foreach (Tool tool in result.Tools)
            _agent.AddTool(tool);
    }

    return result;
}

/// <summary>
/// Restore the full tool list after an optimized turn completes.
/// </summary>
public void RestoreFullTools()
{
    if (_agent is null || _fullToolList is null)
        return;

    _agent.ClearTools();
    foreach (Tool tool in _fullToolList)
        _agent.AddTool(tool);
}
```

**Removed** the `ConsoleRenderer.WriteToolOptimization()` calls — the caller (CLI or ACP) handles reporting.

---

### Other Methods

```csharp
public ChatRuntime SetModel(ChatModel model, ...)
{
    _activeModel = model;
    // Note: no _memoryManager.UpdateModel() — that stays CLI-specific
    return Build(onRuntimeEvent, onToolPermissionRequest);
}

public ChatRuntime RebuildForSkillChange(...) => Build(...);

public ChatRuntime RebuildForAgentChange(...)
{
    _agentManager.ApplyCapabilityBaseline(_skillManager, _toolApproval);
    return Build(...);
}
```

---

## What Stays in CLI

The `CliAgentBuilder` in Phase 6 becomes a **thin wrapper** around the Core `AgentBuilder`:

```csharp
// In LlmTornado.Cli — thin wrapper adding CLI-specific concerns
internal sealed class CliAgentBuilder
{
    private readonly AgentBuilder _coreBuilder;
    private readonly ConversationMemoryManager _memoryManager;
    private readonly ConsoleRenderer _renderer;

    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _coreBuilder.Build(onRuntimeEvent, _toolApproval.HandleToolPermissionRequest);
    }

    public async Task<ToolOptimizationResult?> OptimizeToolsForTurn(string userMessage, CancellationToken ct)
    {
        ToolOptimizationResult? result = await _coreBuilder.OptimizeToolsForTurn(userMessage, ct);
        
        // CLI-specific: render optimization info to console
        if (result is { WasOptimized: true })
            ConsoleRenderer.WriteToolOptimization(result.OriginalCount, result.SelectedCount);
        else if (result is { FallbackReason: not null })
            ConsoleRenderer.WriteToolOptimizationSkipped(result.OriginalCount, result.FallbackReason);
        
        return result;
    }

    public ChatRuntime SetModel(ChatModel model, ...)
    {
        _memoryManager.UpdateModel(model, model.ContextTokens);  // CLI-specific
        return _coreBuilder.SetModel(model, ...);
    }
}
```

---

## How the ACP Server Uses AgentBuilder (Preview of Phase 7)

```csharp
// In TornadoAcpRuntime — uses AgentBuilder directly
AgentBuilder builder = new(
    api: _providerResult.Api,
    activeModel: _providerResult.ActiveModel,
    skillManager: _skillManager,
    mcpLoader: _mcpLoader,
    toolApproval: new AutoApproveToolApproval(),
    agentManager: _agentManager,
    settings: _settings,
    optimizerModel: _providerResult.OptimizerModel,
    additionalTools: BuildAcpLocalTools(request.Cwd)  // filesystem tools
);

ChatRuntime runtime = builder.Build(onRuntimeEvent: null);
```

---

## Built-in Tool Definitions (load_skill, list_skills, read_reference)

These are defined in the Core `AgentBuilder` and are always available. They're identical to the CLI versions:

```csharp
private Tool BuildLoadSkillTool() =>
    new(
        new Func<string, string>(skillName =>
        {
            string? instructions = _skillManager.ActivateSkill(skillName);
            return instructions ?? $"Skill '{skillName}' not found or not enabled.";
        }),
        "load_skill",
        "Load and activate a skill by name.");

private Tool BuildListSkillsTool() =>
    new(
        new Func<string>(() =>
        {
            // ... same as CLI version ...
        }),
        "list_skills",
        "List all enabled skills.");

private Tool BuildReadReferenceTool() =>
    new(
        new Func<string, string, string>((skillName, relativePath) =>
        {
            // ... same as CLI version ...
        }),
        "read_reference",
        "Read a reference file from a skill's directory.");
```

---

## Verification

```powershell
cd src
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
```

The AgentBuilder depends on:
- All Phase 2-4 types (all within Core)
- `LlmTornado.Agents` (TornadoAgent, ChatRuntime, SingletonRuntimeConfiguration)
- `LlmTornado.Common` (Tool)
- `LlmTornado.Mcp` (via McpConfigLoader)

All available through project references.

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Cli.Core/AgentBuilder.cs` | Create (extracted from CliAgentBuilder) |

## Summary of What's Shared vs. CLI-Only

| Concern | Core `AgentBuilder` | CLI `CliAgentBuilder` |
|---------|--------------------|-----------------------|
| System prompt construction | ✅ | Delegates to Core |
| Tool collection + filtering | ✅ | Delegates to Core |
| Agent + Runtime creation | ✅ | Delegates to Core |
| Tool optimization | ✅ | Renders status to console |
| Model switching | ✅ | Also updates memory manager |
| Agent change rebuild | ✅ | Also triggers console output |
| Console rendering | ❌ | ✅ |
| Interactive tool approval | ❌ | ✅ (via IToolApproval impl) |
| Conversation memory | ❌ | ✅ |
