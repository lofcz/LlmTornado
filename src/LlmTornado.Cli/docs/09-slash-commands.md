# Stage 9: Slash Commands

## Goal

Implement a command dispatch system for `/commands` typed in the REPL. Commands manage models, skills, conversations, tools, MCP connections, and the CLI session.

---

## Files to Create

### `src/LlmTornado.Cli/Commands/ICliCommand.cs`
### `src/LlmTornado.Cli/Commands/CommandDispatcher.cs`
### `src/LlmTornado.Cli/Commands/HelpCommand.cs`
### `src/LlmTornado.Cli/Commands/ModelCommand.cs`
### `src/LlmTornado.Cli/Commands/SkillCommand.cs`
### `src/LlmTornado.Cli/Commands/ConversationCommand.cs`
### `src/LlmTornado.Cli/Commands/ToolsCommand.cs`
### `src/LlmTornado.Cli/Commands/McpCommand.cs`
### `src/LlmTornado.Cli/Commands/ClearCommand.cs`
### `src/LlmTornado.Cli/Commands/ExitCommand.cs`

---

## ICliCommand — Interface

```csharp
namespace LlmTornado.Cli.Commands;

internal interface ICliCommand
{
    /// <summary>
    /// Primary command name (e.g., "model", "skill").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Short description shown in /help.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Detailed usage text shown for /help {command}.
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Execute the command with the given arguments.
    /// </summary>
    /// <param name="args">
    /// Arguments after the command name. E.g., for "/model set gpt-4o",
    /// args = ["set", "gpt-4o"].
    /// </param>
    /// <returns>True if the REPL should continue, false to exit.</returns>
    Task<bool> ExecuteAsync(string[] args);
}
```

---

## CommandDispatcher

```csharp
namespace LlmTornado.Cli.Commands;

internal sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICliCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICliCommand command)
    {
        _commands[command.Name] = command;
    }

    /// <summary>
    /// Check if input starts with / and is a command.
    /// </summary>
    public bool IsCommand(string input) => input.TrimStart().StartsWith('/');

    /// <summary>
    /// Parse and execute a slash command.
    /// </summary>
    /// <returns>True to continue REPL, false to exit.</returns>
    public async Task<bool> DispatchAsync(string input)
    {
        string trimmed = input.TrimStart().TrimStart('/');
        string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return true;

        string commandName = parts[0];
        string[] args = parts.Length > 1 ? parts[1..] : [];

        if (_commands.TryGetValue(commandName, out var command))
        {
            return await command.ExecuteAsync(args);
        }

        ConsoleRenderer.WriteError($"Unknown command: /{commandName}. Type /help for available commands.");
        return true;
    }

    public IReadOnlyDictionary<string, ICliCommand> Commands => _commands;
}
```

---

## Command Reference

### `/help`

```
/help              Show all commands
/help <command>    Show detailed usage for a command
```

```csharp
internal sealed class HelpCommand : ICliCommand
{
    public string Name => "help";
    public string Description => "Show available commands";
    public string Usage => "/help [command]";

    private readonly CommandDispatcher _dispatcher;

    public HelpCommand(CommandDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length > 0 && _dispatcher.Commands.TryGetValue(args[0], out var cmd))
        {
            // Detailed help for a specific command
            ConsoleRenderer.WriteInfo($"/{cmd.Name} — {cmd.Description}");
            ConsoleRenderer.WriteInfo($"Usage: {cmd.Usage}");
        }
        else
        {
            // List all commands
            ConsoleRenderer.WriteInfo("Available commands:");
            foreach (var (name, command) in _dispatcher.Commands.OrderBy(c => c.Key))
            {
                ConsoleRenderer.WriteInfo($"  /{name,-20} {command.Description}");
            }
        }
        return Task.FromResult(true);
    }
}
```

---

### `/model`

```
/model list                    List all available models grouped by provider
/model set <name>              Switch to a different model
/model                         Show the currently active model
```

