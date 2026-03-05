# Phase 1: Add Working Directory Methods to ISettingsController

## Objective

Add two new methods to the `ISettingsController` interface so that any settings UI can read and change the agent's working directory at runtime.

## File to Modify

**`src/LlmTornado.Cli.Blazor/ISettingsController.cs`**

## Current State (lines 1–180)

The interface currently has three sections:
- **MCP Servers** (lines 18–75): 10 methods for CRUD + test + reload
- **Skills** (lines 77–122): 8 methods for list/toggle/refresh/import
- **Agents** (lines 124–172): 8 methods for list/CRUD/refresh/tool-picker

There is **no** working directory section.

## Exact Changes

### Add a new section: "Working Directory"

Insert a new section **before** the MCP Servers section (since working directory is the most fundamental setting that affects skills, agents, and MCP path resolution). The new section goes between the interface opening brace and the MCP Servers comment block.

**Insert after line 13** (after `public interface ISettingsController`'s opening brace `{`), **before line 14** (the MCP Servers section comment):

```csharp
    // ─────────────────────────────────────────────
    // Working Directory
    // ─────────────────────────────────────────────

    /// <summary>
    /// Get the current effective working directory.
    /// Returns the explicitly configured working directory, or Environment.CurrentDirectory if none.
    /// </summary>
    string GetWorkingDirectory();

    /// <summary>
    /// Change the agent's working directory and reload all CWD-dependent resources.
    /// This re-resolves project-local skills, custom agents, MCP config, and the agent's
    /// system prompt CWD context — but only for paths that were not explicitly overridden
    /// via ChatRuntimeControllerOptions at startup.
    /// Throws DirectoryNotFoundException if the path does not exist.
    /// </summary>
    Task ChangeWorkingDirectoryAsync(string path);

```

### Method Contracts

#### `string GetWorkingDirectory()`

| Aspect | Detail |
|--------|--------|
| **Returns** | The effective working directory: either the explicitly set `_options.WorkingDirectory` or `Environment.CurrentDirectory` if not set |
| **Thread safety** | Read-only, safe to call at any time |
| **Side effects** | None |
| **Error handling** | Never throws; always returns a valid path string |

#### `Task ChangeWorkingDirectoryAsync(string path)`

| Aspect | Detail |
|--------|--------|
| **Parameter** | `path` — absolute or relative path to the new working directory |
| **Returns** | `Task` — completes when all resources have been reloaded and the agent has been rebuilt |
| **Preconditions** | The path must exist on disk (`Directory.Exists(path)` must return `true`) |
| **Side effects** | Reloads skills, MCP servers, agents; rebuilds the agent runtime; updates UI dropdowns |
| **Error handling** | Throws `DirectoryNotFoundException` if the path doesn't exist. The caller (UI) should catch and display an error message. |
| **Path normalization** | The implementation will call `Path.GetFullPath(path)` to normalize relative paths |
| **Explicit path protection** | If `options.SkillsDirectory`, `options.AgentsDirectory`, or `options.McpConfigPath` were explicitly set at startup, those paths are **not** re-resolved from the new CWD |

### Resulting Interface Outline

After the change, the interface sections will be:

```
ISettingsController
├── Working Directory (2 methods)  ← NEW
├── MCP Servers (10 methods)       ← unchanged
├── Skills (8 methods)             ← unchanged
└── Agents (8 methods)             ← unchanged
```

## Diff Preview

```diff
 public interface ISettingsController
 {
+    // ─────────────────────────────────────────────
+    // Working Directory
+    // ─────────────────────────────────────────────
+
+    /// <summary>
+    /// Get the current effective working directory.
+    /// Returns the explicitly configured working directory, or Environment.CurrentDirectory if none.
+    /// </summary>
+    string GetWorkingDirectory();
+
+    /// <summary>
+    /// Change the agent's working directory and reload all CWD-dependent resources.
+    /// This re-resolves project-local skills, custom agents, MCP config, and the agent's
+    /// system prompt CWD context — but only for paths that were not explicitly overridden
+    /// via ChatRuntimeControllerOptions at startup.
+    /// Throws DirectoryNotFoundException if the path does not exist.
+    /// </summary>
+    Task ChangeWorkingDirectoryAsync(string path);
+
     // ─────────────────────────────────────────────
     // MCP Servers
     // ─────────────────────────────────────────────
```

## Verification

After this phase, the project will **not** build because `ChatRuntimeController` implements `ISettingsController` but doesn't yet have these two methods. That's expected — Phase 2 implements them.

To verify the interface change is syntactically correct in isolation, you can check for compile errors in just the interface file and confirm the two new methods appear.

## Dependencies

- **Depends on**: Nothing (first phase)
- **Blocks**: Phase 2 (controller implementation) and Phase 3 (demo UI calls these methods)
