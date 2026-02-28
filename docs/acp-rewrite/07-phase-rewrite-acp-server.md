# Phase 7 — Rewrite ACP Server

## Objective

Completely rewrite `LlmTornado.Acp.Server` to use the Core library's agent infrastructure. Each CLI agent persona becomes an ACP mode. The server gains multi-provider support, skills with script tools, MCP integration, and capability curation — all powered by the same code as the CLI.

This is the largest phase and the whole point of the rewrite.

---

## 7.1 Update `.csproj`

**`LlmTornado.Acp.Server/LlmTornado.Acp.Server.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
    <AssemblyName>LlmTornado.Acp.Server</AssemblyName>
    <RootNamespace>LlmTornado.Acp.Server</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LlmTornado.Acp\LlmTornado.Acp.csproj" />
    <ProjectReference Include="..\LlmTornado.Agents\LlmTornado.Agents.csproj" />
    <ProjectReference Include="..\LlmTornado\LlmTornado.csproj" />
    <!-- NEW: shared agent infrastructure + MCP -->
    <ProjectReference Include="..\LlmTornado.Cli.Core\LlmTornado.Cli.Core.csproj" />
    <ProjectReference Include="..\LlmTornado.Mcp\LlmTornado.Mcp.csproj" />
  </ItemGroup>

  <!-- REMOVED: Skills\*.skill.md Content items (no longer needed) -->

</Project>
```

**Changes:**
- Added `LlmTornado.Cli.Core` and `LlmTornado.Mcp` references
- Removed the `<Content Include="Skills\*.skill.md">` — replaced by Core's built-in persona files

---

## 7.2 Create Adapter Implementations

### `NoOpSettingsPersistence.cs`

```csharp
using LlmTornado.Cli.Core;

namespace LlmTornado.Acp.Server;

/// <summary>
/// ACP server does not persist settings — sessions are stateless across restarts.
/// </summary>
internal sealed class NoOpSettingsPersistence : ISettingsPersistence
{
    public void SaveSettings(AgentSettings settings) { }
}
```

### `AutoApproveToolApproval.cs`

```csharp
using LlmTornado.Cli.Core;

namespace LlmTornado.Acp.Server;

/// <summary>
/// ACP server auto-approves all tool calls.
/// The IDE client (JetBrains Rider, etc.) handles its own tool approval UX.
/// </summary>
internal sealed class AutoApproveToolApproval : IToolApproval
{
    public void PreApproveTools(IEnumerable<string> toolNames) { }
    
    public bool IsAutoApproved(string toolName) => true;
    
    public ValueTask<bool> HandleToolPermissionRequest(string requestMessage)
        => ValueTask.FromResult(true);
}
```

---

## 7.3 Rewrite `Program.cs`

The entry point changes from OpenAI-only to multi-provider:

```csharp
using LlmTornado.Acp;
using LlmTornado.Acp.Server;
using LlmTornado.Cli.Core;

Console.Error.WriteLine("[ACP] LlmTornado ACP Server starting...");

// Step 1: Detect providers from environment variables (same as CLI)
ProviderDetectionResult? providerResult = ProviderDetector.Detect();

if (providerResult is null)
{
    Console.Error.WriteLine("[ACP] ERROR: No LLM provider API keys found.");
    Console.Error.WriteLine("[ACP] Set one or more environment variables:");
    Console.Error.WriteLine("[ACP]   OPENAI_API_KEY, ANTHROPIC_API_KEY, GOOGLE_API_KEY, etc.");
    return 1;
}

Console.Error.WriteLine($"[ACP] Detected {providerResult.Providers.Count} provider(s): " +
    string.Join(", ", providerResult.Providers.Select(p => p.Provider)));
Console.Error.WriteLine($"[ACP] Active model: {providerResult.ActiveModel}");

// Step 2: Read optional configuration from environment
string? skillsDir = Environment.GetEnvironmentVariable("ACP_SKILLS_DIR");
string? agentsDir = Environment.GetEnvironmentVariable("ACP_AGENTS_DIR");
string? mcpConfigPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");

if (!string.IsNullOrWhiteSpace(skillsDir))
    Console.Error.WriteLine($"[ACP] External skills directory: {skillsDir}");
if (!string.IsNullOrWhiteSpace(agentsDir))
    Console.Error.WriteLine($"[ACP] External agents directory: {agentsDir}");

Console.Error.WriteLine("[ACP] Listening on stdin/stdout...");

// Step 3: Create runtime and start server
TornadoAcpRuntime runtime = new(providerResult, skillsDir, agentsDir, mcpConfigPath);
AcpJsonRpcServer server = new(runtime, Console.OpenStandardInput(), Console.OpenStandardOutput());

await server.RunAsync();

return 0;
```