```csharp
internal sealed class ModelCommand : ICliCommand
{
    public string Name => "model";
    public string Description => "View and switch LLM models";
    public string Usage => "/model [list | set <model-name>]";

    private readonly ProviderDetectionResult _providers;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // Show current model
            ConsoleRenderer.WriteInfo($"Active model: {_builder.ActiveModel.Name}");
            return Task.FromResult(true);
        }

        switch (args[0].ToLower())
        {
            case "list":
                // Group by provider
                foreach (var provider in _providers.Providers)
                {
                    ConsoleRenderer.WriteInfo($"\n  {provider.Provider}:");
                    foreach (var model in provider.Models)
                    {
                        string marker = model == _builder.ActiveModel ? " ← active" : "";
                        ConsoleRenderer.WriteInfo($"    {model.Name}{marker}");
                    }
                }
                break;

            case "set" when args.Length >= 2:
                string modelName = args[1];
                var found = _providers.AllModels
                    .FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
                if (found is null)
                {
                    ConsoleRenderer.WriteError($"Model '{modelName}' not found. Use /model list.");
                }
                else
                {
                    _builder.SetModel(found, _runtimeEventHandler);
                    ConsoleRenderer.WriteInfo($"Switched to: {found.Name}");
                }
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }
}
```

---

### `/skill`

```
/skill list                    List all skills with enabled/disabled status
/skill enable <name>           Enable a skill
/skill disable <name>          Disable a skill
/skill info <name>             Show detailed info (description, scripts, references)
/skill                         Show enabled skill count
```

```csharp
internal sealed class SkillCommand : ICliCommand
{
    public string Name => "skill";
    public string Description => "Manage skills (list, enable, disable, info)";
    public string Usage => "/skill [list | enable <name> | disable <name> | info <name>]";

    private readonly CliSkillManager _skillManager;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            var enabled = _skillManager.GetEnabledSkills();
            var all = _skillManager.GetAllSkills();
            ConsoleRenderer.WriteInfo($"{enabled.Count}/{all.Count} skills enabled.");
            return Task.FromResult(true);
        }

        switch (args[0].ToLower())
        {
            case "list":
                var skills = _skillManager.GetAllSkills();
                if (skills.Count == 0)
                {
                    ConsoleRenderer.WriteInfo("No skills found. Place skills in ./skills/ directory.");
                    break;
                }
                foreach (var skill in skills)
                {
                    string status = skill.Enabled ? "✓" : "✗";
                    string activated = skill.Activated ? " [active in context]" : "";
                    ConsoleRenderer.WriteInfo(
                        $"  {status} {skill.Name,-25} {skill.Description}{activated}");
                }
                break;

            case "enable" when args.Length >= 2:
                if (_skillManager.EnableSkill(args[1]))
                {
                    _builder.RebuildForSkillChange(_runtimeEventHandler);
                    ConsoleRenderer.WriteInfo($"Enabled skill: {args[1]}");
                }
                else
                    ConsoleRenderer.WriteError($"Skill '{args[1]}' not found.");
                break;

            case "disable" when args.Length >= 2:
                if (_skillManager.DisableSkill(args[1]))
                {
                    _builder.RebuildForSkillChange(_runtimeEventHandler);
                    ConsoleRenderer.WriteInfo($"Disabled skill: {args[1]}");
                }
                else
                    ConsoleRenderer.WriteError($"Skill '{args[1]}' not found.");
                break;

            case "info" when args.Length >= 2:
                var info = _skillManager.GetSkill(args[1]);
                if (info is null)
                {
                    ConsoleRenderer.WriteError($"Skill '{args[1]}' not found.");
                    break;
                }
                ConsoleRenderer.WriteInfo($"  Name:          {info.Name}");
                ConsoleRenderer.WriteInfo($"  Description:   {info.Description}");
                ConsoleRenderer.WriteInfo($"  Enabled:       {info.Enabled}");
                ConsoleRenderer.WriteInfo($"  Activated:     {info.Activated}");
                ConsoleRenderer.WriteInfo($"  Path:          {info.DirectoryPath}");
                if (info.License is not null)
                    ConsoleRenderer.WriteInfo($"  License:       {info.License}");
                if (info.Compatibility is not null)
                    ConsoleRenderer.WriteInfo($"  Compatibility: {info.Compatibility}");
                if (info.Scripts.Count > 0)
                    ConsoleRenderer.WriteInfo($"  Scripts:       {string.Join(", ", info.Scripts.Select(s => s.FileName))}");
                if (info.References.Count > 0)
                    ConsoleRenderer.WriteInfo($"  References:    {string.Join(", ", info.References.Select(Path.GetFileName))}");
                if (info.AllowedTools.Count > 0)
                    ConsoleRenderer.WriteInfo($"  Allowed tools: {string.Join(", ", info.AllowedTools)}");
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }
}
```

---

### `/conversation`

