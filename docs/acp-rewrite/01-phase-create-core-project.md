# Phase 1 — Create `LlmTornado.Cli.Core` Project

## Objective

Create a new class library project that will house all the shared agent infrastructure extracted from the CLI. Both `LlmTornado.Cli` (console app) and `LlmTornado.Acp.Server` (JSON-RPC server) will reference this project.

## Why a Separate Library?

The CLI types (`CliAgentDefinition`, `AgentDefinitionLoader`, `CliSkillManager`, etc.) are currently `internal` to the CLI project. The ACP server has its own parallel implementations (`AgentSkill`, `SkillLoader`, `BuiltInSkills`) that duplicate ~60-80% of the same logic. Extracting to a shared library:

- Eliminates code duplication
- Ensures both CLI and ACP server use identical agent/skill behavior
- Makes the built-in persona `.md` files a single source of truth
- Allows the ACP server to benefit from all CLI improvements automatically

## Steps

### 1.1 Create the project directory

```
src/LlmTornado.Cli.Core/
```

### 1.2 Create the `.csproj` file

Create `src/LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworks>net10.0;net8.0</TargetFrameworks>
    <LangVersion>preview</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>LlmTornado.Cli.Core</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LlmTornado\LlmTornado.csproj" />
    <ProjectReference Include="..\LlmTornado.Agents\LlmTornado.Agents.csproj" />
    <ProjectReference Include="..\LlmTornado.Mcp\LlmTornado.Mcp.csproj" />
  </ItemGroup>

  <!-- Shared agent persona files -->
  <ItemGroup>
    <Content Include="Agents\built-in\*.md">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

**Key choices:**
- **Multi-target `net10.0;net8.0`** — matches `LlmTornado.Acp.csproj` pattern (ACP lib is multi-target)
- **References `LlmTornado.Mcp`** — needed for `McpConfigLoader`/`MCPServer` integration
- **Content includes for persona `.md` files** — these are the built-in agents shipped with the library

### 1.3 Create the folder structure

```
src/LlmTornado.Cli.Core/
├── Agents/
│   ├── built-in/          ← persona .md files copied here
│   │   ├── default.md
│   │   ├── architect.md
│   │   ├── code-reviewer.md
│   │   ├── debugger.md
│   │   └── docs-writer.md
│   ├── AgentDefinition.cs       ← renamed from CliAgentDefinition
│   ├── AgentDefinitionLoader.cs
│   └── AgentDefinitionManager.cs
├── Mcp/
│   ├── McpConfigLoader.cs
│   └── McpConfigModel.cs
├── Skills/
│   ├── SkillDefinition.cs       ← renamed from CliSkill
│   ├── SkillLoader.cs           ← renamed from CliSkillLoader
│   ├── SkillManager.cs          ← renamed from CliSkillManager
│   └── ScriptToolBuilder.cs
├── AgentBuilder.cs              ← extracted from CliAgentBuilder
├── AgentSettings.cs             ← renamed from CliSettings
├── ISettingsPersistence.cs
├── IToolApproval.cs
├── ProviderDetector.cs
└── ToolOptimizer.cs
```

### 1.4 Copy built-in persona files

Copy the 5 `.md` files from `src/LlmTornado.Cli/Agents/built-in/` to `src/LlmTornado.Cli.Core/Agents/built-in/`:

- `default.md`
- `architect.md`
- `code-reviewer.md`
- `debugger.md`
- `docs-writer.md`

These are identical — the Core project becomes the canonical source.

### 1.5 Add to the solution file

Edit `src/LlmTornado.slnx` to include the new project. Place it logically near the CLI and ACP projects:

```xml
<!-- Add within the solution, near the existing CLI/ACP entries -->
<Project Path="LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj" />
```

### 1.6 Create stub persistence interface

Create `src/LlmTornado.Cli.Core/ISettingsPersistence.cs` as a placeholder (fully implemented in Phase 2):

```csharp
namespace LlmTornado.Cli.Core;

/// <summary>
/// Abstraction for persisting agent settings.
/// CLI implements with disk I/O; ACP server implements with in-memory no-op.
/// </summary>
public interface ISettingsPersistence
{
    /// <summary>
    /// Persist the current settings state.
    /// </summary>
    void SaveSettings(AgentSettings settings);
}
```

### 1.7 Create stub tool approval interface

Create `src/LlmTornado.Cli.Core/IToolApproval.cs`:

```csharp
namespace LlmTornado.Cli.Core;

/// <summary>
/// Abstraction for tool approval. CLI prompts interactively; 
/// ACP server auto-approves (IDE handles its own UX).
/// </summary>
public interface IToolApproval
{
    /// <summary>
    /// Pre-approve a list of tool names (skip approval prompts).
    /// </summary>
    void PreApproveTools(IEnumerable<string> toolNames);

    /// <summary>
    /// Check if a tool should be auto-approved.
    /// </summary>
    bool IsAutoApproved(string toolName);

    /// <summary>
    /// The delegate to wire as the runtime's tool permission handler.
    /// </summary>
    ValueTask<bool> HandleToolPermissionRequest(string requestMessage);
}
```

## Verification

After this phase, verify the project compiles:

```powershell
cd src
dotnet build LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj
```

At this point it should compile cleanly with just the interfaces and empty folders. The actual types are moved in subsequent phases.

## What This Phase Does NOT Do

- Does not move any existing code yet (that's Phase 2-5)
- Does not modify the CLI project (that's Phase 6)
- Does not modify the ACP server (that's Phase 7)
- The Core project initially compiles with just the interfaces and persona files

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj` | Create |
| `LlmTornado.Cli.Core/ISettingsPersistence.cs` | Create |
| `LlmTornado.Cli.Core/IToolApproval.cs` | Create |
| `LlmTornado.Cli.Core/Agents/built-in/*.md` | Copy from CLI |
| `LlmTornado.slnx` | Add project reference |
