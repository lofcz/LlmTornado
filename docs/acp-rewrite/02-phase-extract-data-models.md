# Phase 2 — Extract Data Models to Core

## Objective

Move the pure data model types from the CLI into the Core library. These types have **zero dependencies** on CLI-specific concerns (no `CliStorage`, no `ConsoleRenderer`, no I/O) — they're simple POCOs that can be made `public` and shared.

## Types to Extract

### 2.1 `CliAgentDefinition` → `AgentDefinition`

**Source:** `LlmTornado.Cli/Agents/CliAgentDefinition.cs`  
**Target:** `LlmTornado.Cli.Core/Agents/AgentDefinition.cs`

**Changes required:**
- Rename class from `CliAgentDefinition` to `AgentDefinition`
- Change access from `internal sealed` to `public sealed`
- Change `AgentSource` enum from `internal` to `public`
- Change namespace from `LlmTornado.Cli.Agents` to `LlmTornado.Cli.Core.Agents`

**Current code (CLI):**
```csharp
namespace LlmTornado.Cli.Agents;

internal enum AgentSource
{
    BuiltIn,
    Custom,
    Project
}

internal sealed class CliAgentDefinition
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required AgentSource Source { get; init; }
    public required string FilePath { get; init; }
    public string Instructions { get; init; } = "";
    public List<string> EnabledSkills { get; init; } = [];
    public List<string> DisabledSkills { get; init; } = [];
    public List<string> EnabledTools { get; init; } = [];
    public List<string> DisabledTools { get; init; } = [];
    public List<string> AutoApproveTools { get; init; } = [];
    
    public bool HasCapabilityCuration => /* ... */;
    public bool IsPersona => Source is AgentSource.BuiltIn or AgentSource.Custom;
}
```

**Target code (Core):**
```csharp
namespace LlmTornado.Cli.Core.Agents;

/// <summary>
/// Source origin of an agent definition.
/// </summary>
public enum AgentSource
{
    BuiltIn,
    Custom,
    Project
}

/// <summary>
/// An agent definition representing either a selectable persona (with capability curation)
/// or auto-detected project context (instructions only).
/// </summary>
public sealed class AgentDefinition
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required AgentSource Source { get; init; }
    public required string FilePath { get; init; }
    public string Instructions { get; init; } = "";
    public List<string> EnabledSkills { get; init; } = [];
    public List<string> DisabledSkills { get; init; } = [];
    public List<string> EnabledTools { get; init; } = [];
    public List<string> DisabledTools { get; init; } = [];
    public List<string> AutoApproveTools { get; init; } = [];

    public bool HasCapabilityCuration =>
        EnabledSkills.Count > 0 ||
        DisabledSkills.Count > 0 ||
        EnabledTools.Count > 0 ||
        DisabledTools.Count > 0 ||
        AutoApproveTools.Count > 0;

    public bool IsPersona => Source is AgentSource.BuiltIn or AgentSource.Custom;
}
```

**Why this is safe:** Zero imports, zero dependencies. Pure POCO with computed properties only.

---

### 2.2 `CliSkill` → `SkillDefinition`

**Source:** `LlmTornado.Cli/Skills/CliSkill.cs`  
**Target:** `LlmTornado.Cli.Core/Skills/SkillDefinition.cs`

**Changes required:**
- Rename `CliSkill` to `SkillDefinition`, `SkillScript` stays the same name
- `internal sealed` → `public sealed`
- Namespace: `LlmTornado.Cli.Skills` → `LlmTornado.Cli.Core.Skills`

**Target code (Core):**
```csharp
namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Represents a loaded skill following the Agent Skills standard (agentskills.io).
/// </summary>
public sealed class SkillDefinition
{
    // --- Frontmatter (always loaded) ---
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? License { get; init; }
    public string? Compatibility { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public List<string> AllowedTools { get; init; } = [];

    // --- Paths ---
    public required string DirectoryPath { get; init; }
    public required string SkillMdPath { get; init; }

    // --- Body (loaded on demand via progressive disclosure) ---
    public string? Instructions { get; set; }

    // --- Discovered Resources ---
    public List<SkillScript> Scripts { get; init; } = [];
    public List<string> References { get; init; } = [];
    public List<string> Assets { get; init; } = [];

    // --- Runtime State ---
    public bool Enabled { get; set; } = true;
    public bool Activated { get; set; }
}

/// <summary>
/// A script file found in a skill's scripts/ directory.
/// </summary>
public sealed class SkillScript
{
    public required string FileName { get; init; }
    public required string AbsolutePath { get; init; }
    public required string Extension { get; init; }
    public required string Command { get; init; }
}
```

**Why this is safe:** Zero imports, zero dependencies. Pure data model.

---

### 2.3 `CliSettings` → `AgentSettings`

**Source:** `LlmTornado.Cli/CliSettings.cs`  
**Target:** `LlmTornado.Cli.Core/AgentSettings.cs`

**Changes required:**
- Rename `CliSettings` to `AgentSettings`
- `internal sealed` → `public sealed`
- Namespace: `LlmTornado.Cli` → `LlmTornado.Cli.Core`
- Keep all JSON property name attributes

