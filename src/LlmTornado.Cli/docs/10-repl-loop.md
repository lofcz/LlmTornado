# Stage 10: REPL Loop

## Goal

Create the main entry point (`Program.cs`) that initializes all subsystems in the correct order, runs the interactive REPL loop, and handles graceful shutdown.

---

## File to Create

### `src/LlmTornado.Cli/Program.cs`

---

## Startup Sequence

```
┌──────────────────────────────────────────────────────────────┐
│ 1. Initialize storage         (CliStorage.Initialize)        │
│ 2. Load settings              (CliSettings from settings.json)│
│ 3. Detect providers           (ProviderDetector.Detect)      │
│ 4. Load skills                (CliSkillManager.LoadSkills)   │
│ 5. Load MCP config            (McpConfigLoader.LoadAsync)    │
│ 6. Initialize tool approval   (ToolApprovalManager)          │
│ 7. Initialize conversation    (ConversationMemoryManager)    │
│ 8. Build agent                (CliAgentBuilder.Build)        │
│ 9. Register commands          (CommandDispatcher)            │
│ 10. Print banner              (Console output)               │
│ 11. Enter REPL loop           (while true: read, dispatch)   │
└──────────────────────────────────────────────────────────────┘
```

---

## Full Program.cs Pseudocode

```csharp
namespace LlmTornado.Cli;

using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Memory;
using LlmTornado.Cli.Mcp;
using LlmTornado.Cli.Skills;

class Program
{
    // References kept for graceful shutdown
    private static McpConfigLoader? _mcpLoader;
    private static ConversationMemoryManager? _memoryManager;
    private static ConversationStore? _conversationStore;
    private static CliAgentBuilder? _agentBuilder;

    static async Task<int> Main(string[] args)
    {
        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            return await RunAsync(args);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        // ─── Step 1: Storage ───
        ConsoleRenderer.WriteBanner();
        CliStorage.Initialize();

        // ─── Step 2: Settings ───
        var settings = CliStorage.LoadJson<CliSettings>(CliStorage.SettingsPath) 
                       ?? new CliSettings();

        // ─── Step 3: Provider Detection ───
        ConsoleRenderer.WriteInfo("Detecting providers...");
        var providerResult = ProviderDetector.Detect();
        if (providerResult is null)
        {
            ConsoleRenderer.WriteError(
                "No LLM providers detected. Set at least one API key environment variable:");
            ConsoleRenderer.WriteError(
                "  OPENAI_API_KEY, ANTHROPIC_API_KEY, GOOGLE_API_KEY, etc.");
            return 1;
        }

        // Apply saved model preference
        if (settings.ActiveModel is not null)
        {
            var savedModel = providerResult.AllModels
                .FirstOrDefault(m => m.Name == settings.ActiveModel);
            if (savedModel is not null)
                providerResult.ActiveModel = savedModel;
        }

        ConsoleRenderer.WriteProviderSummary(providerResult);

        // ─── Step 4: Skills ───
        var skillManager = new CliSkillManager(settings);
        string skillsDir = CliSkillLoader.ResolveSkillsDirectory(settings);
        skillManager.LoadSkills(skillsDir);
        ConsoleRenderer.WriteInfo(
            $"Skills: {skillManager.GetEnabledSkills().Count} enabled, " +
            $"{skillManager.GetAllSkills().Count} total (from {skillsDir})");

        // ─── Step 5: MCP ───
        _mcpLoader = new McpConfigLoader();
        string? mcpConfigPath = McpConfigLoader.ResolveMcpConfigPath(settings);
        if (mcpConfigPath is not null)
        {
            ConsoleRenderer.WriteInfo($"Loading MCP servers from {mcpConfigPath}...");
            await _mcpLoader.LoadAsync(mcpConfigPath, ConsoleRenderer.WriteInfo);
        }
        else
        {
            ConsoleRenderer.WriteInfo("No mcp.json found. MCP tools not loaded.");
        }

        // ─── Step 6: Tool Approval ───
        var renderer = new ConsoleRenderer();
        var toolApproval = new ToolApprovalManager(renderer);

        // ─── Step 7: Conversation Memory ───
        _memoryManager = new ConversationMemoryManager(
            providerResult.Api,
            providerResult.ActiveModel,
            providerResult.ActiveModel.ContextTokens);
        _conversationStore = new ConversationStore();

        if (_memoryManager.Messages.Count > 0)
        {
            ConsoleRenderer.WriteInfo(
                $"Resuming previous conversation ({_memoryManager.Messages.Count} messages).");
        }

        // ─── Step 8: Build Agent ───
        _agentBuilder = new CliAgentBuilder(
            providerResult.Api,
            providerResult.ActiveModel,
            skillManager,
            _mcpLoader,
            toolApproval,
            _memoryManager);

        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler = HandleRuntimeEvent;
        var runtime = _agentBuilder.Build(runtimeEventHandler);

        // ─── Step 9: Register Commands ───
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new HelpCommand(dispatcher));
        dispatcher.Register(new ModelCommand(providerResult, _agentBuilder, runtimeEventHandler));
        dispatcher.Register(new SkillCommand(skillManager, _agentBuilder, runtimeEventHandler));
        dispatcher.Register(new ConversationCommand(_memoryManager, _conversationStore, _agentBuilder));
        dispatcher.Register(new ToolsCommand(toolApproval, _agentBuilder));
        dispatcher.Register(new McpCommand(_mcpLoader, _agentBuilder));
        dispatcher.Register(new ClearCommand());
        dispatcher.Register(new ExitCommand(_memoryManager, _conversationStore, _agentBuilder));

        // ─── Step 10: Banner ───
        ConsoleRenderer.WriteInfo($"\nActive model: {providerResult.ActiveModel.Name}");
        ConsoleRenderer.WriteInfo("Type /help for commands. Start chatting!\n");

        // ─── Step 11: REPL Loop ───
        return await ReplLoop(runtime, dispatcher, _memoryManager, _agentBuilder, runtimeEventHandler);
    }

    private static async Task<int> ReplLoop(
        ChatRuntime runtime,
        CommandDispatcher dispatcher,
        ConversationMemoryManager memory,
        CliAgentBuilder builder,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        while (true)
        {
            // Prompt
            ConsoleRenderer.WritePrompt(builder.ActiveModel.Name);

            // Read input
            string? input = Console.ReadLine();
            if (input is null)  // EOF (pipe closed, Ctrl+D on Unix)
                break;

            input = input.Trim();
            if (string.IsNullOrEmpty(input))
                continue;

            // Slash commands
            if (dispatcher.IsCommand(input))
            {
                bool shouldContinue = await dispatcher.DispatchAsync(input);
                if (!shouldContinue)
                    break;

                // Commands may rebuild the runtime
                runtime = builder.Runtime;
                continue;
            }

            // Chat message
            try
            {
                var userMessage = new ChatMessage(ChatMessageRoles.User, input);
                memory.AddMessage(userMessage);

                // Invoke agent (streaming output handled by runtimeEventHandler)
                var response = await runtime.InvokeAsync(userMessage);

                // Store assistant response
                memory.AddMessage(response);

                // Check if summarization is needed (runs in the foreground)
                bool summarized = await memory.MaybeSummarize();
                if (summarized)
                {
                    ConsoleRenderer.WriteInfo("[context compressed]");
                }
            }
            catch (OperationCanceledException)
            {
                ConsoleRenderer.WriteInfo("\n[cancelled]");
            }
            catch (Exception ex)
            {
                ConsoleRenderer.WriteError($"Error: {ex.Message}");
            }

            Console.WriteLine(); // Blank line between turns
        }

        return 0;
    }

    /// <summary>
    /// Handles streaming events from the ChatRuntime.
    /// </summary>
    private static async ValueTask HandleRuntimeEvent(ChatRuntimeEvents evt)
    {
        switch (evt)
        {
            case ChatRuntimeStartedEvent:
                // Could show a spinner or "thinking..." indicator
                break;

            case ChatRuntimeAgentRunnerEvent runnerEvt:
                if (runnerEvt.RunnerEvent is AgentRunnerStreamingEvent streamEvt)
                {
                    if (streamEvt.Event is ModelStreamingOutputTextDeltaEvent delta)
                    {
                        ConsoleRenderer.WriteStreamingToken(delta.DeltaText);
                    }
                }
                break;

            case ChatRuntimeCompletedEvent:
                ConsoleRenderer.EndStreamingResponse();
                break;

            case ChatRuntimeErrorEvent errorEvt:
                ConsoleRenderer.WriteError($"\n[Error: {errorEvt.Exception?.Message}]");
                break;
        }
    }

    /// <summary>
    /// Graceful Ctrl+C handling.
    /// </summary>
    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;  // Don't terminate immediately
        _agentBuilder?.Runtime?.CancelExecution();
    }
}
```