```
/conversation save [label]     Save current conversation (optional label)
/conversation list             List saved conversations
/conversation load <id>        Load a saved conversation
/conversation delete <id>      Delete a saved conversation
/conversation new              Start a fresh conversation
/conversation                  Show current conversation stats
```

```csharp
internal sealed class ConversationCommand : ICliCommand
{
    public string Name => "conversation";
    public string Description => "Save, load, and manage conversations";
    public string Usage => "/conversation [save [label] | list | load <id> | delete <id> | new]";

    private readonly ConversationMemoryManager _memory;
    private readonly ConversationStore _store;
    private readonly CliAgentBuilder _builder;

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // Show stats
            var messages = _memory.Messages;
            ConsoleRenderer.WriteInfo($"Current conversation: {messages.Count} messages");
            return true;
        }

        switch (args[0].ToLower())
        {
            case "save":
                string? label = args.Length >= 2 ? string.Join(" ", args[1..]) : null;
                var meta = _store.Save(
                    _memory.Messages.ToList(),
                    label,
                    _builder.ActiveModel.Name,
                    _builder.SkillManager.GetEnabledSkills().Select(s => s.Name).ToList());
                ConsoleRenderer.WriteInfo($"Saved conversation: {meta.Id}");
                break;

            case "list":
                var conversations = _store.List();
                if (conversations.Count == 0)
                {
                    ConsoleRenderer.WriteInfo("No saved conversations.");
                    break;
                }
                ConsoleRenderer.WriteInfo("Saved conversations:");
                foreach (var conv in conversations)
                {
                    string labelDisplay = conv.Label is not null ? $" ({conv.Label})" : "";
                    ConsoleRenderer.WriteInfo(
                        $"  {conv.Id}{labelDisplay}  [{conv.MessageCount} msgs, {conv.Model}]");
                    if (conv.FirstMessagePreview is not null)
                        ConsoleRenderer.WriteInfo($"    \"{conv.FirstMessagePreview}\"");
                }
                break;

            case "load" when args.Length >= 2:
                var messages = _store.Load(args[1]);
                if (messages is null)
                {
                    ConsoleRenderer.WriteError($"Conversation '{args[1]}' not found.");
                    break;
                }
                _memory.LoadConversation(messages);
                ConsoleRenderer.WriteInfo($"Loaded conversation: {args[1]} ({messages.Count} messages)");
                break;

            case "delete" when args.Length >= 2:
                if (_store.Delete(args[1]))
                    ConsoleRenderer.WriteInfo($"Deleted: {args[1]}");
                else
                    ConsoleRenderer.WriteError($"Conversation '{args[1]}' not found.");
                break;

            case "new":
                // Auto-save current if it has messages
                if (_memory.Messages.Count > 0)
                {
                    _store.Save(_memory.Messages.ToList(), null, _builder.ActiveModel.Name, null);
                    ConsoleRenderer.WriteInfo("Previous conversation auto-saved.");
                }
                _memory.NewConversation();
                ConsoleRenderer.WriteInfo("Started new conversation.");
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return true;
    }
}
```

---

### `/tools`

```
/tools list                    Show all registered tools with approval status
/tools reset                   Clear all tool approvals (re-prompt on next use)
/tools reset <name>            Clear approval for a specific tool
```

```csharp
internal sealed class ToolsCommand : ICliCommand
{
    public string Name => "tools";
    public string Description => "View tools and manage approvals";
    public string Usage => "/tools [list | reset [tool-name]]";

    private readonly ToolApprovalManager _approvalManager;
    private readonly CliAgentBuilder _builder;

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || args[0].ToLower() == "list")
        {
            var agent = _builder.Agent;
            var approvals = _approvalManager.GetAllApprovals();

            ConsoleRenderer.WriteInfo("Registered tools:");
            foreach (var (name, tool) in agent.ToolList)
            {
                string approval;
                if (!agent.ToolPermissionRequired.GetValueOrDefault(name, false))
                    approval = "no approval required";
                else if (approvals.TryGetValue(name, out var state))
                    approval = state == ToolApprovalState.AlwaysAllow ? "always allow" : "always deny";
                else
                    approval = "will prompt";

                bool isMcp = agent.McpTools.ContainsKey(name);
                string source = isMcp ? "[MCP]" : "[local]";

                ConsoleRenderer.WriteInfo($"  {source} {name,-35} [{approval}]");
            }
            return Task.FromResult(true);
        }

        if (args[0].ToLower() == "reset")
        {
            if (args.Length >= 2)
            {
                if (_approvalManager.ResetTool(args[1]))
                    ConsoleRenderer.WriteInfo($"Reset approval for: {args[1]}");
                else
                    ConsoleRenderer.WriteError($"No approval found for: {args[1]}");
            }
            else
            {
                _approvalManager.ResetAll();
                ConsoleRenderer.WriteInfo("All tool approvals cleared.");
            }
            return Task.FromResult(true);
        }

        ConsoleRenderer.WriteError($"Usage: {Usage}");
        return Task.FromResult(true);
    }
}
```

