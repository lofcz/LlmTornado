# Working Directory Change Feature — Overview

## Problem Statement

The agent's working directory (`WorkingDirectory`) is currently set once during initialization via `ChatRuntimeControllerOptions` and only affects the agent's system prompt (`BuildSystemPrompt()` appends `"The user's current working directory is: {cwd}"`). Several CWD-relative resources — project-local skills (`./skills/`), custom agents (`./agents/`), MCP config (`./mcp.json`), and project `AGENTS.md` discovery — are resolved at startup and never updated.

There is no UI to change the working directory at runtime, and no mechanism to reload CWD-dependent resources when the directory changes.

## Goal

Make working directory a **first-class runtime setting**:

1. When the user changes the working directory in the Settings UI, **all CWD-dependent resources reload** from the new directory.
2. The agent's system prompt reflects the new CWD.
3. Paths that the host app explicitly configured (e.g., `options.SkillsDirectory = "/fixed/path"`) are **not** overridden by a CWD change — only paths that were left at their defaults (CWD-relative) are re-resolved.

## Scope

| Area | Changes |
|------|---------|
| `ISettingsController` interface | Add 2 methods: `GetWorkingDirectory()`, `ChangeWorkingDirectoryAsync(string)` |
| `ChatRuntimeController` | Add 3 boolean tracking fields, implement the 2 new methods, set tracking fields in `InitializeAsync()` |
| `ChatRuntimeController.Settings.cs` | Implementation of `ChangeWorkingDirectoryAsync` + `GetWorkingDirectory` |
| Demo `Settings.razor` | Insert new "General" tab as tab index 0 |
| Demo `GeneralPanel.razor` | New Blazor component with working directory input, apply button, resolved-path display |

## Out of Scope

| Item | Reason |
|------|--------|
| CLI Core changes | `AgentBuilder`, `SkillLoader`, `AgentDefinitionLoader`, `McpConfigLoader` already accept explicit paths — fully parameterized |
| Persisting working directory to `settings.json` | Working directory is a session/launch concern, not a user preference |
| Folder browser dialog | Text input with validation is sufficient; browser component can be added later |
| `ChatRuntimeControllerOptions` changes | The options class already has `WorkingDirectory`; we mutate it at runtime |

## Architecture

```
┌─────────────────────────┐
│ GeneralPanel.razor (UI) │──── ChangeWorkingDirectoryAsync(path) ────┐
└─────────────────────────┘                                           │
                                                                      ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ ChatRuntimeController (ISettingsController)                                  │
│                                                                              │
│  ChangeWorkingDirectoryAsync(path):                                          │
│    1. Validate + normalize path                                              │
│    2. Update _options.WorkingDirectory                                       │
│    3. Re-resolve non-explicit paths:                                         │
│       - skillsDir  = Path.Combine(path, "skills")   if !_skillsDirExplicit   │
│       - agentsDir  = Path.Combine(path, "agents")   if !_agentsDirExplicit   │
│       - mcpPath    = Path.Combine(path, "mcp.json") if !_mcpPathExplicit     │
│    4. Reload skills  → _skillManager.LoadSkills(skillsDir, globalDir)        │
│    5. Reload MCP     → _mcpLoader dispose + LoadAsync(mcpPath) if exists     │
│    6. Reload agents  → _agentManager.LoadAll(builtIn, global, agentsDir, cwd)│
│    7. Update _agentBuilder.WorkingDirectory                                  │
│    8. Rebuild runtime → _agentBuilder.Build(HandleRuntimeEvent)              │
│    9. Update UI dropdowns (agents may have changed)                          │
└──────────────────────────────────────────────────────────────────────────────┘
         │                        │                        │
         ▼                        ▼                        ▼
   SkillManager            McpConfigLoader        AgentDefinitionManager
   (already parameterized)  (already parameterized) (already parameterized)
```

## Phase Breakdown

| Phase | Document | Summary |
|-------|----------|---------|
| 1 | [01-phase-interface.md](01-phase-interface.md) | Add `GetWorkingDirectory()` and `ChangeWorkingDirectoryAsync()` to `ISettingsController` |
| 2 | [02-phase-controller.md](02-phase-controller.md) | Implement the methods in `ChatRuntimeController`, add tracking fields |
| 3 | [03-phase-demo-ui.md](03-phase-demo-ui.md) | Create `GeneralPanel.razor` and wire into `Settings.razor` |

## Verification

1. **Build**: `cd src && dotnet build LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj`
2. **Manual Testing**:
   - Launch demo, navigate to Settings → General
   - Change working directory to a folder with `skills/`, `agents/`, `mcp.json`
   - Switch to Skills tab → verify skills from new directory appear
   - Switch to Agents tab → verify custom agents from new directory appear
   - Switch to MCP Servers tab → verify servers from new `mcp.json` appear
   - Change to a folder without skills/agents → verify graceful empty state
   - Chat with the agent → verify system prompt shows new CWD
   - Verify original providers tab still works (unaffected by CWD)

## Decisions Log

| Decision | Rationale |
|----------|-----------|
| General tab as tab 0 (first position) | Working directory is the most fundamental setting — it affects all other tabs |
| Only re-resolve non-explicit paths | Preserves intentional fixed paths set by the host app via options |
| No persistence to `settings.json` | Working directory is a deployment/launch concern; the demo bootstraps it via `ChatRuntimeControllerOptions` |
| MCP reload disposes old servers | `McpConfigLoader.ReloadAsync()` already handles dispose → clear → re-load; we use `LoadAsync(newPath)` for path changes |
| No CLI Core modifications needed | All core managers already accept explicit paths — they're fully parameterized |