**Key differences from the old `Program.cs`:**
1. Uses `ProviderDetector.Detect()` instead of reading `OPENAI_API_KEY` directly
2. Accepts `ACP_AGENTS_DIR` for custom agent personas
3. Accepts `TORNADO_MCP_CONFIG` for MCP server configuration
4. No fallback to `apiKey.json` — relies on standard env vars (consistent with CLI)
5. Reports all detected providers, not just OpenAI

---

## 7.4 Rewrite `TornadoAcpRuntime.cs`

This is the core of the rewrite. The new runtime uses Core's `AgentBuilder`, `AgentDefinitionManager`, `SkillManager`, and `McpConfigLoader`.

```csharp
using LlmTornado.Acp;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Common;

namespace LlmTornado.Acp.Server;

public class TornadoAcpRuntime : BaseAcpTornadoRuntimeConfiguration
{
    private readonly ProviderDetectionResult _providerResult;
    private readonly AgentSettings _settings;
    private readonly ISettingsPersistence _persistence;
    private readonly IToolApproval _toolApproval;
    private readonly SkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly AgentDefinitionManager _agentManager;

    public TornadoAcpRuntime(
        ProviderDetectionResult providerResult,
        string? skillsDir = null,
        string? agentsDir = null,
        string? mcpConfigPath = null)
        : base("LlmTornado", "1.0.0")
    {
        _providerResult = providerResult;
        _settings = new AgentSettings();
        _persistence = new NoOpSettingsPersistence();
        _toolApproval = new AutoApproveToolApproval();

        // Initialize skill manager
        _skillManager = new SkillManager(_settings, _persistence);
        string resolvedSkillsDir = SkillLoader.ResolveSkillsDirectory(skillsDir);
        _skillManager.LoadSkills(resolvedSkillsDir);

        Console.Error.WriteLine($"[ACP] Loaded {_skillManager.GetAllSkills().Count} skill(s)");

        // Initialize MCP loader
        _mcpLoader = new McpConfigLoader();
        string? resolvedMcpPath = McpConfigLoader.ResolveMcpConfigPath(mcpConfigPath);
        if (resolvedMcpPath is not null)
        {
            _mcpLoader.LoadAsync(resolvedMcpPath, msg => Console.Error.WriteLine($"[ACP] MCP: {msg}"))
                .GetAwaiter().GetResult();
            Console.Error.WriteLine($"[ACP] Loaded {_mcpLoader.AllTools.Count} MCP tool(s)");
        }

        // Initialize agent definition manager
        _agentManager = new AgentDefinitionManager(_settings, _persistence);
        string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string customDir = AgentDefinitionLoader.ResolveAgentsDirectory(agentsDir);
        _agentManager.LoadAll(builtInDir, customDir, Directory.GetCurrentDirectory());

        List<AgentDefinition> personas = _agentManager.GetAllPersonas();
        Console.Error.WriteLine($"[ACP] Loaded {personas.Count} agent persona(s): " +
            string.Join(", ", personas.Select(p => p.Name)));
    }
}
```

### `CreateRuntimeConfiguration` — Building an Agent per Session

```csharp
protected override IRuntimeConfiguration CreateRuntimeConfiguration(
    AcpNewSessionRequest request, string modeId, string modelId)
{
    // Set the active persona to match the requested mode
    _agentManager.SetActivePersona(modeId);
    _agentManager.ApplyCapabilityBaseline(_skillManager, _toolApproval);

    // Resolve the model
    ChatModel resolvedModel = ResolveModel(modelId);

    // Build filesystem tools scoped to the working directory
    List<Tool> filesystemTools = BuildAcpLocalTools(request.Cwd);

    // Use the shared AgentBuilder to assemble the agent
    AgentBuilder builder = new(
        api: _providerResult.Api,
        activeModel: resolvedModel,
        skillManager: _skillManager,
        mcpLoader: _mcpLoader,
        toolApproval: _toolApproval,
        agentManager: _agentManager,
        settings: _settings,
        optimizerModel: _providerResult.OptimizerModel,
        additionalTools: filesystemTools
    );

    // Build returns a ChatRuntime wrapping a SingletonRuntimeConfiguration
    builder.Build();

    // Return the underlying runtime configuration
    return builder.Runtime.RuntimeConfiguration;
}
```