---

### `/mcp`

```
/mcp list                      Show MCP server connections and tools
/mcp reload                    Reconnect all MCP servers
```

```csharp
internal sealed class McpCommand : ICliCommand
{
    public string Name => "mcp";
    public string Description => "View and manage MCP server connections";
    public string Usage => "/mcp [list | reload]";

    private readonly McpConfigLoader _mcpLoader;
    private readonly CliAgentBuilder _builder;

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || args[0].ToLower() == "list")
        {
            var statuses = _mcpLoader.ServerStatuses;
            if (statuses.Count == 0)
            {
                ConsoleRenderer.WriteInfo("No MCP servers configured. Add servers to mcp.json.");
                return true;
            }

            ConsoleRenderer.WriteInfo("MCP servers:");
            foreach (var status in statuses)
            {
                string icon = status.Connected ? "✓" : "✗";
                ConsoleRenderer.WriteInfo($"  {icon} {status.Name} ({status.Type})");
                if (status.Connected)
                {
                    ConsoleRenderer.WriteInfo($"    Tools: {string.Join(", ", status.ToolNames)}");
                }
                else
                {
                    ConsoleRenderer.WriteError($"    Error: {status.Error}");
                }
            }
            return true;
        }

        if (args[0].ToLower() == "reload")
        {
            ConsoleRenderer.WriteInfo("Reconnecting MCP servers...");
            await _mcpLoader.ReloadAsync(ConsoleRenderer.WriteInfo);
            _builder.RebuildForSkillChange(/* runtimeEventHandler */);
            ConsoleRenderer.WriteInfo("MCP reload complete.");
            return true;
        }

        ConsoleRenderer.WriteError($"Usage: {Usage}");
        return true;
    }
}
```

---

### `/clear`

```
/clear                         Clear the terminal screen
```

```csharp
internal sealed class ClearCommand : ICliCommand
{
    public string Name => "clear";
    public string Description => "Clear the terminal screen";
    public string Usage => "/clear";

    public Task<bool> ExecuteAsync(string[] args)
    {
        Console.Clear();
        return Task.FromResult(true);
    }
}
```

---

### `/exit`

```
/exit                          Exit the CLI (auto-saves conversation)
```

```csharp
internal sealed class ExitCommand : ICliCommand
{
    public string Name => "exit";
    public string Description => "Exit the CLI agent";
    public string Usage => "/exit";

    private readonly ConversationMemoryManager _memory;
    private readonly ConversationStore _store;
    private readonly CliAgentBuilder _builder;

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (_memory.Messages.Count > 0)
        {
            _store.Save(_memory.Messages.ToList(), null, _builder.ActiveModel.Name, null);
            ConsoleRenderer.WriteInfo("Conversation auto-saved.");
        }
        ConsoleRenderer.WriteInfo("Goodbye!");
        return Task.FromResult(false);  // false = exit REPL
    }
}
```

---

## Registration

All commands are registered in `Program.cs` during startup:

```csharp
var dispatcher = new CommandDispatcher();
dispatcher.Register(new HelpCommand(dispatcher));
dispatcher.Register(new ModelCommand(providers, builder, runtimeEventHandler));
dispatcher.Register(new SkillCommand(skillManager, builder, runtimeEventHandler));
dispatcher.Register(new ConversationCommand(memory, store, builder));
dispatcher.Register(new ToolsCommand(approvalManager, builder));
dispatcher.Register(new McpCommand(mcpLoader, builder));
dispatcher.Register(new ClearCommand());
dispatcher.Register(new ExitCommand(memory, store, builder));
```

---

## Command Autocomplete (Optional Enhancement)

A possible future enhancement: when the user types `/` and presses Tab, show available commands. This would require reading individual keystrokes via `Console.ReadKey()` instead of `Console.ReadLine()`. Not in initial scope but noted as a natural extension.
