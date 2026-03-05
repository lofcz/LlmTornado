# Phase 2: Implement Working Directory Change in ChatRuntimeController

## Objective

Add three boolean tracking fields to `ChatRuntimeController.cs` and implement `GetWorkingDirectory()` + `ChangeWorkingDirectoryAsync(string path)` in `ChatRuntimeController.Settings.cs`.

## Files to Modify

1. **`src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.cs`** — add tracking fields + set them in `InitializeAsync()`
2. **`src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Settings.cs`** — add the two method implementations

---

## File 1: ChatRuntimeController.cs

### Change A: Add Three Tracking Fields

**Purpose**: Track which paths were explicitly set by the host app at startup. When `ChangeWorkingDirectoryAsync` runs, only paths that were NOT explicitly set get re-resolved from the new CWD.

**Location**: After the existing field group (line ~60), in the "Settings persistence path" area. Insert after `private string _settingsPath = string.Empty;` and before `public IChatUi? Ui { get; set; }`.

**Insert these 5 lines:**

```csharp
    // Track whether paths were explicitly set (vs. defaulting to CWD-relative).
    // When changing working directory, only re-resolve paths that were not explicit.
    private bool _skillsDirExplicit;
    private bool _agentsDirExplicit;
    private bool _mcpPathExplicit;
```

### Change B: Set Tracking Fields in InitializeAsync()

**Location**: In `InitializeAsync()`, after `// 2. Resolve paths` (line ~84). The tracking fields must be set **before** the path-resolution code runs, so they capture whether the user explicitly provided paths.

**Insert immediately after** the line `string appData = Path.Combine(...)` block (before `string conversationsDir = ...`):

```csharp
            // Track which paths were explicitly configured (not defaulting to CWD-relative)
            _skillsDirExplicit = _options.SkillsDirectory is not null;
            _agentsDirExplicit = _options.AgentsDirectory is not null;
            _mcpPathExplicit = _options.McpConfigPath is not null;
```

**Exact insertion point** — insert AFTER this line:

```csharp
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "llmtornado");
```

and BEFORE this line:

```csharp
            string conversationsDir = _options.ConversationsDirectory
                ?? Path.Combine(appData, "conversations");
```

### Resulting Field Layout (after both changes)

```csharp
    // Settings persistence path
    private string _settingsPath = string.Empty;

    // Track whether paths were explicitly set (vs. defaulting to CWD-relative).
    // When changing working directory, only re-resolve paths that were not explicit.
    private bool _skillsDirExplicit;
    private bool _agentsDirExplicit;
    private bool _mcpPathExplicit;

    public IChatUi? Ui { get; set; }
```

### Diff Preview for ChatRuntimeController.cs

```diff
     // Settings persistence path
     private string _settingsPath = string.Empty;
 
+    // Track whether paths were explicitly set (vs. defaulting to CWD-relative).
+    // When changing working directory, only re-resolve paths that were not explicit.
+    private bool _skillsDirExplicit;
+    private bool _agentsDirExplicit;
+    private bool _mcpPathExplicit;
+
     public IChatUi? Ui { get; set; }
```

```diff
             string appData = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                 "llmtornado");
+
+            // Track which paths were explicitly configured (not defaulting to CWD-relative)
+            _skillsDirExplicit = _options.SkillsDirectory is not null;
+            _agentsDirExplicit = _options.AgentsDirectory is not null;
+            _mcpPathExplicit = _options.McpConfigPath is not null;
+
             string conversationsDir = _options.ConversationsDirectory
                 ?? Path.Combine(appData, "conversations");
```

---

## File 2: ChatRuntimeController.Settings.cs

### Change: Add Working Directory Section

**Location**: Insert a new section at the **top** of the file, right after the class declaration and before the MCP Servers section. This mirrors the interface order from Phase 1.

