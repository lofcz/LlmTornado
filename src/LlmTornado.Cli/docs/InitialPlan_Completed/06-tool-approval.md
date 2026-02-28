# Stage 6: Tool Approval

## Goal

Implement a tool approval system that requires user confirmation before executing any tool for the first time. Users can choose to allow once, always allow, always deny, or deny once. Approval decisions are persisted to disk so they survive across sessions.

---

## File to Create

### `src/LlmTornado.Cli/ToolApprovalManager.cs`

---

## How Tool Approval Works in TornadoRunner

The existing `TornadoRunner.HandleToolCall` has built-in support for tool permission:

```csharp
// In TornadoRunner.HandleToolCall():
if (agent.ToolPermissionRequired[toolCall.Name])
{
    string requestMessage = $"Tool: {toolCall.Name}\nArguments: {toolCall.Arguments}";
    permissionGranted = await toolPermissionHandle.Invoke(requestMessage);
}

if (!permissionGranted)
    functionResult = new FunctionResult(toolCall, "Tool Permission was not granted by user");
else
    // Execute tool (MCP or local)
```

The CLI hooks into this via:
1. Setting `agent.ToolPermissionRequired[toolName] = true` for every tool
2. Passing a `Func<string, ValueTask<bool>>` delegate to `Agent.Run(toolPermissionHandle: ...)` through the runtime configuration

---

## Approval States

```csharp
internal enum ToolApprovalState
{
    /// <summary>
    /// No decision recorded — will prompt the user.
    /// </summary>
    Unknown,

    /// <summary>
    /// User chose "Always allow" — execute without prompting.
    /// </summary>
    AlwaysAllow,

    /// <summary>
    /// User chose "Always deny" — reject without prompting.
    /// </summary>
    AlwaysDeny
}
```

---

## Persisted File Format

`tool-approvals.json` at `CliStorage.ToolApprovalsPath`:

```json
{
    "code-review:lint": "allow",
    "code-review:check-types": "allow",
    "mcp:filesystem:read_file": "allow",
    "mcp:filesystem:write_file": "deny",
    "mcp:github:create_issue": "allow",
    "load_skill": "allow",
    "list_skills": "allow"
}
```

Values: `"allow"` or `"deny"`. Tools not in the file have `Unknown` state.

---

## ToolApprovalManager — Implementation

```csharp
namespace LlmTornado.Cli;

internal sealed class ToolApprovalManager
{
    private readonly Dictionary<string, ToolApprovalState> _approvals = new();
    private readonly ConsoleRenderer _renderer;

    public ToolApprovalManager(ConsoleRenderer renderer)
    {
        LoadFromDisk();
    }

    /// <summary>
    /// The delegate to pass as toolPermissionHandle.
    /// Called by TornadoRunner when a tool has ToolPermissionRequired=true.
    /// </summary>
    /// <param name="requestMessage">
    /// Format: "Tool: {name}\nArguments: {args}" (built by TornadoRunner)
    /// </param>
    public async ValueTask<bool> HandleToolPermissionRequest(string requestMessage)
    {
        // 1. Parse tool name from requestMessage
        string toolName = ParseToolName(requestMessage);

        // 2. Check persisted state
        if (_approvals.TryGetValue(toolName, out var state))
        {
            switch (state)
            {
                case ToolApprovalState.AlwaysAllow:
                    _renderer.WriteToolAutoApproved(toolName);
                    return true;
                case ToolApprovalState.AlwaysDeny:
                    _renderer.WriteToolAutoDenied(toolName);
                    return false;
            }
        }

        // 3. Prompt user for decision
        return await PromptForApproval(toolName, requestMessage);
    }

    private async ValueTask<bool> PromptForApproval(string toolName, string requestMessage)
    {
        _renderer.WriteToolApprovalPrompt(requestMessage);
        // Display:
        //
        // ╭─ Tool Call Request ────────────────────────╮
        // │ Tool: code-review:lint                     │
        // │ Arguments: {"path": "src/main.py"}         │
        // ╰────────────────────────────────────────────╯
        //
        // [1] Allow once
        // [2] Always allow (remember for this tool)
        // [3] Deny once
        // [4] Always deny (remember for this tool)
        //
        // Choice [1-4]:

        while (true)
        {
            string? input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    return true;

                case "2":
                    _approvals[toolName] = ToolApprovalState.AlwaysAllow;
                    SaveToDisk();
                    _renderer.WriteInfo($"Tool '{toolName}' will be auto-approved in future.");
                    return true;

                case "3":
                    return false;

                case "4":
                    _approvals[toolName] = ToolApprovalState.AlwaysDeny;
                    SaveToDisk();
                    _renderer.WriteInfo($"Tool '{toolName}' will be auto-denied in future.");
                    return false;

                default:
                    _renderer.WriteError("Please enter 1, 2, 3, or 4.");
                    break;
            }
        }
    }

    /// <summary>
    /// Get all approval states. Used by /tools list.
    /// </summary>
    public Dictionary<string, ToolApprovalState> GetAllApprovals() => new(_approvals);

    /// <summary>
    /// Reset all approvals. Used by /tools reset.
    /// </summary>
    public void ResetAll()
    {
        _approvals.Clear();
        SaveToDisk();
    }

    /// <summary>
    /// Reset a single tool's approval.
    /// </summary>
    public bool ResetTool(string toolName)
    {
        bool removed = _approvals.Remove(toolName);
        if (removed) SaveToDisk();
        return removed;
    }

    /// <summary>
    /// Pre-approve tools from a skill's allowed-tools frontmatter.
    /// Only sets if the tool doesn't already have a decision.
    /// </summary>
    public void PreApproveSkillTools(IEnumerable<string> toolNames)
    {
        bool changed = false;
        foreach (string name in toolNames)
        {
            if (!_approvals.ContainsKey(name))
            {
                _approvals[name] = ToolApprovalState.AlwaysAllow;
                changed = true;
            }
        }
        if (changed) SaveToDisk();
    }

    // --- Persistence ---

    private void LoadFromDisk()
    {
        var data = CliStorage.LoadJson<Dictionary<string, string>>(CliStorage.ToolApprovalsPath);
        if (data is null) return;

        foreach (var (key, value) in data)
        {
            _approvals[key] = value switch
            {
                "allow" => ToolApprovalState.AlwaysAllow,
                "deny" => ToolApprovalState.AlwaysDeny,
                _ => ToolApprovalState.Unknown
            };
        }
    }

    private void SaveToDisk()
    {
        var data = _approvals
            .Where(kv => kv.Value != ToolApprovalState.Unknown)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value == ToolApprovalState.AlwaysAllow ? "allow" : "deny"
            );
        CliStorage.SaveJson(CliStorage.ToolApprovalsPath, data);
    }

    private static string ParseToolName(string requestMessage)
    {
        var firstLine = requestMessage.Split('\n')[0];
        return firstLine.Replace("Tool: ", "").Trim();
    }
}
```

