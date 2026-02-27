# Stage 1: Project Scaffolding

## Goal

Create the `LlmTornado.Cli` executable project with proper dependencies and add it to the solution.

---

## Files to Create

### `src/LlmTornado.Cli/LlmTornado.Cli.csproj`

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
        <ProjectReference Include="..\LlmTornado\LlmTornado.csproj" />
        <ProjectReference Include="..\LlmTornado.Agents\LlmTornado.Agents.csproj" />
        <ProjectReference Include="..\LlmTornado.Mcp\LlmTornado.Mcp.csproj" />
    </ItemGroup>

</Project>
```

**Key choices:**
- `net8.0` only (not multi-target) — this is an executable, not a library
- Matches the pattern used by `LlmTornado.Acp.Server` and `LlmTornado.Agents.Samples`
- `LangVersion=preview` enables `required`, `init`, `file-scoped namespaces`, collection expressions, etc.

### Solution file update

Add to `src/LlmTornado.slnx`:

```xml
<Project Path="LlmTornado.Cli\LlmTornado.Cli.csproj" />
```

Place it alongside other executable projects (after `LlmTornado.Agents.Samples` or similar).

---

## Dependencies Explained

| Project Reference | Why |
|---|---|
| `LlmTornado` | Core: `TornadoApi`, `ChatModel`, `ChatMessage`, `ApiAuthentication`, `ProviderAuthentication`, `LLmProviders`, `Tool`, `FunctionResult` |
| `LlmTornado.Agents` | Runtime: `ChatRuntime`, `SingletonRuntimeConfiguration`, `TornadoAgent`, `PersistentConversation`, `TornadoRunner`, `AgentRunnerEvents` |
| `LlmTornado.Mcp` | MCP: `MCPServer`, `MCPToolkits`, tool conversion via `McpClientTool.ToTornadoTool()` |

**Not referenced** (and why):
- `LlmTornado.Acp` — ACP protocol is specific to IDE integration; the CLI uses ChatRuntime directly
- `LlmTornado.Acp.Server` — Skills system will be reimplemented following the open Agent Skills standard, not the ACP-specific one

---

## Namespace Conventions

All files use the `LlmTornado.Cli` root namespace with folder-based sub-namespaces:

```
LlmTornado.Cli                    # Program.cs, top-level types
LlmTornado.Cli.Commands           # Commands/*.cs
LlmTornado.Cli.Skills             # Skills/*.cs
LlmTornado.Cli.Mcp                # Mcp/*.cs
LlmTornado.Cli.Memory             # Memory/*.cs
```

---

## Minimal `Program.cs` Placeholder

```csharp
namespace LlmTornado.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("LlmTornado CLI Agent");
        Console.WriteLine("===================");
        Console.WriteLine("Initializing...");
        
        // TODO: Stage 2 - Provider detection
        // TODO: Stage 3 - Storage initialization
        // TODO: Stage 4 - Skill loading
        // TODO: Stage 5 - MCP configuration
        // TODO: Stage 6 - Tool approval manager
        // TODO: Stage 7 - Conversation memory
        // TODO: Stage 8 - Agent builder
        // TODO: Stage 10 - REPL loop
        
        return 0;
    }
}
```

---

## Verification

```powershell
cd src
dotnet build LlmTornado.Cli/LlmTornado.Cli.csproj
dotnet run --project LlmTornado.Cli
```

Expected: prints banner message and exits cleanly.

---

## Reference: Existing Executable Projects

Pattern taken from:
- `src/LlmTornado.Acp.Server/LlmTornado.Acp.Server.csproj` — `<OutputType>Exe</OutputType>`, `net8.0`, references `LlmTornado` + `LlmTornado.Agents` + `LlmTornado.Acp`
- `src/LlmTornado.Agents.Samples/LlmTornado.Agents.Samples.csproj` — `<OutputType>Exe</OutputType>`, `net8.0`, references `LlmTornado` + `LlmTornado.Agents` + `LlmTornado.Mcp`
