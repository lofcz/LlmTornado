# Phase 8 — Cleanup and Verify

## Objective

Delete all obsolete files from the ACP server, update the solution file, verify everything builds, and run a final integration smoke test.

---

## 8.1 Delete Obsolete ACP Server Files

These files are replaced by Core's shared infrastructure:

### Skills Directory (Entire `Skills/` Folder)

```
LlmTornado.Acp.Server/Skills/AgentSkill.cs         → replaced by Core's AgentDefinition
LlmTornado.Acp.Server/Skills/BuiltInSkills.cs       → replaced by Core's AgentDefinitionLoader
LlmTornado.Acp.Server/Skills/SkillLoader.cs         → replaced by Core's SkillLoader
LlmTornado.Acp.Server/Skills/agent.skill.md         → replaced by Core's built-in personas
LlmTornado.Acp.Server/Skills/chat.skill.md          → replaced by Core's built-in personas
LlmTornado.Acp.Server/Skills/planning.skill.md      → replaced by Core's built-in personas
LlmTornado.Acp.Server/Skills/refactoring.skill.md   → deleted (orchestrated refactor dropped)
```

**Delete the entire `Skills/` directory.**

### Orchestrated Refactoring Files

```
LlmTornado.Acp.Server/FileRefactoringOrchestrationConfiguration.cs
LlmTornado.Acp.Server/FileRefactoringRunnables.cs
LlmTornado.Acp.Server/FileRefactoringModels.cs
```

These implemented the multi-agent refactoring pipeline that was dropped (design decision #4).

### SkillRuntimeConfiguration

```
LlmTornado.Acp.Server/SkillRuntimeConfiguration.cs
```

Replaced by `AgentBuilder` creating a `SingletonRuntimeConfiguration` directly.

### Shell Command

```powershell
# From src/ directory
$acpServer = "LlmTornado.Acp.Server"
Remove-Item "$acpServer/Skills" -Recurse -Force
Remove-Item "$acpServer/FileRefactoringOrchestrationConfiguration.cs"
Remove-Item "$acpServer/FileRefactoringRunnables.cs"
Remove-Item "$acpServer/FileRefactoringModels.cs"
Remove-Item "$acpServer/SkillRuntimeConfiguration.cs"
```

---

## 8.2 Update Solution File

Add `LlmTornado.Cli.Core` to the solution. The solution file is `LlmTornado.slnx` (XML format).

```powershell
cd src
dotnet sln LlmTornado.slnx add LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
```

Verify it appears in the same folder group as `LlmTornado.Cli`:

```xml
<!-- In LlmTornado.slnx -->
<Folder Name="/CLI/">
  <Project Path="LlmTornado.Cli/LlmTornado.Cli.csproj" />
  <Project Path="LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj" />
  <Project Path="LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj" />
</Folder>
```

If it ends up at root level, manually move the `<Project>` element into the correct `<Folder>`.

---

## 8.3 Build Verification

Build all affected projects in dependency order:

```powershell
cd src

# 1. Core library (new)
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
# Expected: Build succeeded. 0 Error(s).

# 2. CLI (should still work, now references Core)
dotnet build LlmTornado.Cli/LlmTornado.Cli.csproj
# Expected: Build succeeded. Some pre-existing warnings in LlmTornado.Agents are normal.

# 3. ACP Server (fully rewritten)
dotnet build LlmTornado.Acp.Server/LlmTornado.Acp.Server.csproj
# Expected: Build succeeded. 0 Error(s).
```

**Common build issues and fixes:**

| Error | Cause | Fix |
|-------|-------|-----|
| `CS0246: type 'AgentDefinition' not found` | Missing `using LlmTornado.Cli.Core.Agents` | Add the using statement |
| `CS0122: inaccessible due to protection level` | Type still `internal` in Core | Change to `public` in the Core project |
| `CS0234: namespace 'Mcp' does not exist` | Missing project reference | Add `LlmTornado.Mcp` reference to `.csproj` |
| `CS0117: 'SkillLoader' does not contain 'ResolveSkillsDirectory'` | Method signature changed | Check parameter types match the new `string?` signature |

---

## 8.4 Run CLI Tests

The existing CLI tests should still pass since Core was extracted without changing behavior:

```powershell
cd src
dotnet test LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj --filter "BundledSkills"
# Expected: Passed!
```

If the test project needs NuGet source configuration:

```powershell
dotnet test LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj --filter "BundledSkills" --source https://api.nuget.org/v3/index.json
```

---

## 8.5 Manual Smoke Test — ACP Server

### Prerequisites
Set at least one provider API key:
```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
# or
$env:OPENAI_API_KEY = "sk-..."
```

### Basic Startup Test
```powershell
cd src
dotnet run --project LlmTornado.Acp.Server/LlmTornado.Acp.Server.csproj 2>&1
```

You should see on stderr:
```
[ACP] LlmTornado ACP Server starting...
[ACP] Detected 1 provider(s): Anthropic
[ACP] Active model: claude-sonnet-4-20250514
[ACP] Loaded 5 skill(s)
[ACP] Loaded 5 agent persona(s): default, architect, code-reviewer, debugger, docs-writer
[ACP] Listening on stdin/stdout...
```

### JSON-RPC Handshake Test

Send the ACP initialize request via stdin:

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"clientInfo":{"name":"test","version":"1.0"}}}
```

Expected response should include:
- `serverInfo` with name "LlmTornado"
- `capabilities` with `setMode: true`, `setConfigOption: true`


### New Session Test

```json
{"jsonrpc":"2.0","id":2,"method":"session/new","params":{"cwd":"C:\\Users\\johnl\\source\\repos\\lofcz\\LLMTornado"}}
```

Expected response should include:
- A `sessionId`
- `modes.availableModes` with 5 entries (default, architect, code-reviewer, debugger, docs-writer)
- `configOptions` with a "model" option containing provider-grouped model lists

---

## 8.6 Verify No Stale Imports

After the rewrite, check that no remaining file references deleted types:

```powershell
cd src/LlmTornado.Acp.Server
# Should find zero results for deleted types
Select-String -Pattern "AgentSkill|BuiltInSkills|SkillLoader|FileRefactoring|SkillRuntimeConfiguration" -Path *.cs -Recurse
```

---

## 8.7 Verify InternalsVisibleTo

If the CLI test project uses `[InternalsVisibleTo]` to test internal types, ensure it now points at the right assembly:

- Types that moved to Core: test project must reference `LlmTornado.Cli.Core`
- The Core project may need `[InternalsVisibleTo("LlmTornado.Cli.Tests")]` if any tested types are `internal` in Core

Check the test project's `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\LlmTornado.Cli\LlmTornado.Cli.csproj" />
  <ProjectReference Include="..\LlmTornado.Cli.Core\LlmTornado.Cli.Core.csproj" />
