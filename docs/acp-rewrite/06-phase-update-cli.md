# Phase 6 — Update CLI Project to Use Core

## Objective

Update `LlmTornado.Cli` to reference and use `LlmTornado.Cli.Core` instead of its own internal copies of the extracted types. The CLI should work identically after this phase — same behavior, same commands, same output. The only change is that the underlying types come from the shared library.

## Strategy

1. Add project reference to `LlmTornado.Cli.Core`
2. Delete the moved files from the CLI project
3. Update `using` statements throughout the CLI
4. Create thin adapter implementations for `ISettingsPersistence` and `IToolApproval`
5. Update `CliAgentBuilder` to wrap Core's `AgentBuilder`
6. Update `Program.cs` initialization code
7. Remove the built-in persona `.md` files (now in Core)

---

## 6.1 Update `.csproj`

**`LlmTornado.Cli/LlmTornado.Cli.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>LlmTornado.Cli</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="LlmTornado.Cli.Tests" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\LlmTornado\LlmTornado.csproj" />
    <ProjectReference Include="..\LlmTornado.Agents\LlmTornado.Agents.csproj" />
    <ProjectReference Include="..\LlmTornado.Mcp\LlmTornado.Mcp.csproj" />
    <!-- NEW: shared agent infrastructure -->
    <ProjectReference Include="..\LlmTornado.Cli.Core\LlmTornado.Cli.Core.csproj" />
  </ItemGroup>

  <!-- REMOVED: Built-in agent persona files (now in Core) -->
  <!-- The Content Include for Agents/built-in/*.md is removed -->

</Project>
```

**Changes:**
- Added `LlmTornado.Cli.Core` project reference
- Removed the `<Content Include="Agents\built-in\*.md">` item — personas are now in Core

---

## 6.2 Delete Moved Files

Remove these files from the CLI project (they now live in Core):

| File to Delete | Core Equivalent |
|---------------|-----------------|
| `Agents/CliAgentDefinition.cs` | `Core/Agents/AgentDefinition.cs` |
| `Agents/AgentDefinitionLoader.cs` | `Core/Agents/AgentDefinitionLoader.cs` |
| `Agents/AgentDefinitionManager.cs` | `Core/Agents/AgentDefinitionManager.cs` |
| `Skills/CliSkill.cs` | `Core/Skills/SkillDefinition.cs` |
| `Skills/CliSkillLoader.cs` | `Core/Skills/SkillLoader.cs` |
| `Skills/CliSkillManager.cs` | `Core/Skills/SkillManager.cs` |
| `Skills/ScriptToolBuilder.cs` | `Core/Skills/ScriptToolBuilder.cs` |
| `CliSettings.cs` | `Core/AgentSettings.cs` |
| `ProviderDetector.cs` | `Core/ProviderDetector.cs` |
| `ToolOptimizer.cs` | `Core/ToolOptimizer.cs` |
| `Mcp/McpConfigLoader.cs` | `Core/Mcp/McpConfigLoader.cs` |
| `Mcp/McpConfigModel.cs` | `Core/Mcp/McpConfigModel.cs` |
| `Agents/built-in/*.md` | `Core/Agents/built-in/*.md` |

**Files that STAY in CLI** (not extracted):
- `Program.cs` — entry point, REPL loop
- `ConsoleRenderer.cs` — CLI-specific rendering
- `ToolApprovalManager.cs` — interactive approval (implements `IToolApproval`)
- `CliAgentBuilder.cs` — thin wrapper around Core `AgentBuilder`
- `CliStorage.cs` — CLI-specific disk persistence
- `Memory/ConversationMemoryManager.cs` — CLI-specific
- `Commands/*` — CLI command handlers

---

## 6.3 Create Adapter Implementations

### `DiskSettingsPersistence.cs`

```csharp
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli;

/// <summary>
/// CLI implementation of settings persistence — writes to disk via CliStorage.
/// </summary>
internal sealed class DiskSettingsPersistence : ISettingsPersistence
{
    public void SaveSettings(AgentSettings settings)
    {
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
    }
}
```

### Update `ToolApprovalManager.cs` to implement `IToolApproval`

```csharp
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli;

internal sealed class ToolApprovalManager : IToolApproval  // <-- add interface
{
    // ... existing code ...

    // Rename PreApproveSkillTools → PreApproveTools (or add as alias)
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

    // HandleToolPermissionRequest already matches the interface signature
}
```