**Target code (Core):**
```csharp
using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core;

/// <summary>
/// Serializable settings shared between CLI and ACP server.
/// </summary>
public sealed class AgentSettings
{
    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("disabled_skills")]
    public HashSet<string> DisabledSkills { get; set; } = [];

    [JsonPropertyName("skills_directory")]
    public string? SkillsDirectory { get; set; }

    [JsonPropertyName("mcp_config_path")]
    public string? McpConfigPath { get; set; }

    [JsonPropertyName("max_turns_before_summary")]
    public int MaxTurnsBeforeSummary { get; set; }

    [JsonPropertyName("active_agent")]
    public string? ActiveAgent { get; set; }

    [JsonPropertyName("agents_directory")]
    public string? AgentsDirectory { get; set; }

    [JsonPropertyName("project_agents_enabled")]
    public bool ProjectAgentsEnabled { get; set; } = true;

    [JsonPropertyName("max_tools")]
    public int MaxTools { get; set; } = 25;

    [JsonPropertyName("tool_optimizer_enabled")]
    public bool ToolOptimizerEnabled { get; set; } = true;
}
```

**Why this is safe:** Only dependency is `System.Text.Json.Serialization` (BCL). Pure serializable data bag.

---

### 2.4 `ProviderDetector` + DTOs

**Source:** `LlmTornado.Cli/ProviderDetector.cs`  
**Target:** `LlmTornado.Cli.Core/ProviderDetector.cs`

**Changes required:**
- `internal sealed` → `public sealed` for `DetectedProvider`, `ProviderDetectionResult`
- `internal static` → `public static` for `ProviderDetector`
- Namespace: `LlmTornado.Cli` → `LlmTornado.Cli.Core`

**This type depends on `LlmTornado` types** (`TornadoApi`, `ChatModel`, `LLmProviders`, `ProviderAuthentication`) — all of which are already referenced by the Core project.

**No code changes needed** beyond access modifiers and namespace. The entire file is pure logic reading environment variables and constructing API objects.

---

### 2.5 `ToolOptimizer` + `ToolOptimizationResult`

**Source:** `LlmTornado.Cli/ToolOptimizer.cs`  
**Target:** `LlmTornado.Cli.Core/ToolOptimizer.cs`

**Changes required:**
- `internal sealed` → `public sealed`
- Namespace: `LlmTornado.Cli` → `LlmTornado.Cli.Core`

**Dependencies:** `TornadoApi`, `ChatModel`, `Tool`, `ChatRequest`, `Conversation` — all from `LlmTornado` (already referenced). No CLI-specific deps.

---

### 2.6 `McpConfigModel` DTOs

**Source:** `LlmTornado.Cli/Mcp/McpConfigModel.cs`  
**Target:** `LlmTornado.Cli.Core/Mcp/McpConfigModel.cs`

**Changes required:**
- `internal sealed` → `public sealed` for `McpConfig`, `McpServerEntry`, `McpServerStatus`
- Namespace: `LlmTornado.Cli.Mcp` → `LlmTornado.Cli.Core.Mcp`

**Dependencies:** Only `System.Text.Json.Serialization` (BCL). Pure DTOs.

---

### 2.7 Finalize `ISettingsPersistence`

Update the stub from Phase 1 with the finalized signature:

```csharp
namespace LlmTornado.Cli.Core;

/// <summary>
/// Abstraction for persisting agent settings.
/// CLI implements with disk I/O via CliStorage.
/// ACP server implements with in-memory no-op.
/// </summary>
public interface ISettingsPersistence
{
    /// <summary>
    /// Persist the current settings state.
    /// </summary>
    void SaveSettings(AgentSettings settings);
}
```

---

## Naming Convention: Why Rename?

| CLI Name | Core Name | Reason |
|----------|-----------|--------|
| `CliAgentDefinition` | `AgentDefinition` | Remove `Cli` prefix — it's no longer CLI-specific |
| `CliSkill` | `SkillDefinition` | Parallel with `AgentDefinition`, clearer name |
| `CliSettings` | `AgentSettings` | No longer CLI-specific, describes agent configuration |
| `CliSkillLoader` | `SkillLoader` | Remove `Cli` prefix |
| `CliSkillManager` | `SkillManager` | Remove `Cli` prefix |

The `Cli` prefix was appropriate when these were internal to the CLI project. In a shared library, they need domain-oriented names.

## Verification

```powershell
cd src
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
```

All types should compile cleanly since they have no cross-dependencies beyond what the Core project already references.

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Cli.Core/Agents/AgentDefinition.cs` | Create (from CliAgentDefinition.cs) |
| `LlmTornado.Cli.Core/Skills/SkillDefinition.cs` | Create (from CliSkill.cs) |
| `LlmTornado.Cli.Core/AgentSettings.cs` | Create (from CliSettings.cs) |
| `LlmTornado.Cli.Core/ProviderDetector.cs` | Create (from ProviderDetector.cs) |
| `LlmTornado.Cli.Core/ToolOptimizer.cs` | Create (from ToolOptimizer.cs) |
| `LlmTornado.Cli.Core/Mcp/McpConfigModel.cs` | Create (from McpConfigModel.cs) |
| `LlmTornado.Cli.Core/ISettingsPersistence.cs` | Update (finalize) |
