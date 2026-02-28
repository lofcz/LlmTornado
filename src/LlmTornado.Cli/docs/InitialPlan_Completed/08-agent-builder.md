# Stage 8: Agent Builder

## Goal

Assemble a fully configured `TornadoAgent` and `IRuntimeConfiguration` from detected providers, loaded skills, MCP tools, and tool approval settings. Provide the ability to rebuild the agent on the fly when skills are toggled or the model is changed.

---

## File to Create

### `src/LlmTornado.Cli/CliAgentBuilder.cs`

---

## Inputs

| Input | Source | Stage |
|-------|--------|-------|
| `TornadoApi` | `ProviderDetector` | 2 |
| `ChatModel` (active model) | `ProviderDetectionResult.ActiveModel` | 2 |
| `List<CliSkill>` (enabled skills) | `CliSkillManager.GetEnabledSkills()` | 4 |
| `List<Tool>` (script tools) | `ScriptToolBuilder.BuildScriptTools()` | 4 |
| `List<Tool>` (MCP tools) | `McpConfigLoader.AllTools` | 5 |
| `ToolApprovalManager` | Instance from startup | 6 |
| `ConversationMemoryManager` | Instance from startup | 7 |

---

## Outputs

| Output | Type | Used By |
|--------|------|---------|
| `ChatRuntime` | `ChatRuntime` | REPL loop's `InvokeAsync()` |
| `TornadoAgent` | `TornadoAgent` | Direct access for reconfiguration |

---

## CliAgentBuilder — Implementation

```csharp
namespace LlmTornado.Cli;

using LlmTornado.Cli.Skills;
using LlmTornado.Cli.Memory;

internal sealed class CliAgentBuilder
{
    private readonly TornadoApi _api;
    private readonly CliSkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly ToolApprovalManager _toolApproval;
    private readonly ConversationMemoryManager _memoryManager;

    private ChatModel _activeModel;
    private TornadoAgent? _agent;
    private ChatRuntime? _runtime;

    public TornadoAgent Agent => _agent ?? throw new InvalidOperationException("Agent not built");
    public ChatRuntime Runtime => _runtime ?? throw new InvalidOperationException("Runtime not built");

    public CliAgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        CliSkillManager skillManager,
        McpConfigLoader mcpLoader,
        ToolApprovalManager toolApproval,
        ConversationMemoryManager memoryManager)
    {
        _api = api;
        _activeModel = activeModel;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _toolApproval = toolApproval;
        _memoryManager = memoryManager;
    }

    /// <summary>
    /// Build or rebuild the agent and runtime. Called at startup and after
    /// /model set, /skill enable, /skill disable.
    /// </summary>
    public ChatRuntime Build(
        Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        // 1. Build system prompt with skill metadata
        string systemPrompt = BuildSystemPrompt();

        // 2. Collect all tools
        var allTools = CollectTools();

        // 3. Create agent
        _agent = new TornadoAgent(
            client: _api,
            model: _activeModel,
            name: "CLI-Agent",
            instructions: systemPrompt,
            streaming: true
        );

        // 4. Add tools
        foreach (var tool in allTools)
        {
            _agent.AddTool(tool);
        }

        // 5. Configure tool permissions
        ConfigureToolPermissions(allTools);

        // 6. Create runtime configuration
        var runtimeConfig = new SingletonRuntimeConfiguration(_agent);
        runtimeConfig.OnRuntimeEvent = onRuntimeEvent;
        runtimeConfig.OnRuntimeRequestEvent = _toolApproval.HandleToolPermissionRequest;

        // 7. Create ChatRuntime
        _runtime = new ChatRuntime(runtimeConfig);

        return _runtime;
    }

    /// <summary>
    /// Update the active model and rebuild.
    /// </summary>
    public ChatRuntime SetModel(
        ChatModel model,
        Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        _activeModel = model;
        _memoryManager.UpdateModel(model, model.ContextTokens);
        return Build(onRuntimeEvent);
    }

    /// <summary>
    /// Rebuild after skill enable/disable.
    /// </summary>
    public ChatRuntime RebuildForSkillChange(
        Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return Build(onRuntimeEvent);
    }

    public ChatModel ActiveModel => _activeModel;
}
```

---

## System Prompt Construction

The system prompt follows the Agent Skills progressive disclosure pattern:

```csharp
private string BuildSystemPrompt()
{
    var sb = new StringBuilder();

    // Base instructions
    sb.AppendLine("You are a helpful CLI assistant with access to skills and tools.");
    sb.AppendLine("You can activate skills to gain specialized knowledge and capabilities.");
    sb.AppendLine();

    // Skill metadata (progressive disclosure — only names + descriptions)
    var enabledSkills = _skillManager.GetEnabledSkills();
    if (enabledSkills.Count > 0)
    {
        sb.AppendLine(_skillManager.BuildSkillsContextXml());
        sb.AppendLine();
        sb.AppendLine("When a user's task matches a skill, use the `load_skill` tool to activate it.");
        sb.AppendLine("The skill's full instructions will be returned and you should follow them.");
        sb.AppendLine();
    }

    // Working directory context
    sb.AppendLine($"The user's current working directory is: {Environment.CurrentDirectory}");

    return sb.ToString();
}
```

### Example Generated System Prompt

```
You are a helpful CLI assistant with access to skills and tools.
You can activate skills to gain specialized knowledge and capabilities.

<available_skills>
  <skill>
    <name>code-review</name>
    <description>Reviews code for bugs, security issues, and adherence to best practices.</description>
  </skill>
  <skill>
    <name>data-analysis</name>
    <description>Analyzes datasets, generates charts, and creates summary reports.</description>
  </skill>
</available_skills>

When a user's task matches a skill, use the `load_skill` tool to activate it.
The skill's full instructions will be returned and you should follow them.

The user's current working directory is: C:\Users\john\projects\my-app
```

---

## Tool Collection

```csharp
private List<Tool> CollectTools()
{
    var tools = new List<Tool>();

    // 1. Built-in skill management tools
    tools.Add(BuildLoadSkillTool());
    tools.Add(BuildListSkillsTool());
    tools.Add(BuildReadReferenceTool());

    // 2. Script tools from enabled skills
    var enabledSkills = _skillManager.GetEnabledSkills();
    tools.AddRange(ScriptToolBuilder.BuildScriptTools(enabledSkills));

    // 3. MCP tools
    tools.AddRange(_mcpLoader.AllTools);

    return tools;
}
```

---

## Built-in Tools

### `load_skill` — Activate a Skill

```csharp
private Tool BuildLoadSkillTool()
{
    return new Tool(
        (string skillName) =>
        {
            var instructions = _skillManager.ActivateSkill(skillName);
            if (instructions is null)
                return $"Skill '{skillName}' not found. Use list_skills to see available skills.";
            return instructions;
        },
        "load_skill",
        "Load and activate a skill by name. Returns the skill's full instructions. " +
        "Use this when a user's task matches a skill from the available_skills list."
    );
}
```

**Why this matters**: This is the progressive disclosure pattern from the Agent Skills spec. At startup, only skill names + descriptions are in context (~100 tokens each). When the agent calls `load_skill`, the full SKILL.md body is returned (<5000 tokens), giving the agent detailed instructions on demand.

### `list_skills` — Show Available Skills

```csharp
private Tool BuildListSkillsTool()
{
    return new Tool(
        () =>
        {
            var skills = _skillManager.GetEnabledSkills();
            if (skills.Count == 0)
                return "No skills are currently enabled.";

            var sb = new StringBuilder();
            sb.AppendLine("Available skills:");
            foreach (var skill in skills)
            {
                string status = skill.Activated ? " [active]" : "";
                sb.AppendLine($"  - {skill.Name}: {skill.Description}{status}");
                if (skill.Scripts.Count > 0)
                {
                    sb.AppendLine($"    Scripts: {string.Join(", ", skill.Scripts.Select(s => s.FileName))}");
                }
            }
            return sb.ToString();
        },
        "list_skills",
        "List all enabled skills with their descriptions and available scripts."
    );
}
```

### `read_reference` — Load a Reference File

```csharp
private Tool BuildReadReferenceTool()
{
    return new Tool(
        (string skillName, string relativePath) =>
        {
            var skill = _skillManager.GetSkill(skillName);
            if (skill is null)
                return $"Skill '{skillName}' not found.";

            // Security: resolve and validate the path is within the skill directory
            string fullPath = Path.GetFullPath(Path.Combine(skill.DirectoryPath, relativePath));
            if (!fullPath.StartsWith(skill.DirectoryPath))
                return "Access denied: path is outside the skill directory.";

            if (!File.Exists(fullPath))
                return $"File not found: {relativePath}";

            string content = File.ReadAllText(fullPath);
            if (content.Length > 50_000)
                content = content[..50_000] + "\n[truncated at 50000 characters]";

            return content;
        },
        "read_reference",
        "Read a reference file from a skill's directory. Use relative paths (e.g., 'references/GUIDE.md')."
    );
}
```