---

## 6.4 Update `using` Statements

Every file that used the old internal types needs updated imports:

```csharp
// BEFORE
using LlmTornado.Cli.Agents;     // CliAgentDefinition, AgentSource
using LlmTornado.Cli.Skills;     // CliSkill, CliSkillManager
using LlmTornado.Cli.Mcp;        // McpConfigLoader, McpConfig

// AFTER
using LlmTornado.Cli.Core;            // AgentSettings, AgentBuilder, ProviderDetector, etc.
using LlmTornado.Cli.Core.Agents;     // AgentDefinition, AgentSource, AgentDefinitionManager
using LlmTornado.Cli.Core.Skills;     // SkillDefinition, SkillManager, SkillLoader
using LlmTornado.Cli.Core.Mcp;        // McpConfigLoader, McpConfig
```

**Type name mapping for search-and-replace:**

| Old Name | New Name |
|----------|----------|
| `CliAgentDefinition` | `AgentDefinition` |
| `CliSkill` | `SkillDefinition` |
| `CliSkillLoader` | `SkillLoader` |
| `CliSkillManager` | `SkillManager` |
| `CliSettings` | `AgentSettings` |

---

## 6.5 Update `CliAgentBuilder` to Wrap Core `AgentBuilder`

The CLI's `CliAgentBuilder` becomes a thin wrapper that adds CLI-specific concerns:

```csharp
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Memory;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Chat.Models;

namespace LlmTornado.Cli;

/// <summary>
/// CLI wrapper around the shared AgentBuilder.
/// Adds console rendering, conversation memory, and interactive tool approval.
/// </summary>
internal sealed class CliAgentBuilder
{
    private readonly AgentBuilder _coreBuilder;
    private readonly ConversationMemoryManager _memoryManager;
    private readonly ToolApprovalManager _toolApproval;

    public TornadoAgent Agent => _coreBuilder.Agent;
    public ChatRuntime Runtime => _coreBuilder.Runtime;
    public ChatModel ActiveModel => _coreBuilder.ActiveModel;
    public bool NeedsOptimization => _coreBuilder.NeedsOptimization;
    public int TotalToolCount => _coreBuilder.TotalToolCount;

    public CliAgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        SkillManager skillManager,
        McpConfigLoader mcpLoader,
        ToolApprovalManager toolApproval,
        ConversationMemoryManager memoryManager,
        AgentDefinitionManager agentManager,
        AgentSettings settings,
        ChatModel? optimizerModel)
    {
        _toolApproval = toolApproval;
        _memoryManager = memoryManager;

        _coreBuilder = new AgentBuilder(
            api, activeModel, skillManager, mcpLoader,
            toolApproval, agentManager, settings,
            optimizerModel);
    }

    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _coreBuilder.Build(onRuntimeEvent, _toolApproval.HandleToolPermissionRequest);
    }

    public ChatRuntime SetModel(ChatModel model, Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        _memoryManager.UpdateModel(model, model.ContextTokens);
        return _coreBuilder.SetModel(model, onRuntimeEvent);
    }

    public ChatRuntime RebuildForSkillChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _coreBuilder.RebuildForSkillChange(onRuntimeEvent);
    }

    public ChatRuntime RebuildForAgentChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _coreBuilder.RebuildForAgentChange(onRuntimeEvent);
    }

    public async Task<ToolOptimizationResult?> OptimizeToolsForTurn(string userMessage, CancellationToken ct = default)
    {
        ToolOptimizationResult? result = await _coreBuilder.OptimizeToolsForTurn(userMessage, ct);

        // CLI-specific: render optimization status to console
        if (result is { WasOptimized: true })
            ConsoleRenderer.WriteToolOptimization(result.OriginalCount, result.SelectedCount);
        else if (result is { FallbackReason: not null })
            ConsoleRenderer.WriteToolOptimizationSkipped(result.OriginalCount, result.FallbackReason);

        return result;
    }

    public void RestoreFullTools() => _coreBuilder.RestoreFullTools();

    public void SetOptimizerEnabled(bool enabled, ChatModel? optimizerModel = null)
        => _coreBuilder.SetOptimizerEnabled(enabled, optimizerModel);

    public void SetMaxTools(int maxTools, ChatModel? optimizerModel = null)
        => _coreBuilder.SetMaxTools(maxTools, optimizerModel);
}
```