**Insert after line 16** (after `public sealed partial class ChatRuntimeController : ISettingsController`'s opening brace `{`), **before line 17** (the MCP Servers section comment):

```csharp
    // ─────────────────────────────────────────────
    // Working Directory
    // ─────────────────────────────────────────────

    public string GetWorkingDirectory()
        => _options.WorkingDirectory ?? Environment.CurrentDirectory;

    public async Task ChangeWorkingDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        path = Path.GetFullPath(path);
        _options.WorkingDirectory = path;

        // Re-resolve paths that were not explicitly set at startup
        string skillsDir = _skillsDirExplicit
            ? _options.SkillsDirectory!
            : Path.Combine(path, "skills");
        string agentsDir = _agentsDirExplicit
            ? _options.AgentsDirectory!
            : Path.Combine(path, "agents");
        string? mcpPath = _mcpPathExplicit
            ? _options.McpConfigPath
            : Path.Combine(path, "mcp.json");

        // 1. Reload skills
        if (_skillManager is not null)
        {
            _skillManager.LoadSkills(skillsDir, _options.GlobalSkillsDirectory);
        }

        // 2. Reload MCP servers (dispose old, load new if config exists)
        if (_mcpLoader is not null)
        {
            await _mcpLoader.DisposeAsync();
            _mcpLoader = new McpConfigLoader();

            if (mcpPath is not null && File.Exists(mcpPath))
            {
                await _mcpLoader.LoadAsync(mcpPath);
            }
        }

        // 3. Reload agents
        if (_agentManager is not null)
        {
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string? globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
            _agentManager.LoadAll(builtInDir, globalDir, agentsDir, path);

            // Update the UI agent dropdown
            List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
                .Where(a => a.IsPersona)
                .Select(MapAgent)
                .ToList();
            Ui?.SetAgents(uiAgents);
            Ui?.SetSelectedAgent(_agentManager.ActivePersonaName);
        }

        // 4. Rebuild the agent runtime with updated tools and system prompt
        if (_agentBuilder is not null)
        {
            _agentBuilder.WorkingDirectory = path;
            _runtime = _agentBuilder.Build(HandleRuntimeEvent);
        }
    }
```

---

## Implementation Details

### GetWorkingDirectory()

Simple one-liner. Returns the effective CWD:
- If `_options.WorkingDirectory` was set (either at startup or by a previous `ChangeWorkingDirectoryAsync` call), return it.
- Otherwise, return `Environment.CurrentDirectory`.

### ChangeWorkingDirectoryAsync(string path) — Step by Step

#### Step 1: Validate and normalize

```csharp
if (!Directory.Exists(path))
    throw new DirectoryNotFoundException($"Directory not found: {path}");

path = Path.GetFullPath(path);
_options.WorkingDirectory = path;
```

- `Directory.Exists` prevents use of non-existent paths.
- `Path.GetFullPath` normalizes relative paths (e.g., `"../other-project"` → absolute path).
- We mutate `_options.WorkingDirectory` so that subsequent `GetWorkingDirectory()` calls and any code reading `_options.WorkingDirectory` see the new value.

#### Step 2: Re-resolve CWD-dependent paths

```csharp
string skillsDir = _skillsDirExplicit
    ? _options.SkillsDirectory!
    : Path.Combine(path, "skills");
```

The pattern is the same for all three paths:
- If the host app explicitly set `options.SkillsDirectory` at startup → keep using that fixed path.
- If left null at startup (meaning it defaulted to `./skills/` relative to CWD) → re-derive from the new CWD.

This ensures `ChangeWorkingDirectoryAsync` never stomps on intentionally fixed paths.

#### Step 3: Reload skills

```csharp
_skillManager.LoadSkills(skillsDir, _options.GlobalSkillsDirectory);
```

`SkillManager.LoadSkills()` already clears and re-discovers skills. Global skills directory is unchanged (it's not CWD-relative).

#### Step 4: Reload MCP

```csharp
await _mcpLoader.DisposeAsync();
_mcpLoader = new McpConfigLoader();

if (mcpPath is not null && File.Exists(mcpPath))
{
    await _mcpLoader.LoadAsync(mcpPath);
}
```

We create a fresh `McpConfigLoader` because:
- The old loader's `_configPath` points to the old `mcp.json`.
- `ReloadAsync()` would reload from the old path — we need a new path.
- `DisposeAsync()` shuts down the old MCP server connections gracefully.
- The new loader gets the new `mcpPath`.
- If the new CWD has no `mcp.json`, we end up with an empty MCP loader (no servers, no tools) — which is the correct behavior.

**Important**: We reassign `_mcpLoader` (not just the path) because `AgentBuilder` holds a reference to `_mcpLoader`. Since `AgentBuilder` reads `_mcpLoader.AllTools` during `CollectTools()`, and we're about to call `Build()`, the `AgentBuilder` sees the new tools via the same reference. **Wait** — actually `AgentBuilder` holds the reference passed at construction. Reassigning `_mcpLoader` on the controller doesn't update the builder's reference.

**Correction**: We need to handle this carefully. Looking at the `AgentBuilder` constructor:

```csharp
public AgentBuilder(
    TornadoApi api,
    ChatModel activeModel,
    SkillManager skillManager,
    McpConfigLoader mcpLoader,  // ← stored as _mcpLoader
    ...)
```

The `AgentBuilder` stores its own `_mcpLoader` reference. When we reassign `_mcpLoader` on the controller, the builder still points to the old one. 

**Two solutions**:
1. Add a method to `AgentBuilder` to update the MCP loader reference.
2. Use the existing `McpConfigLoader.ReloadAsync()` but set the new config path first.

Looking at `McpConfigLoader`, the `_configPath` field is private with no setter. `LoadAsync(configPath)` sets it. And `ReloadAsync()` calls `LoadAsync(_configPath)`. So the correct approach is:

```csharp
// Dispose old servers, clear tools/statuses
await _mcpLoader.ReloadAsync(); // This disposes + clears + reloads from _configPath

// But we need to load from the NEW path... ReloadAsync uses _configPath.
// We need to call LoadAsync with the new path instead.
```

**Revised approach**: Since `McpConfigLoader` has no public way to change `_configPath` without calling `LoadAsync()`, and since we need the builder's reference to stay valid, we should:

1. Dispose the MCP loader's servers (via `ReloadAsync` which disposes → clears → reloads).
2. But `ReloadAsync` reloads from the OLD path... 

**Final approach**: We dispose the loader, recreate it, and **also recreate the AgentBuilder**. But `AgentBuilder` has many constructor parameters...

**Better final approach**: Actually, let's look at what `ReloadAsync` does:

```csharp
public async Task ReloadAsync(Action<string>? log = null)
{
    await DisposeAsync();      // dispose servers
    _servers.Clear();          // clear lists
    _allTools.Clear();
    _serverStatuses.Clear();

    if (_configPath is not null)
        await LoadAsync(_configPath, log); // reload from stored path
}
```

And `LoadAsync` sets `_configPath`. So if we call `LoadAsync(newPath)` directly (not `ReloadAsync`), it works... but it doesn't dispose old servers first. We need to dispose them.

The cleanest approach: **Call dispose on the loader manually, clear its lists, then call LoadAsync with the new path**. But the internal lists are private.

**Simplest correct approach**: Since the `_mcpLoader` field on the controller is the same object that `AgentBuilder` holds, and we can't change the private `_configPath`, the simplest solution is:

1. Call `_mcpLoader.ReloadAsync()` — this disposes servers, clears lists, and reloads from OLD path.
2. Then call `_mcpLoader.ReloadAsync()` again... no, that's wrong.

**Actually, the cleanest approach is to dispose + call LoadAsync with the new path.** Looking more carefully at `ReloadAsync`:

```csharp
await DisposeAsync();   // disposes servers
_servers.Clear();
_allTools.Clear();
_serverStatuses.Clear();
```

After `ReloadAsync()`, if the old config path still exists, it reloads from there. If we then call `LoadAsync(newPath)`, the tools will be ADDED to the existing list (not cleared). That's wrong — we'd have duplicates.

**Correct approach**: 
1. Call `_mcpLoader.ReloadAsync()` — this clears everything and reloads from old path.
2. Then immediately clear again and load from new path... No, that's wasteful.

**The actual simplest correct approach**: Looking at the controller's `ReloadMcpConfigAsync()` pattern, it just calls `_mcpLoader.ReloadAsync()`. For changing the config path, we need a different pattern.

Since `McpConfigLoader` doesn't expose a way to cleanly change path + reload, and since modifying `McpConfigLoader` is "out of scope" for this feature (it's in CLI Core), we should use a small surgical addition to `McpConfigLoader`:

**Add one method to McpConfigLoader** — `public async Task LoadFromPathAsync(string newConfigPath)`:

```csharp
public async Task LoadFromPathAsync(string newConfigPath, Action<string>? log = null)
{
    await DisposeAsync();
    _servers.Clear();
    _allTools.Clear();
    _serverStatuses.Clear();
    
    if (File.Exists(newConfigPath))
        await LoadAsync(newConfigPath, log);
}
```

This is a minimal, focused addition that keeps the same McpConfigLoader instance (so the AgentBuilder's reference stays valid) and cleanly transitions to a new config path.

**This means Phase 2 also modifies `McpConfigLoader.cs` in CLI Core (one small method addition).**

---

## Revised File List

1. **`src/LlmTornado.Cli.Core/Mcp/McpConfigLoader.cs`** — add `LoadFromPathAsync` method
2. **`src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.cs`** — add tracking fields + set in init
3. **`src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Settings.cs`** — add implementations

---

## File 1 (Revised): McpConfigLoader.cs

### Add LoadFromPathAsync Method

**Location**: Insert after the existing `ReloadAsync` method (after line ~101), before the `InitializeServer` method.

```csharp
    /// <summary>
    /// Switch to a new config file path, disposing existing servers first.
    /// If the new path does not exist, the loader ends up empty (no servers, no tools).
    /// </summary>
    public async Task LoadFromPathAsync(string? newConfigPath, Action<string>? log = null)
    {
        // Dispose existing servers and clear state
        await DisposeAsync();
        _servers.Clear();
        _allTools.Clear();
        _serverStatuses.Clear();

        if (newConfigPath is not null && File.Exists(newConfigPath))
            await LoadAsync(newConfigPath, log);
    }
```

**Diff Preview:**

```diff
     public async Task ReloadAsync(Action<string>? log = null)
     {
         await DisposeAsync();
         _servers.Clear();
         _allTools.Clear();
         _serverStatuses.Clear();
 
         if (_configPath is not null)
             await LoadAsync(_configPath, log);
     }
 
+    /// <summary>
+    /// Switch to a new config file path, disposing existing servers first.
+    /// If the new path does not exist, the loader ends up empty (no servers, no tools).
+    /// </summary>
+    public async Task LoadFromPathAsync(string? newConfigPath, Action<string>? log = null)
+    {
+        // Dispose existing servers and clear state
+        await DisposeAsync();
+        _servers.Clear();
+        _allTools.Clear();
+        _serverStatuses.Clear();
+
+        if (newConfigPath is not null && File.Exists(newConfigPath))
+            await LoadAsync(newConfigPath, log);
+    }
+
     private async Task InitializeServer(McpServerEntry entry, Action<string>? log)
```

---

## Revised ChangeWorkingDirectoryAsync Implementation

With `LoadFromPathAsync` available, the MCP reload step becomes clean:

```csharp
    public async Task ChangeWorkingDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        path = Path.GetFullPath(path);
        _options.WorkingDirectory = path;

        // Re-resolve paths that were not explicitly set at startup
        string skillsDir = _skillsDirExplicit
            ? _options.SkillsDirectory!
            : Path.Combine(path, "skills");
        string agentsDir = _agentsDirExplicit
            ? _options.AgentsDirectory!
            : Path.Combine(path, "agents");
        string? mcpPath = _mcpPathExplicit
            ? _options.McpConfigPath
            : Path.Combine(path, "mcp.json");

        // 1. Reload skills
        if (_skillManager is not null)
        {
            _skillManager.LoadSkills(skillsDir, _options.GlobalSkillsDirectory);
        }

        // 2. Reload MCP servers from new config path  
        if (_mcpLoader is not null)
        {
            await _mcpLoader.LoadFromPathAsync(mcpPath);
        }

        // 3. Reload agents
        if (_agentManager is not null)
        {
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string? globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
            _agentManager.LoadAll(builtInDir, globalDir, agentsDir, path);

            List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
                .Where(a => a.IsPersona)
                .Select(MapAgent)
                .ToList();
            Ui?.SetAgents(uiAgents);
            Ui?.SetSelectedAgent(_agentManager.ActivePersonaName);
        }

        // 4. Rebuild the agent runtime with updated tools and system prompt
        if (_agentBuilder is not null)
        {
            _agentBuilder.WorkingDirectory = path;
            _runtime = _agentBuilder.Build(HandleRuntimeEvent);
        }
    }
```

---

## Impact on Existing Methods

### RefreshSkills()

Currently uses `_options.SkillsDirectory ?? Path.GetFullPath("skills")`. After a CWD change, if `_skillsDirExplicit` is false, the skills directory has been updated (but only in our `ChangeWorkingDirectoryAsync` local variable, not stored anywhere persistent).

**Problem**: `RefreshSkills()` still reads `_options.SkillsDirectory ?? Path.GetFullPath("skills")`. After `ChangeWorkingDirectoryAsync`, `_options.SkillsDirectory` is still null (we only changed `_options.WorkingDirectory`). And `Path.GetFullPath("skills")` resolves relative to `Environment.CurrentDirectory`, not our new working directory.

**Solution**: We need to **also update** `_options.SkillsDirectory` (etc.) when they weren't explicitly set. This way, subsequent calls to `RefreshSkills()`, `GetSkillsDirectory()`, `RefreshAgents()`, `GetAgentsDirectory()`, etc., all see the correct path.

But wait — if we set `_options.SkillsDirectory` to the derived value, then `_skillsDirExplicit` is no longer needed (the options now have a concrete value). The next time `ChangeWorkingDirectoryAsync` is called, we need to know whether to re-derive or keep the value. The tracking field preserves this knowledge.

**Revised step 2 in ChangeWorkingDirectoryAsync**: Also update `_options` properties for non-explicit paths:

```csharp
        // Re-resolve paths that were not explicitly set at startup.
        // Update _options so that RefreshSkills/RefreshAgents/GetSkillsDirectory etc. stay consistent.
        if (!_skillsDirExplicit)
            _options.SkillsDirectory = Path.Combine(path, "skills");
        if (!_agentsDirExplicit)
            _options.AgentsDirectory = Path.Combine(path, "agents");
        if (!_mcpPathExplicit)
            _options.McpConfigPath = Path.Combine(path, "mcp.json");

        string skillsDir = _options.SkillsDirectory!;
        string agentsDir = _options.AgentsDirectory!;
        string? mcpPath = _options.McpConfigPath;
```

Now `RefreshSkills()`, `GetSkillsDirectory()`, `RefreshAgents()`, `GetAgentsDirectory()`, `GetMcpConfigPath()` all read from `_options` and automatically see the updated paths.

---

## Final Complete Implementation for ChatRuntimeController.Settings.cs

```csharp
    // ─────────────────────────────────────────────
    // Working Directory
    // ─────────────────────────────────────────────

    public string GetWorkingDirectory()
        => _options.WorkingDirectory ?? Environment.CurrentDirectory;

    public async Task ChangeWorkingDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        path = Path.GetFullPath(path);
        _options.WorkingDirectory = path;

        // Re-resolve paths that were not explicitly set at startup.
        // Update _options so that RefreshSkills/RefreshAgents/GetSkillsDirectory etc. stay consistent.
        if (!_skillsDirExplicit)
            _options.SkillsDirectory = Path.Combine(path, "skills");
        if (!_agentsDirExplicit)
            _options.AgentsDirectory = Path.Combine(path, "agents");
        if (!_mcpPathExplicit)
            _options.McpConfigPath = Path.Combine(path, "mcp.json");

        // 1. Reload skills
        if (_skillManager is not null)
        {
            _skillManager.LoadSkills(_options.SkillsDirectory!, _options.GlobalSkillsDirectory);
        }

        // 2. Reload MCP servers from new config path
        if (_mcpLoader is not null)
        {
            await _mcpLoader.LoadFromPathAsync(_options.McpConfigPath);
        }

        // 3. Reload agents
        if (_agentManager is not null)
        {
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string? globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
            _agentManager.LoadAll(builtInDir, globalDir, _options.AgentsDirectory!, path);

            List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
                .Where(a => a.IsPersona)
                .Select(MapAgent)
                .ToList();
            Ui?.SetAgents(uiAgents);
            Ui?.SetSelectedAgent(_agentManager.ActivePersonaName);
        }

        // 4. Rebuild the agent runtime with updated tools and system prompt
        if (_agentBuilder is not null)
        {
            _agentBuilder.WorkingDirectory = path;
            _runtime = _agentBuilder.Build(HandleRuntimeEvent);
        }
    }
```

---

## Edge Cases

| Case | Behavior |
|------|----------|
| Path doesn't exist | `DirectoryNotFoundException` thrown; nothing changes |
| Path has no `skills/` subfolder | `SkillManager.LoadSkills` finds nothing; skills list becomes empty (global skills still present) |
| Path has no `mcp.json` | `LoadFromPathAsync` sees `File.Exists` → false; MCP tools become empty |
| Path has no `agents/` subfolder | Agent discovery returns only built-in + global agents |
| Same path as current | All resources reload (harmless refresh); no-op semantically |
| Relative path (e.g., `"../other"`) | Normalized to absolute via `Path.GetFullPath()` before use |
| Skills dir was explicitly set | `_skillsDirExplicit == true` → `_options.SkillsDirectory` not changed → skills not affected |
| Called before `InitializeAsync` | All null checks prevent NPE; method returns after doing nothing useful (degenerate case) |

## Verification

After Phase 1 + Phase 2, the project should build successfully:

```bash
cd src && dotnet build LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj
```

The two new interface methods are now implemented. The demo won't use them yet (that's Phase 3).

## Dependencies

- **Depends on**: Phase 1 (interface methods must exist)
- **Blocks**: Phase 3 (demo UI calls `GetWorkingDirectory()` and `ChangeWorkingDirectoryAsync()`)
