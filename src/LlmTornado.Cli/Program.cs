using System.Text;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Input;
using LlmTornado.Cli.Input;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.Tools;
using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Cli.Core.State;
using LlmTornado.Code;

namespace LlmTornado.Cli;

class Program
{
    private static McpConfigLoader? _mcpLoader;
    private static ConversationMemoryManager? _memoryManager;
    private static SqliteConversationStore? _conversationStore;
    private static SqliteAgentStateStore? _agentStateStore;
    private static CliAgentBuilder? _agentBuilder;
    private static bool _showThinking = true;

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

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
        AgentSettings settings = CliStorage.LoadJson<AgentSettings>(CliStorage.SettingsPath)
                              ?? new AgentSettings();
        _showThinking = settings.ShowThinking;
        MessageTimestampPrefixer.Enabled = settings.ShowTimestamps;
        CliSettingsPersistence persistence = new();

        // ─── Step 3: Provider Detection ───
        ConsoleRenderer.WriteInfo("Detecting providers...");
        ProviderDetectionResult? providerResult = ProviderDetector.Detect();
        if (providerResult is null)
        {
            ConsoleRenderer.WriteError(
                "No LLM providers detected. Set at least one API key environment variable:");
            ConsoleRenderer.WriteError(
                "  OPENAI_API_KEY, ANTHROPIC_API_KEY, GOOGLE_API_KEY, GROQ_API_KEY,");
            ConsoleRenderer.WriteError(
                "  COHERE_API_KEY, MISTRAL_API_KEY, DEEPSEEK_API_KEY, XAI_API_KEY,");
            ConsoleRenderer.WriteError(
                "  PERPLEXITY_API_KEY, OPENROUTER_API_KEY, DEEPINFRA_API_KEY, VOYAGE_API_KEY");
            ConsoleRenderer.WriteError(
                "  ...or run a local Ollama server (set OLLAMA_HOST, default http://localhost:11434).");
            return 1;
        }

        // Apply saved model preference
        if (settings.ActiveModel is not null)
        {
            Chat.Models.ChatModel? savedModel = providerResult.AllModels
                .FirstOrDefault(m => m.Name == settings.ActiveModel);
            if (savedModel is not null)
                providerResult.ActiveModel = savedModel;
        }

        if (providerResult.ActiveModel.Provider == LLmProviders.Custom)
        {
            string ollamaHost = OllamaContextInspector.ResolveHost(Environment.GetEnvironmentVariable("OLLAMA_HOST"));
            int? runtimeContext = await OllamaContextInspector.TryGetRuntimeContextTokens(providerResult.ActiveModel.Name, ollamaHost);
            int? modelCardContext = runtimeContext is > 0
                ? null
                : await OllamaContextInspector.TryGetModelCardContextTokens(providerResult.ActiveModel.Name, ollamaHost);
            int? detectedContext = runtimeContext ?? modelCardContext;

            if (detectedContext is > 0)
            {
                providerResult.ActiveModel = new Chat.Models.ChatModel(
                    providerResult.ActiveModel.Name,
                    providerResult.ActiveModel.Provider,
                    detectedContext.Value);

                string sourceLabel = runtimeContext is > 0 ? "runtime" : "model metadata";
                ConsoleRenderer.WriteInfo(
                    $"Detected Ollama context ({sourceLabel}): {detectedContext:N0} tokens for {providerResult.ActiveModel.Name}");
            }
            else
            {
                ConsoleRenderer.WriteInfo(
                    "Could not detect Ollama context size for the active model; using default compression budget.");
            }
        }

        ConsoleRenderer.WriteProviderSummary(providerResult);

        // ─── Step 4: Skills ───
        SkillManager skillManager = new(settings, persistence);
        string skillsDir = SkillLoader.ResolveSkillsDirectory(settings.SkillsDirectory);
        string globalSkillsDir = SkillLoader.ResolveGlobalSkillsDirectory();