**What this does:**
1. Sets the active persona to the requested ACP mode (e.g., "architect")
2. Applies capability curation (skill whitelist/blacklist, tool filtering)
3. Resolves the chat model from detected providers (not hardcoded OpenAI)
4. Builds filesystem tools scoped to the working directory
5. Uses `AgentBuilder` to assemble everything — system prompt, tools, agent, runtime

### Initial Mode and Model

```csharp
protected override string GetInitialMode(AcpNewSessionRequest request) => "default";

protected override string GetInitialModel(AcpNewSessionRequest request)
{
    return _providerResult.ActiveModel.Name;
}
```

### Model Resolution — Multi-Provider

```csharp
private ChatModel ResolveModel(string modelId)
{
    // Search all detected providers for a matching model
    foreach (DetectedProvider provider in _providerResult.Providers)
    {
        ChatModel? match = provider.Models.FirstOrDefault(m => m.Name == modelId);
        if (match is not null)
            return match;
    }

    // Default to the active model
    return _providerResult.ActiveModel;
}
```

**No more hardcoded OpenAI model switch statement.** The model list comes from whatever providers are detected.

### Building Available Modes from Agent Personas

```csharp
private List<AcpSessionMode> BuildAvailableModes()
{
    List<AgentDefinition> personas = _agentManager.GetAllPersonas();

    // Stable ordering: "default" first, then alphabetical
    List<AcpSessionMode> modes = [];

    // Put "default" first if it exists
    AgentDefinition? defaultPersona = personas.FirstOrDefault(p => p.Name == "default");
    if (defaultPersona is not null)
    {
        modes.Add(new AcpSessionMode
        {
            Id = defaultPersona.Name,
            Name = FormatDisplayName(defaultPersona.Name),
            Description = defaultPersona.Description
        });
    }

    // Add remaining personas alphabetically
    foreach (AgentDefinition persona in personas.OrderBy(p => p.Name))
    {
        if (persona.Name == "default") continue;

        modes.Add(new AcpSessionMode
        {
            Id = persona.Name,
            Name = FormatDisplayName(persona.Name),
            Description = persona.Description
        });
    }

    return modes;
}

/// <summary>
/// "code-reviewer" → "Code Reviewer", "debugger" → "Debugger"
/// </summary>
private static string FormatDisplayName(string slug)
{
    return string.Join(' ', slug.Split('-')
        .Select(word => char.ToUpper(word[0]) + word[1..]));
}
```

**The modes now come directly from agent personas** — whatever `.md` files exist in the built-in and custom directories become ACP modes. No hardcoded list.

### Building Config Options (Model Selector)

```csharp
private List<AcpSessionConfigOption> BuildConfigOptions(AcpSessionContext ctx)
{
    // Group models by provider
    List<AcpSessionConfigSelectGroup> groups = [];

    foreach (DetectedProvider provider in _providerResult.Providers)
    {
        groups.Add(new AcpSessionConfigSelectGroup
        {
            Group = provider.Provider.ToString().ToLowerInvariant(),
            Name = provider.Provider.ToString(),
            Options = provider.Models.ConvertAll(m => new AcpSessionConfigSelectOption
            {
                Value = m.Name,
                Name = m.Name,
                Description = $"{provider.Provider} model"
            })
        });
    }

    return
    [
        new AcpSessionConfigOption
        {
            Id = "model",
            Name = "Model",
            Description = "The LLM model to use for completions",
            Type = "select",
            Category = "model",
            CurrentValue = ctx.CurrentModelId,
            Options = groups
        }
    ];
}
```

**Models are now grouped by provider** instead of a flat OpenAI-only list. If both Anthropic and OpenAI are detected, the IDE shows both sets.

### NewSessionAsync Override

