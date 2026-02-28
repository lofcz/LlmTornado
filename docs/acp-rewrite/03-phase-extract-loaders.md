# Phase 3 — Extract Loaders to Core

## Objective

Move the stateless loader/parser classes from the CLI to Core. These handle filesystem discovery and YAML frontmatter parsing. The only refactoring needed is to **parameterize directory resolution** — instead of reading `CliSettings` directly, accept a path string parameter.

## Why This Is Easy

The loaders are `static partial` classes with a clean separation:
- **One method** reads `CliSettings` to resolve a directory path
- **All other methods** are pure functions: `(string path or content) → parsed result`

The fix is simple: make the directory resolution methods accept a string parameter instead of `CliSettings`.

---

## Types to Extract

### 3.1 `AgentDefinitionLoader`

**Source:** `LlmTornado.Cli/Agents/AgentDefinitionLoader.cs`  
**Target:** `LlmTornado.Cli.Core/Agents/AgentDefinitionLoader.cs`

**The one method that needs changing:**

```csharp
// BEFORE (CLI — reads CliSettings directly)
public static string ResolveAgentsDirectory(CliSettings settings)
{
    if (!string.IsNullOrEmpty(settings.AgentsDirectory) && Directory.Exists(settings.AgentsDirectory))
        return Path.GetFullPath(settings.AgentsDirectory);
    return Path.GetFullPath("agents");
}

// AFTER (Core — accepts a nullable path string)
public static string ResolveAgentsDirectory(string? configuredPath)
{
    if (!string.IsNullOrEmpty(configuredPath) && Directory.Exists(configuredPath))
        return Path.GetFullPath(configuredPath);
    return Path.GetFullPath("agents");
}
```

**Everything else stays the same**. The parsing methods (`ParsePersonaFile`, `ParseFrontmatter`, `ExtractBody`, `DiscoverProjectAgents`, `DiscoverPersonaAgents`) have zero dependency on `CliSettings`.

**Full list of changes:**
| Change | Why |
|--------|-----|
| Namespace → `LlmTornado.Cli.Core.Agents` | Moved to Core |
| `internal static partial` → `public static partial` | Shared access |
| `ResolveAgentsDirectory(CliSettings)` → `ResolveAgentsDirectory(string?)` | Decouple from settings type |
| All `CliAgentDefinition` references → `AgentDefinition` | Renamed in Phase 2 |
| Remove `using LlmTornado.Cli;` import | No longer needed |

**Complete target code:**

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace LlmTornado.Cli.Core.Agents;

/// <summary>
/// Stateless discovery and parsing of agent definitions from both
/// the project directory hierarchy (AGENTS.md files) and the agents
/// directory (persona .md files).
/// </summary>
public static partial class AgentDefinitionLoader
{
    private const int MaxHierarchyDepth = 20;
    private const int MaxFileSize = 100 * 1024;

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex ValidNameRegex();

    [GeneratedRegex(@"--")]
    private static partial Regex ConsecutiveHyphensRegex();

    /// <summary>
    /// Resolve the custom agents directory. Accepts an optional configured path
    /// (from settings), falls back to ./agents/ relative to CWD.
    /// </summary>
    public static string ResolveAgentsDirectory(string? configuredPath)
    {
        if (!string.IsNullOrEmpty(configuredPath) && Directory.Exists(configuredPath))
            return Path.GetFullPath(configuredPath);
        return Path.GetFullPath("agents");
    }

    /// <summary>
    /// Resolve the built-in agents directory relative to the application binary.
    /// </summary>
    public static string ResolveBuiltInDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
    }

    // ... rest of the methods unchanged except CliAgentDefinition → AgentDefinition ...
}
```

**Callers update** (in Phase 6 for CLI, Phase 7 for ACP):
```csharp
// CLI: was
string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings);
// CLI: becomes  
string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings.AgentsDirectory);

// ACP server:
string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(
    Environment.GetEnvironmentVariable("ACP_AGENTS_DIR"));
```

---

### 3.2 `CliSkillLoader` → `SkillLoader`

**Source:** `LlmTornado.Cli/Skills/CliSkillLoader.cs`  
**Target:** `LlmTornado.Cli.Core/Skills/SkillLoader.cs`

**Same pattern — one method to parameterize:**

```csharp
// BEFORE
public static string ResolveSkillsDirectory(CliSettings settings)
{
    if (!string.IsNullOrEmpty(settings.SkillsDirectory) && Directory.Exists(settings.SkillsDirectory))
        return Path.GetFullPath(settings.SkillsDirectory);
    return Path.GetFullPath("skills");
}