        // Seed the built-in skills (shipped alongside the binary) into the global user folder on first
        // run, so the built CLI never depends on source-tree skill files. Existing skills are preserved.
        SkillLoader.SeedBuiltInSkills(globalSkillsDir,
            name => ConsoleRenderer.WriteInfo($"Installed built-in skill '{name}' to {globalSkillsDir}"));

        skillManager.LoadSkills(skillsDir, globalSkillsDir, ConsoleRenderer.WriteWarning);
        ConsoleRenderer.WriteInfo(
            $"Skills: {skillManager.GetEnabledSkills().Count} enabled, " +
            $"{skillManager.GetAllSkills().Count} total (project: {skillsDir}, global: {globalSkillsDir})");

        // ─── Step 4b: Agent Discovery ───
        AgentDefinitionManager agentManager = new(settings, persistence);
        string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings.AgentsDirectory);
        string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string globalAgentsDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
        agentManager.LoadAll(builtInDir, globalAgentsDir, agentsDir, Environment.CurrentDirectory, ConsoleRenderer.WriteWarning);

        AgentDefinition? projectContext = agentManager.GetProjectContext();
        ConsoleRenderer.WriteInfo(
            $"Agents: {agentManager.GetAllPersonas().Count} personas available" +
            $"{(projectContext is not null ? ", project AGENTS.md detected" : "")}");

        // ─── Step 5: MCP ───
        _mcpLoader = new McpConfigLoader();
        McpSessionPolicy sessionPolicy = McpSessionPolicy.FromSettings(settings, Environment.CurrentDirectory);
        _mcpLoader.Configure(settings, sessionPolicy);

        string? localMcpConfigPath = McpConfigLoader.ResolveMcpConfigPath(settings.McpConfigPath);
        string? globalMcpConfigPath = McpConfigLoader.ResolveGlobalMcpConfigPath();

        if (localMcpConfigPath is null && globalMcpConfigPath is null)
        {
            ConsoleRenderer.WriteInfo("No mcp.json found. Loading built-in MCP servers only.");
        }
        else if (localMcpConfigPath is not null && globalMcpConfigPath is not null)
        {
            ConsoleRenderer.WriteInfo($"Loading MCP servers from local {localMcpConfigPath} and global {globalMcpConfigPath}...");
        }
        else if (localMcpConfigPath is not null)
        {
            ConsoleRenderer.WriteInfo($"Loading MCP servers from local {localMcpConfigPath}...");
        }
        else
        {
            ConsoleRenderer.WriteInfo($"Loading MCP servers from global {globalMcpConfigPath}...");
        }

        await _mcpLoader.LoadAsync(localMcpConfigPath, globalMcpConfigPath, ConsoleRenderer.WriteInfo);

        // ─── Step 6: Tool Approval ───
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);

        // ─── Step 7: Conversation Memory (SQLite) ───
        _conversationStore = new SqliteConversationStore(CliStorage.DatabasePath, CliStorage.AttachmentsDirectory);
        _agentStateStore = new SqliteAgentStateStore(CliStorage.DatabasePath);
        _memoryManager = new ConversationMemoryManager(
            providerResult.Api,
            providerResult.ActiveModel,
            providerResult.ActiveModel.ContextTokens,
            _conversationStore,
            compressionContextTokenCap: settings.CompressionContextTokenCap);

        if (_memoryManager.Messages.Count > 0)
        {
            ConsoleRenderer.WriteInfo(
                $"Resuming previous conversation ({_memoryManager.Messages.Count} messages).");
        }

        // Surface hard-budget trims so context loss is never silent.
        _memoryManager.ContextTrimmed += dropped =>
            ConsoleRenderer.WriteInfo($"[context trimmed: {dropped} message(s) dropped to fit the token budget]");

        // ─── Step 8: Build Agent ───
        _agentBuilder = new CliAgentBuilder(
            providerResult.Api,
            providerResult.ActiveModel,
            skillManager,
            _mcpLoader,
            toolApproval,
            _memoryManager,
            agentManager,
            settings,
            providerResult.OptimizerModel,
            _agentStateStore);

        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler = HandleRuntimeEvent;

        // Apply saved agent baseline (if any)
        if (agentManager.ActivePersonaName is not null)
        {
            agentManager.ApplyCapabilityBaseline(skillManager, toolApproval);
            ConsoleRenderer.WriteInfo(
                $"Restored agent: {agentManager.ActivePersonaName}");
        }

        global::LlmTornado.Agents.ChatRuntime.ChatRuntime runtime = _agentBuilder.Build(runtimeEventHandler);

        // ─── Step 9: Register Commands ───
        CommandDispatcher dispatcher = new();
        dispatcher.Register(new HelpCommand(dispatcher));
        dispatcher.Register(new ModelCommand(providerResult, _agentBuilder, runtimeEventHandler));
        dispatcher.Register(new SkillCommand(
            skillManager, _agentBuilder, settings, providerResult, toolApproval, runtimeEventHandler));
        dispatcher.Register(new AgentCommand(
            agentManager, skillManager, _agentBuilder,
            settings, providerResult, toolApproval, runtimeEventHandler));
        dispatcher.Register(new ConversationCommand(_memoryManager, _conversationStore, _agentBuilder));
        dispatcher.Register(new ContextCommand(_memoryManager, _conversationStore, CliStorage.ContextDumpsDirectory, settings, persistence));
        dispatcher.Register(new ToolsCommand(toolApproval, _agentBuilder, settings, providerResult));
        dispatcher.Register(new McpCommand(_mcpLoader, _agentBuilder, settings, runtimeEventHandler));
        dispatcher.Register(new CdCommand(_agentBuilder, agentManager, skillManager, _mcpLoader, settings, runtimeEventHandler));
        dispatcher.Register(new ThinkingCommand(settings, () => _showThinking, value => _showThinking = value));
        dispatcher.Register(new TimestampCommand(settings,
            () => MessageTimestampPrefixer.Enabled,
            value => MessageTimestampPrefixer.Enabled = value));
        dispatcher.Register(new ClearCommand());
        dispatcher.Register(new ExitCommand(_memoryManager, _conversationStore, _agentBuilder));

        // ─── Step 10: Banner ───
        string agentLabel = agentManager.ActivePersonaName is not null
            ? $" [{agentManager.ActivePersonaName}]" : "";
        ConsoleRenderer.WriteInfo($"\nActive model: {providerResult.ActiveModel.Name}{agentLabel}");
        ConsoleRenderer.WriteInfo("Type /help for commands. Start chatting!\n");

        // ─── Step 11: REPL Loop ───
        return await ReplLoop(runtime, dispatcher, _memoryManager, _agentBuilder, runtimeEventHandler);
    }

    private static async Task<int> ReplLoop(
        global::LlmTornado.Agents.ChatRuntime.ChatRuntime runtime,
        CommandDispatcher dispatcher,
        ConversationMemoryManager memory,
        CliAgentBuilder builder,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        // Live autocomplete: /commands from the dispatcher, @documents scanned from the
        // (dynamically read) working directory so /cd is reflected automatically.
        FileSuggestionProvider fileProvider = new();
        LineEditor editor = new(
            dispatcher.Commands,
            partial => fileProvider.Suggest(
                builder.WorkingDirectory ?? Directory.GetCurrentDirectory(), partial));

        while (true)
        {
            string? input = editor.ReadLine(builder.ActiveModel.Name);
            if (input is null) // EOF
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
                // Parse input for inline file references (@path/to/file)
                ParsedInput parsed = InputParser.Parse(input, builder.WorkingDirectory);
                FileAttachmentResult attachResult = FileAttachmentResolver.Resolve(parsed);

                // Report attached files
                foreach (ResolvedAttachment att in attachResult.Attachments)
                {
                    ConsoleRenderer.WriteInfo($"  [attached: {att.FileName} ({att.MimeType}, {att.FormattedSize})]");
                }

                // Report any file errors
                foreach (string err in attachResult.Errors)
                {
                    ConsoleRenderer.WriteError($"  {err}");
                }

                ChatMessage userMessage = attachResult.Message;
                int currentTokens = memory.GetMessagesForAgent().Sum(CompressionStrategy.EstimateTokens)
                                    + CompressionStrategy.EstimateTokens(userMessage);
                int contextUsedPercent = memory.EffectiveCompressionContextTokens > 0
                    ? (int)Math.Ceiling(currentTokens * 100.0 / memory.EffectiveCompressionContextTokens)
                    : 0;
                MessageTimestampPrefixer.Prefix(
                    userMessage,
                    "user",
                    contextUsedPercent: contextUsedPercent);

                // Optimize tools for this turn if needed
                bool optimized = false;
                if (builder.NeedsOptimization)
                {
                    try
                    {
                        ToolOptimizationResult? optResult = await builder.OptimizeToolsForTurn(input);
                        optimized = optResult?.WasOptimized == true;
                    }
                    catch (Exception ex)
                    {
                        ConsoleRenderer.WriteToolOptimizationSkipped(
                            builder.TotalToolCount, ex.Message);
                    }
                }

                try
                {
                    // The managed runtime config records the user message + full response (including tool
                    // messages) into memory, then runs compression/budget enforcement and persists — all
                    // inside InvokeAsync, so the next request reflects the compressed canonical set.
                    await runtime.InvokeAsync(userMessage);
                    ConsoleRenderer.EndStreamingResponse();
                }
                finally
                {
                    // Always restore full tool list after the turn
                    if (optimized)
                        builder.RestoreFullTools();
                }

                if (builder.ConversationConfig?.LastTurnCompressed == true)
                    ConsoleRenderer.WriteInfo("[context compressed]");
            }
            catch (OperationCanceledException)
            {
                ConsoleRenderer.EndStreamingResponse();
                ConsoleRenderer.WriteInfo("\n[cancelled]");
            }
            catch (Exception ex)
            {
                ConsoleRenderer.EndStreamingResponse();
                ConsoleRenderer.WriteError($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        // Cleanup
        if (_mcpLoader is not null)
            await _mcpLoader.DisposeAsync();

        _agentStateStore?.Dispose();
        _conversationStore?.Dispose();

        return 0;
    }

    private static ValueTask HandleRuntimeEvent(ChatRuntimeEvents evt)
    {
        if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
        {
            if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
            {
                if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta)
                {
                    ConsoleRenderer.WriteStreamingToken(delta.DeltaText);
                }
                else if (_showThinking && streamEvt.ModelStreamingEvent is ModelStreamingReasoningPartAddedEvent reasoning)
                {
                    ConsoleRenderer.WriteReasoningToken(reasoning.DeltaText);
                }
                else if (streamEvt.ModelStreamingEvent is ModelStreamingFunctionCallDeltaEvent functionDelta)
                {
                    ConsoleRenderer.WriteToolCallArgumentDelta(functionDelta.ToolName, functionDelta.ArgumentsDelta);
                }
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerToolInvokedEvent toolEvt)
            {
                ConsoleRenderer.WriteInfo($"  [calling tool: {toolEvt.ToolCalled.Name}]");
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerErrorEvent errorEvt)
            {
                ConsoleRenderer.WriteError($"\n[Agent error: {errorEvt.ErrorMessage}]");
            }
        }
        else if (evt is ChatRuntimeErrorEvent runtimeError)
        {
            ConsoleRenderer.WriteError($"\n[Runtime error: {runtimeError.Exception?.Message}]");
        }

        return ValueTask.CompletedTask;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        try
        {
            _agentBuilder?.Runtime.CancelExecution();
        }
        catch
        {
            // best effort
        }
    }
}