```csharp
public override Task<AcpNewSessionResponse> NewSessionAsync(
    AcpNewSessionRequest request, CancellationToken cancellationToken)
{
    Task<AcpNewSessionResponse> responseTask = base.NewSessionAsync(request, cancellationToken);
    AcpNewSessionResponse response = responseTask.Result;

    response.Modes = new AcpSessionModeState
    {
        CurrentModeId = "default",
        AvailableModes = BuildAvailableModes()
    };

    response.ConfigOptions = BuildConfigOptions(GetSessionContext(response.SessionId)!);

    Console.Error.WriteLine($"[ACP] New session: {response.SessionId} " +
        $"(cwd: {request.Cwd}, model: {GetInitialModel(request)}, " +
        $"modes: {response.Modes.AvailableModes.Count})");

    return Task.FromResult(response);
}
```

### SetModeAsync Override

```csharp
public override Task<AcpSetSessionModeResponse> SetModeAsync(
    AcpSetSessionModeRequest request, CancellationToken cancellationToken)
{
    // Validate the mode exists as a persona
    if (_agentManager.GetPersona(request.ModeId) is null)
    {
        Console.Error.WriteLine($"[ACP] Unknown mode: {request.ModeId}");
        return Task.FromResult(new AcpSetSessionModeResponse());
    }

    Console.Error.WriteLine($"[ACP] Session {request.SessionId} mode → {request.ModeId}");
    return base.SetModeAsync(request, cancellationToken);
}
```

### SetConfigOptionAsync Override

```csharp
public override Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(
    AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken)
{
    AcpSessionContext? ctx = GetSessionContext(request.SessionId);
    if (ctx is null)
        return Task.FromResult(new AcpSetSessionConfigOptionResponse());

    if (request.ConfigId == "model")
    {
        // Validate the model exists in detected providers
        ChatModel? resolved = null;
        foreach (DetectedProvider provider in _providerResult.Providers)
        {
            resolved = provider.Models.FirstOrDefault(m => m.Name == request.Value);
            if (resolved is not null) break;
        }

        if (resolved is not null)
        {
            RebuildSessionRuntime(ctx, ctx.CurrentModeId, request.Value);
            Console.Error.WriteLine($"[ACP] Session {request.SessionId} model → {request.Value}");
        }
    }

    return Task.FromResult(new AcpSetSessionConfigOptionResponse
    {
        ConfigOptions = BuildConfigOptions(ctx)
    });
}
```

### Capabilities

```csharp
protected override AcpAgentCapabilities DescribeCapabilities()
{
    return new AcpAgentCapabilities
    {
        LoadSession = false,
        SessionCapabilities = new AcpSessionCapabilities
        {
            SetMode = true,
            SetConfigOption = true
        },
        PromptCapabilities = new AcpPromptCapabilities
        {
            Image = false,
            Audio = false,
            EmbeddedContext = true
        }
    };
}
```

### Filesystem Tools (Kept from Old Server)

The filesystem tools stay in the ACP server — they're IDE-specific and scoped to the working directory. The implementation is identical to the current `TornadoAcpRuntime`:

```csharp
internal static List<Tool> BuildAcpLocalTools(string cwd)
{
    string acpRoot = ResolveWorkspaceRoot(cwd);

    return
    [
        new Tool(
            (string relativePath) => ListDirectory(acpRoot, relativePath),
            "list_dir",
            "Lists files and folders under the workspace directory."),
        new Tool(
            (string query, string includePattern, int maxResults) => 
                SearchFiles(acpRoot, query, includePattern, maxResults),
            "search_files",
            "Searches for text in files under the workspace directory."),
        new Tool(
            (string relativePath, int startLine, int endLine) => 
                ReadFileRange(acpRoot, relativePath, startLine, endLine),
            "read_file",
            "Reads a range of lines from a file in the workspace."),
        new Tool(
            (string relativePath, string content) => 
                WriteFile(acpRoot, relativePath, content),
            "write_file",
            "Writes full file content to a file in the workspace."),
        new Tool(
            (string relativePath, string oldText, string newText) => 
                ReplaceInFile(acpRoot, relativePath, oldText, newText),
            "replace_in_file",
            "Replaces exact text in a file in the workspace.")
    ];
}

/// <summary>
/// Resolve the workspace root path from the CWD reported by the IDE.
/// </summary>
internal static string ResolveWorkspaceRoot(string cwd)
{
    // Use the CWD directly — the IDE reports the project root
    return Path.GetFullPath(cwd);
}
```