// AFTER
public static string ResolveSkillsDirectory(string? configuredPath)
{
    if (!string.IsNullOrEmpty(configuredPath) && Directory.Exists(configuredPath))
        return Path.GetFullPath(configuredPath);
    return Path.GetFullPath("skills");
}
```

**Full list of changes:**
| Change | Why |
|--------|-----|
| Rename class `CliSkillLoader` → `SkillLoader` | Remove Cli prefix |
| Namespace → `LlmTornado.Cli.Core.Skills` | Moved to Core |
| `internal static partial` → `public static partial` | Shared access |
| `ResolveSkillsDirectory(CliSettings)` → `ResolveSkillsDirectory(string?)` | Decouple |
| All `CliSkill` references → `SkillDefinition` | Renamed in Phase 2 |
| Remove `using LlmTornado.Cli;` import | No longer needed |

**All parsing methods stay identical:** `DiscoverSkills`, `ParseSkillMetadata`, `LoadInstructions`, `ParseFrontmatter`, `DiscoverScripts`, `DiscoverReferences`, `DiscoverAssets`, `DetectScriptCommand`.

---

### 3.3 `ScriptToolBuilder`

**Source:** `LlmTornado.Cli/Skills/ScriptToolBuilder.cs`  
**Target:** `LlmTornado.Cli.Core/Skills/ScriptToolBuilder.cs`

**No logic changes needed at all.** This class:
- Takes `List<CliSkill>` (→ `List<SkillDefinition>`) and returns `List<Tool>`
- Uses `System.Diagnostics.Process`, `System.Text.StringBuilder`, and `LlmTornado.Common.Tool`
- Has zero dependency on any CLI type other than `CliSkill`/`SkillScript`

**Changes:**
| Change | Why |
|--------|-----|
| Namespace → `LlmTornado.Cli.Core.Skills` | Moved to Core |
| `internal static` → `public static` | Shared access |
| `List<CliSkill>` → `List<SkillDefinition>` | Renamed in Phase 2 |

---

### 3.4 `McpConfigLoader`

**Source:** `LlmTornado.Cli/Mcp/McpConfigLoader.cs`  
**Target:** `LlmTornado.Cli.Core/Mcp/McpConfigLoader.cs`

**Same pattern — parameterize the path resolution:**

```csharp
// BEFORE
public static string? ResolveMcpConfigPath(CliSettings settings)
{
    string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
    if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        return Path.GetFullPath(envPath);

    if (!string.IsNullOrEmpty(settings.McpConfigPath) && File.Exists(settings.McpConfigPath))
        return Path.GetFullPath(settings.McpConfigPath);

    string defaultPath = Path.GetFullPath("mcp.json");
    return File.Exists(defaultPath) ? defaultPath : null;
}

// AFTER
public static string? ResolveMcpConfigPath(string? configuredPath)
{
    string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
    if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        return Path.GetFullPath(envPath);

    if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
        return Path.GetFullPath(configuredPath);

    string defaultPath = Path.GetFullPath("mcp.json");
    return File.Exists(defaultPath) ? defaultPath : null;
}
```

Similarly for `ResolveDefaultMcpConfigPath`.

**Full list of changes:**
| Change | Why |
|--------|-----|
| Namespace → `LlmTornado.Cli.Core.Mcp` | Moved to Core |
| `internal sealed partial` → `public sealed partial` | Shared access |
| `ResolveMcpConfigPath(CliSettings)` → `ResolveMcpConfigPath(string?)` | Decouple |
| `ResolveDefaultMcpConfigPath(CliSettings)` → `ResolveDefaultMcpConfigPath(string?)` | Decouple |
| Remove `using LlmTornado.Cli;` import | No longer needed |

The core loading logic (`LoadAsync`, `InitializeServer`, `ReloadAsync`, etc.) is untouched — it uses `MCPServer` from `LlmTornado.Mcp` which the Core project already references.

---

## Eliminating Duplicate Code

The ACP server currently has its own `SkillLoader.cs` with a near-identical frontmatter parser. Here's what gets unified:

| ACP Server (deleted in Phase 8) | Core Equivalent |
|--------------------------------|-----------------|
| `SkillLoader.ParseSkillFile()` | `SkillLoader.ParseFrontmatter()` + `AgentDefinitionLoader.ParseFrontmatter()` |
| `SkillLoader.LoadFromDirectory()` | `SkillLoader.DiscoverSkills()` |
| `SkillLoader.LoadFromEmbedded()` | No longer needed (built-in agents are `.md` files, not embedded strings) |
| `BuiltInSkills.Load()` | Agent personas loaded from Core's `Agents/built-in/` content files |
| `AgentSkill` data model | Replaced by `AgentDefinition` (for modes) + `SkillDefinition` (for skills) |

---

## Verification

```powershell
cd src
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
```

All loaders should compile cleanly. They depend only on:
- BCL types (`System.Text`, `System.Text.RegularExpressions`, `System.Diagnostics`, `System.Text.Json`)
- Phase 2 types (`AgentDefinition`, `SkillDefinition`, `SkillScript`)
- `LlmTornado.Common.Tool` (for `ScriptToolBuilder`)
- `LlmTornado.Mcp.MCPServer` (for `McpConfigLoader`)

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Cli.Core/Agents/AgentDefinitionLoader.cs` | Create (from CLI, parameterize) |
| `LlmTornado.Cli.Core/Skills/SkillLoader.cs` | Create (from CliSkillLoader, parameterize) |
| `LlmTornado.Cli.Core/Skills/ScriptToolBuilder.cs` | Create (from CLI, type rename only) |
| `LlmTornado.Cli.Core/Mcp/McpConfigLoader.cs` | Create (from CLI, parameterize) |