---

## 6.6 Update `Program.cs` Initialization

Key changes in the startup sequence:

```csharp
// BEFORE
CliSettings settings = CliStorage.LoadJson<CliSettings>(CliStorage.SettingsPath) ?? new();

// AFTER
AgentSettings settings = CliStorage.LoadJson<AgentSettings>(CliStorage.SettingsPath) ?? new();
ISettingsPersistence persistence = new DiskSettingsPersistence();

// BEFORE
CliSkillManager skillManager = new(settings);
// ...
AgentDefinitionManager agentManager = new(settings);

// AFTER
SkillManager skillManager = new(settings, persistence);
// ...
AgentDefinitionManager agentManager = new(settings, persistence);

// BEFORE
string skillsDir = CliSkillLoader.ResolveSkillsDirectory(settings);
string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings);

// AFTER
string skillsDir = SkillLoader.ResolveSkillsDirectory(settings.SkillsDirectory);
string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings.AgentsDirectory);

// BEFORE  
string? mcpConfigPath = McpConfigLoader.ResolveMcpConfigPath(settings);

// AFTER
string? mcpConfigPath = McpConfigLoader.ResolveMcpConfigPath(settings.McpConfigPath);
```

---

## 6.7 Update Command Handlers

Command files that reference CLI types need `using` updates. The logic stays the same:

```csharp
// In any command file that references these types:
using LlmTornado.Cli.Core;            // AgentSettings, AgentBuilder
using LlmTornado.Cli.Core.Agents;     // AgentDefinition, AgentDefinitionManager 
using LlmTornado.Cli.Core.Skills;     // SkillDefinition, SkillManager
```

---

## 6.8 Update Tests

The test project `LlmTornado.Cli.Tests` needs:

1. Add project reference to `LlmTornado.Cli.Core`
2. Update `using` statements for renamed types
3. The `InternalsVisibleTo` in the CLI `.csproj` still works for CLI-only types
4. For Core types (now `public`), no `InternalsVisibleTo` needed

---

## Verification

```powershell
cd src

# Core compiles
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj

# CLI compiles and works identically
dotnet build LlmTornado.Cli/LlmTornado.Cli.csproj

# Tests pass
dotnet test LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj
```

**Behavioral test:** Run the CLI and verify:
- Provider detection works
- Skills load from `skills/` directory
- Agent personas load from `Agents/built-in/` (now from Core's output)
- `/agent list` shows all 5 personas
- `/agent set architect` switches persona
- MCP tools load from `mcp.json`
- Tool optimizer works when tool count exceeds threshold

---

## Troubleshooting Common Issues

### Built-in persona files not found
If `AgentDefinitionLoader.ResolveBuiltInDirectory()` returns a path where the `.md` files don't exist, check that the Core project's `.csproj` has the `<Content Include="Agents\built-in\*.md">` with `CopyToOutputDirectory`. The Core library must copy these files to the output directory, and the CLI's output directory will get them transitively.

### Ambiguous type references
If both the CLI and Core have a type with the same name (during migration), you'll get CS0104. Fix: delete the CLI copy first, then add the Core `using`.

### `CliStorage` still used in `DiskSettingsPersistence`
That's intentional. `CliStorage` stays in the CLI project — it's the CLI-specific persistence layer. Only the `SaveJson` call is abstracted through `ISettingsPersistence`.

---

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Cli/LlmTornado.Cli.csproj` | Add Core reference, remove persona Content items |
| `LlmTornado.Cli/DiskSettingsPersistence.cs` | Create |
| `LlmTornado.Cli/ToolApprovalManager.cs` | Update (implement `IToolApproval`) |
| `LlmTornado.Cli/CliAgentBuilder.cs` | Rewrite (wrap Core `AgentBuilder`) |
| `LlmTornado.Cli/Program.cs` | Update (new types, injection) |
| `LlmTornado.Cli/Commands/*.cs` | Update `using` statements |
| All deleted files (see §6.2) | Delete |
| `LlmTornado.Cli.Tests/*.csproj` | Add Core reference |
| `LlmTornado.Cli.Tests/*.cs` | Update `using` statements |