</ItemGroup>
```

---

## Final State Summary

After all phases are complete:

| Project | Role | Status |
|---------|------|--------|
| `LlmTornado.Cli.Core` | Shared agent infrastructure | **NEW** |
| `LlmTornado.Cli` | Console REPL | Updated (thin wrapper over Core) |
| `LlmTornado.Acp.Server` | ACP JSON-RPC server | **REWRITTEN** |
| `LlmTornado.Acp` | ACP protocol library | Unchanged |

### Files Created
- `LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj`
- `LlmTornado.Cli.Core/ISettingsPersistence.cs`
- `LlmTornado.Cli.Core/IToolApproval.cs`
- `LlmTornado.Cli.Core/Agents/AgentDefinition.cs`
- `LlmTornado.Cli.Core/Agents/AgentDefinitionLoader.cs`
- `LlmTornado.Cli.Core/Agents/AgentDefinitionManager.cs`
- `LlmTornado.Cli.Core/Agents/built-in/*.md` (embedded resources)
- `LlmTornado.Cli.Core/Skills/Skill.cs`
- `LlmTornado.Cli.Core/Skills/SkillLoader.cs`
- `LlmTornado.Cli.Core/Skills/SkillManager.cs`
- `LlmTornado.Cli.Core/Skills/ScriptToolBuilder.cs`
- `LlmTornado.Cli.Core/Providers/ProviderDetector.cs`
- `LlmTornado.Cli.Core/Providers/ProviderDetectionResult.cs`
- `LlmTornado.Cli.Core/Tools/ToolOptimizer.cs`
- `LlmTornado.Cli.Core/Mcp/McpConfigLoader.cs`
- `LlmTornado.Cli.Core/Mcp/McpConfigModel.cs`
- `LlmTornado.Cli.Core/AgentSettings.cs`
- `LlmTornado.Cli.Core/AgentBuilder.cs`
- `LlmTornado.Acp.Server/NoOpSettingsPersistence.cs`
- `LlmTornado.Acp.Server/AutoApproveToolApproval.cs`

### Files Deleted
- `LlmTornado.Acp.Server/Skills/` (entire directory)
- `LlmTornado.Acp.Server/FileRefactoringOrchestrationConfiguration.cs`
- `LlmTornado.Acp.Server/FileRefactoringRunnables.cs`
- `LlmTornado.Acp.Server/FileRefactoringModels.cs`
- `LlmTornado.Acp.Server/SkillRuntimeConfiguration.cs`

### Files Modified
- `LlmTornado.Acp.Server/LlmTornado.Acp.Server.csproj` — added Core + MCP references
- `LlmTornado.Acp.Server/Program.cs` — rewritten
- `LlmTornado.Acp.Server/TornadoAcpRuntime.cs` — rewritten
- `LlmTornado.Cli/LlmTornado.Cli.csproj` — added Core reference
- `LlmTornado.Cli/Program.cs` — updated to use Core types
- CLI files that had types extracted — redirected to import from Core
- `LlmTornado.slnx` — added LlmTornado.Cli.Core