---

## Input Handling Details

### Multi-line Input

For now, input is single-line via `Console.ReadLine()`. A future enhancement could support multi-line input (backslash continuation or paste detection):

```
You: This is line one \
     and this continues.
```

Not in initial scope — keep it simple with single-line input.

### Empty Input

Empty input (just pressing Enter) is silently ignored and the prompt is re-displayed.

### EOF / Pipe

When stdin is a pipe (e.g., `echo "hello" | dotnet run --project LlmTornado.Cli`), `Console.ReadLine()` returns `null` at EOF. The loop exits cleanly.

---

## Error Recovery

| Error Type | Handling |
|-----------|----------|
| API auth failure | Caught by general Exception handler, printed, continue loop |
| Network timeout | Same — agent returns error, loop continues |
| Tool execution failure | Agent receives error as tool result, decides how to proceed |
| MCP server disconnect | Tool calls fail with error, loop continues |
| Summarization failure | `MaybeSummarize()` catches internally, logs, returns false |
| Ctrl+C during streaming | `CancelExecution()` on runtime, cancellation token propagates |

---

## Startup Banner

```
╭─────────────────────────────────────╮
│       LlmTornado CLI Agent          │
│       v1.0.0                        │
╰─────────────────────────────────────╯

Detected providers:
  ✓ Anthropic (ANTHROPIC_API_KEY)  — 8 models
  ✓ OpenAI (OPENAI_API_KEY)        — 12 models

Skills: 3 enabled, 5 total (from ./skills/)
MCP: 2 servers connected (5 tools)

Active model: claude-3-7-sonnet
Type /help for commands. Start chatting!

claude-3-7-sonnet> _
```