**Note:** `ResolveAcpRootPath` is renamed to `ResolveWorkspaceRoot` and simplified. The old logic tried to find `src/LlmTornado.Acp` — that was the server's own source code! The new version just uses the CWD as the workspace root, which is what the IDE reports.

The `ListDirectory`, `SearchFiles`, `ReadFileRange`, `WriteFile`, `ReplaceInFile` methods are unchanged — copy them from the old server.

---

## 7.5 Delete `SkillRuntimeConfiguration.cs`

The custom `IRuntimeConfiguration` is no longer needed. The `AgentBuilder` creates a `SingletonRuntimeConfiguration` via the standard path. The ACP server's `CreateRuntimeConfiguration` returns `builder.Runtime.RuntimeConfiguration` directly.

---

## End-to-End Example: What Happens When...

### ...a user opens a new ACP session in JetBrains Rider

1. IDE sends `initialize` → server returns capabilities (modes supported, config options supported)
2. IDE sends `newSession` with `cwd: "/path/to/project"`
3. Server creates:
   - `AgentBuilder` with `default` persona, detected models, filesystem tools, skills, MCP tools
   - Returns session ID + 5 modes (default, architect, code-reviewer, debugger, docs-writer) + model list grouped by provider
4. IDE displays mode selector with all 5 options

### ...the user switches to "architect" mode

1. IDE sends `setMode(modeId: "architect")`
2. Server calls `base.SetModeAsync()` → `RebuildSessionRuntime()` → `CreateRuntimeConfiguration("architect", modelId)`
3. Inside `CreateRuntimeConfiguration`:
   - `_agentManager.SetActivePersona("architect")` — selects the architect persona
   - `ApplyCapabilityBaseline()` — enables only `file-analyzer` and `web-search` skills (per the persona's `enabled-skills`), pre-approves `file-analyzer:tree-summary` and `file-analyzer:line-count`
   - `AgentBuilder.Build()` — creates agent with architect system prompt, filtered tools
4. Conversation history is replayed to the new runtime
5. IDE shows "Architect" as active mode

### ...the user sends a prompt

1. IDE sends `prompt` with content blocks
2. Base class converts to `ChatMessage`, calls `AddToChatAsync`
3. Agent processes with streaming — each chunk fires `OnRuntimeEvent` → `HandleRuntimeEvent` → `OnSessionUpdate` → IDE displays streaming text
4. Filesystem tools can be called (list_dir, read_file, etc.)
5. Skill script tools can be called (if skills are discovered)
6. MCP tools can be called (if mcp.json is configured)

### ...the user switches models

1. IDE sends `setConfigOption(configId: "model", value: "claude-4.6-opus")`
2. Server validates the model exists in detected providers
3. `RebuildSessionRuntime()` rebuilds with the new model
4. Response includes updated config options with new current value

---

## File Checklist

| File | Action |
|------|--------|
| `LlmTornado.Acp.Server/LlmTornado.Acp.Server.csproj` | Update (add Core + MCP refs) |
| `LlmTornado.Acp.Server/Program.cs` | Rewrite |
| `LlmTornado.Acp.Server/TornadoAcpRuntime.cs` | Rewrite |
| `LlmTornado.Acp.Server/NoOpSettingsPersistence.cs` | Create |
| `LlmTornado.Acp.Server/AutoApproveToolApproval.cs` | Create |
| `LlmTornado.Acp.Server/SkillRuntimeConfiguration.cs` | Delete |
| `LlmTornado.Acp.Server/FileRefactoringOrchestrationConfiguration.cs` | Delete |
| `LlmTornado.Acp.Server/FileRefactoringRunnables.cs` | Delete |
| `LlmTornado.Acp.Server/FileRefactoringModels.cs` | Delete |
| `LlmTornado.Acp.Server/Skills/AgentSkill.cs` | Delete |
| `LlmTornado.Acp.Server/Skills/BuiltInSkills.cs` | Delete |
| `LlmTornado.Acp.Server/Skills/SkillLoader.cs` | Delete |
| `LlmTornado.Acp.Server/Skills/*.skill.md` | Delete |