---

## Wiring to the Agent

In `CliAgentBuilder` (Stage 8):

```csharp
// 1. Set all tools to require permission
foreach (var tool in allTools)
{
    string name = tool.Function?.Name ?? tool.ToolName ?? "unknown";
    agent.ToolPermissionRequired[name] = true;
}

// 2. Exempt safe built-in tools
agent.ToolPermissionRequired["load_skill"] = false;
agent.ToolPermissionRequired["list_skills"] = false;

// 3. Pre-approve tools from skill allowed-tools frontmatter
foreach (var skill in enabledSkills)
{
    toolApprovalManager.PreApproveSkillTools(skill.AllowedTools);
}
```

In the runtime configuration:

```csharp
// Via IRuntimeConfiguration.OnRuntimeRequestEvent
runtimeConfig.OnRuntimeRequestEvent = toolApprovalManager.HandleToolPermissionRequest;
```

---

## User Interaction Flow

### First time a tool is called

```
You: Review the code in src/main.py

╭─ Tool Call Request ────────────────────────╮
│ Tool: code-review:lint                     │
│ Arguments: {"path": "src/main.py"}         │
╰────────────────────────────────────────────╯

[1] Allow once
[2] Always allow (remember for this tool)
[3] Deny once
[4] Always deny (remember for this tool)

Choice [1-4]: 2
✓ Tool 'code-review:lint' will be auto-approved in future.

[Running code-review:lint...]
```

### Subsequent calls to the same tool

```
You: Also check src/utils.py

✓ [auto-approved] code-review:lint
[Running code-review:lint...]
```

### Viewing approval status

```
/tools list

Registered Tools:
  ✓ code-review:lint          [always allow]
  ✓ code-review:check-types   [always allow]
  ✓ mcp:filesystem:read_file  [always allow]
  ✗ mcp:filesystem:write_file [always deny]
  ? load_skill                 [no approval required]
  ? list_skills                [no approval required]
```

### Resetting approvals

```
/tools reset
All tool approvals have been cleared. Tools will prompt for approval on next use.
```

---

## Integration with Skill `allowed-tools`

The Agent Skills standard supports an `allowed-tools` frontmatter field:

```yaml
---
name: code-review
allowed-tools: code-review:lint code-review:check-types
---
```

When a skill is loaded, its `allowed-tools` are pre-approved via `PreApproveSkillTools()`. This means tools listed in the skill's frontmatter won't prompt on first use, but the user can still override by using `/tools reset` or manually editing `tool-approvals.json`.

---

## Interaction with Streaming

When the agent is streaming a response and decides to call a tool:
1. Streaming pauses (the runner awaits the tool call)
2. The approval prompt is displayed
3. User makes their choice
4. Tool executes (or is denied)
5. The tool result is passed back to the model
6. Streaming resumes with the model's next response

This works naturally because `TornadoRunner` awaits `toolPermissionHandle.Invoke()` before proceeding.

---

## Types Used from LlmTornado

| Type | Purpose |
|------|---------|
| `TornadoAgent.ToolPermissionRequired` | Dict controlling which tools need permission |
| `TornadoRunner` (internal) | Calls `toolPermissionHandle` during `HandleToolCall` |
| `IRuntimeConfiguration.OnRuntimeRequestEvent` | The delegate hook for permission requests |