---

## Prompt Format

The prompt shows the active model name:

```
claude-3-7-sonnet> user types here
```

When streaming a response:

```
claude-3-7-sonnet> What is LLMTornado?
LLMTornado is a .NET library for working with Large Language Models...
[context compressed]

claude-3-7-sonnet> _
```

---

## Exit Flow

```
/exit
  → ExitCommand.ExecuteAsync()
    → Auto-save conversation if non-empty
    → Print "Goodbye!"
    → Return false (exit REPL)
  → Main loop breaks
  → McpConfigLoader.DisposeAsync() (disconnect MCP servers)
  → Return 0
```

For Ctrl+C:
```
Ctrl+C
  → OnCancelKeyPress fires
  → CancelExecution() on current runtime
  → If mid-streaming, cancellation token fires, agent loop stops
  → If at prompt (ReadLine), no effect — user must type /exit or Ctrl+C again
```

---

## Types Used from LlmTornado

| Type | Purpose |
|------|---------|
| `ChatRuntime` | `InvokeAsync()` for each user message |
| `ChatMessage` | User input → `ChatMessage(ChatMessageRoles.User, input)` |
| `ChatMessageRoles` | Role enum |
| `ChatRuntimeEvents` | Event hierarchy for streaming |
| `ChatRuntimeStartedEvent` | Agent started |
| `ChatRuntimeCompletedEvent` | Agent finished |
| `ChatRuntimeErrorEvent` | Agent error |
| `ChatRuntimeAgentRunnerEvent` | Runner event wrapper |
| `AgentRunnerStreamingEvent` | Streaming sub-event |
| `ModelStreamingOutputTextDeltaEvent` | Text delta token |