---

## Tool Permission Configuration

```csharp
private void ConfigureToolPermissions(List<Tool> allTools)
{
    foreach (var tool in allTools)
    {
        string name = tool.Function?.Name ?? tool.ToolName ?? "unknown";

        // Safe built-in tools — no permission needed
        if (name is "load_skill" or "list_skills" or "read_reference")
        {
            _agent!.ToolPermissionRequired[name] = false;
            continue;
        }

        // Everything else requires permission (checked by ToolApprovalManager)
        _agent!.ToolPermissionRequired[name] = true;
    }

    // Pre-approve tools from skill allowed-tools frontmatter
    foreach (var skill in _skillManager.GetEnabledSkills())
    {
        _toolApproval.PreApproveSkillTools(skill.AllowedTools);
    }
}
```

---

## Agent Rebuild Flow

When the user changes model or toggles skills, the agent must be rebuilt:

```
/model set gpt-4o
  → CliAgentBuilder.SetModel(ChatModel.OpenAi.Gpt4.O)
    → Updates _activeModel
    → Updates ConversationMemoryManager model + context window
    → Calls Build() to reconstruct agent + runtime
    → Returns new ChatRuntime

/skill disable code-review
  → CliSkillManager.DisableSkill("code-review")
  → CliAgentBuilder.RebuildForSkillChange()
    → Calls Build() to reconstruct with updated skill list
    → Script tools from disabled skill are removed
    → System prompt updated (disabled skill removed from available_skills XML)
    → Returns new ChatRuntime
```

**Important**: Conversation history is preserved across rebuilds via `ConversationMemoryManager`. Only the agent and runtime are reconstructed.

---

## Runtime Event Handling

The `onRuntimeEvent` callback handles streaming output:

```csharp
// Passed to Build() from Program.cs:
async ValueTask HandleRuntimeEvent(ChatRuntimeEvents evt)
{
    switch (evt)
    {
        case ChatRuntimeStartedEvent:
            // Agent started processing
            break;

        case ChatRuntimeAgentRunnerEvent runnerEvt:
            if (runnerEvt.RunnerEvent is AgentRunnerStreamingEvent streamEvt)
            {
                if (streamEvt.Event is ModelStreamingOutputTextDeltaEvent delta)
                {
                    // Stream text to console
                    ConsoleRenderer.WriteStreamingToken(delta.DeltaText);
                }
            }
            break;

        case ChatRuntimeCompletedEvent:
            // Agent finished, newline after streamed content
            ConsoleRenderer.EndStreamingResponse();
            break;

        case ChatRuntimeErrorEvent errorEvt:
            ConsoleRenderer.WriteError(errorEvt.Exception.Message);
            break;
    }
}
```

---

## Types Used from LlmTornado

| Type | Namespace | Purpose |
|------|-----------|---------|
| `TornadoAgent` | `LlmTornado.Agents` | Agent construction |
| `ChatRuntime` | `LlmTornado.Agents` | Runtime wrapper |
| `SingletonRuntimeConfiguration` | `LlmTornado.Agents` | Single-agent config |
| `ChatRuntimeEvents` | `LlmTornado.Agents` | Event base class |
| `ChatRuntimeStartedEvent` | `LlmTornado.Agents` | Started event |
| `ChatRuntimeCompletedEvent` | `LlmTornado.Agents` | Completed event |
| `ChatRuntimeAgentRunnerEvent` | `LlmTornado.Agents` | Runner event wrapper |
| `AgentRunnerStreamingEvent` | `LlmTornado.Agents` | Streaming event |
| `ModelStreamingOutputTextDeltaEvent` | `LlmTornado.Agents` | Text delta |
| `Tool` | `LlmTornado.Chat` | Tool definition |
| `ChatModel` | `LlmTornado.Chat` | Model identifier |
| `IRuntimeConfiguration` | `LlmTornado.Agents` | Runtime config interface |
