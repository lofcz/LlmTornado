using System.Text;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
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
using LlmTornado.Cli.Core.Telemetry;
using LlmTornado.Cli.Rendering;
using LlmTornado.Code;

namespace LlmTornado.Cli;

class Program
{
    private static McpConfigLoader? _mcpLoader;
    private static ConversationMemoryManager? _memoryManager;
    private static SqliteConversationStore? _conversationStore;
    private static SqliteAgentStateStore? _agentStateStore;
    private static CliAgentBuilder? _agentBuilder;
    private static AutoLoopController? _autoLoopController;
    private static bool _showThinking = true;
    private static readonly SessionTelemetry _sessionTelemetry = new();

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        AnsiSupport.TryEnableVirtualTerminal();
        RenderContext.Initialize(AnsiSupport.Detect());
        ConsoleRenderer.InitializeRendering(
            RenderContext.Capabilities,
            toolName => _mcpLoader is not null && _mcpLoader.ToolServerMap.TryGetValue(toolName, out string? server)
                ? server
                : null);

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
        List<OpenAiCompatEndpoint> openAiCompatEndpoints = OpenAiCompatEndpoint.Merge(
            settings.OpenAiCompatEndpoints,
            OpenAiCompatEndpoint.ParseEnv(Environment.GetEnvironmentVariable("TORNADO_OPENAI_COMPAT")));

        ProviderDetectionResult? providerResult = ProviderDetector.Detect(
            openAiCompatEndpoints,
            warning => ConsoleRenderer.WriteWarning(warning));
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
                "  ...or run a local Ollama server (set OLLAMA_HOST, default http://localhost:11434)");
            ConsoleRenderer.WriteError(
                "  ...or configure an OpenAI-compatible endpoint via /endpoint add or TORNADO_OPENAI_COMPAT.");
            return 1;
        }

        // Apply saved model preference (supports bare name or endpoint/model)
        if (settings.ActiveModel is not null)
        {
            Chat.Models.ChatModel? savedModel = providerResult.ResolveModel(settings.ActiveModel, out _);
            if (savedModel is not null)
                providerResult.ActiveModel = savedModel;
        }

        DetectedProvider? activeOwner = providerResult.FindOwner(providerResult.ActiveModel);
        if (providerResult.ActiveModel.Provider == LLmProviders.Custom)
        {
            if (activeOwner?.EndpointName is null or "ollama")
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
                else if (providerResult.ActiveModel.ContextTokens is null)
                {
                    int fallback = OpenAiCompatProber.ResolveContextTokens(
                        null, activeOwner?.DefaultContextTokens, settings.CompressionContextTokenCap);
                    providerResult.ActiveModel = new Chat.Models.ChatModel(
                        providerResult.ActiveModel.Name,
                        providerResult.ActiveModel.Provider,
                        fallback);
                    ConsoleRenderer.WriteInfo(
                        $"Could not detect Ollama context size; assuming {fallback:N0} tokens.");
                }
            }
            else if (providerResult.ActiveModel.ContextTokens is null)
            {
                int resolved = OpenAiCompatProber.ResolveContextTokens(
                    null, activeOwner?.DefaultContextTokens, settings.CompressionContextTokenCap);
                providerResult.ActiveModel = new Chat.Models.ChatModel(
                    providerResult.ActiveModel.Name,
                    providerResult.ActiveModel.Provider,
                    resolved);
                ConsoleRenderer.WriteInfo(
                    $"Context window unknown for [{activeOwner?.EndpointName}] {providerResult.ActiveModel.Name}; assuming {resolved:N0} tokens.");
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
        TornadoApi activeApi = providerResult.GetApiForModel(providerResult.ActiveModel);
        _memoryManager = new ConversationMemoryManager(
            activeApi,
            providerResult.ActiveModel,
            providerResult.ActiveModel.ContextTokens,
            _conversationStore,
            compressionContextTokenCap: settings.CompressionContextTokenCap);
        _memoryManager.ConfigureCompressionThresholds(
            settings.CompressionTriggerUtilization,
            settings.CompressionTargetUtilization);

        if (_memoryManager.Messages.Count > 0)
        {
            ConsoleRenderer.WriteInfo(
                $"Resuming previous conversation ({_memoryManager.Messages.Count} messages).");
        }

        // Surface hard-budget trims so context loss is never silent.
        _memoryManager.ContextTrimmed += dropped =>
            ConsoleRenderer.WriteInfo($"[context trimmed: {dropped} message(s) dropped to fit the token budget]");

        // ─── Step 8: Build Agent ───
        // Local models: keep tool schemas stable across turns so the server-side prompt cache
        // survives — an optimizer-swapped tool set changes the prompt prefix every reload.
        // Session-only decision (no settings mutation); /tools optimize on re-enables it.
        ChatModel? optimizerModel = providerResult.OptimizerModel;
        if (providerResult.ActiveModel.Provider == LLmProviders.Custom && optimizerModel is not null && settings.ToolOptimizerEnabled)
        {
            optimizerModel = null;
            ConsoleRenderer.WriteInfo(
                "[tool optimizer off for local model — stable tool schemas keep the prompt cache warm; /tools optimize on to override]");
        }

        _agentBuilder = new CliAgentBuilder(
            activeApi,
            providerResult.ActiveModel,
            skillManager,
            _mcpLoader,
            toolApproval,
            _memoryManager,
            agentManager,
            settings,
            optimizerModel,
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
        _autoLoopController = new AutoLoopController();
        dispatcher.Register(new HelpCommand(dispatcher));
        dispatcher.Register(new ModelCommand(providerResult, _agentBuilder, runtimeEventHandler, settings));
        dispatcher.Register(new EndpointCommand(settings, providerResult, _agentBuilder, runtimeEventHandler));
        dispatcher.Register(new SkillCommand(
            skillManager, _agentBuilder, settings, providerResult, toolApproval, runtimeEventHandler));
        dispatcher.Register(new AgentCommand(
            agentManager, skillManager, _agentBuilder,
            settings, providerResult, toolApproval, runtimeEventHandler));
        dispatcher.Register(new ConversationCommand(_memoryManager, _conversationStore, _agentBuilder));
        dispatcher.Register(new ContextCommand(_memoryManager, _conversationStore, CliStorage.ContextDumpsDirectory, settings, persistence));
        dispatcher.Register(new ToolsCommand(toolApproval, _agentBuilder, settings, providerResult));
        dispatcher.Register(new MaxToolsCommand(
            settings,
            maxTools => _agentBuilder.SetMaxTools(maxTools, providerResult.OptimizerModel),
            () => _agentBuilder.TotalToolCount,
            () => _agentBuilder.NeedsOptimization));
        dispatcher.Register(new McpCommand(_mcpLoader, _agentBuilder, settings, runtimeEventHandler));
        dispatcher.Register(new CdCommand(_agentBuilder, agentManager, skillManager, _mcpLoader, settings, runtimeEventHandler));
        dispatcher.Register(new ThinkingCommand(settings, () => _showThinking, value => _showThinking = value));
        dispatcher.Register(new ReasoningCommand(settings, effort => _agentBuilder!.SetReasoningEffort(effort)));
        dispatcher.Register(new TimestampCommand(settings,
            () => MessageTimestampPrefixer.Enabled,
            value => MessageTimestampPrefixer.Enabled = value));
        dispatcher.Register(new AutoLoopCommand(
            _autoLoopController,
            async (statement, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RunChatTurnAsync(statement, _agentBuilder!.Runtime, _memoryManager!, _agentBuilder!);
                cancellationToken.ThrowIfCancellationRequested();
            }));
        dispatcher.Register(new ClearCommand());
        dispatcher.Register(new MarkdownDemoCommand());
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
                builder.WorkingDirectory ?? Directory.GetCurrentDirectory(), partial),
            historySeed: PersistentInputHistory.Load(CliStorage.InputHistoryPath),
            historySink: entry => PersistentInputHistory.Append(CliStorage.InputHistoryPath, entry));

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
                await RunChatTurnAsync(input, runtime, memory, builder);
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

    private static async Task RunChatTurnAsync(
        string input,
        global::LlmTornado.Agents.ChatRuntime.ChatRuntime runtime,
        ConversationMemoryManager memory,
        CliAgentBuilder builder)
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

        // Prefer the real context size (last request's prompt + its completion, both now history)
        // over the chars/4 estimate; fall back to the estimate until real usage arrives.
        int newMessageTokens = CompressionStrategy.EstimateTokens(userMessage);
        int currentTokens = _sessionTelemetry.EstimatedNextPromptTokens is int realTokens
            ? realTokens + newMessageTokens
            : memory.GetMessagesForAgent().Sum(CompressionStrategy.EstimateTokens) + newMessageTokens;
        int contextUsedPercent = memory.EffectiveCompressionContextTokens > 0
            ? (int)Math.Ceiling(currentTokens * 100.0 / memory.EffectiveCompressionContextTokens)
            : 0;
        MessageTimestampPrefixer.Prefix(
            userMessage,
            "user",
            contextUsedPercent: contextUsedPercent);

        // Optimize tools once for this built agent/tool catalog if needed. The selected set stays
        // active across turns; the model can call select_tools later if it needs a different set.
        if (builder.NeedsOptimization)
        {
            try
            {
                await builder.OptimizeToolsForTurn(input);
            }
            catch (Exception ex)
            {
                ConsoleRenderer.WriteToolOptimizationSkipped(
                    builder.TotalToolCount, ex.Message);
            }
        }

        _sessionTelemetry.BeginTurn();

        // The managed runtime config records the user message and full response into memory, then
        // runs compression/budget enforcement and persists inside InvokeAsync. Esc interrupts the
        // turn (mid-stream) while keeping the session and any partial output.
        await TurnInterruptWatcher.WatchAsync(
            runtime.InvokeAsync(userMessage),
            () => runtime.CancelExecution());
        ConsoleRenderer.EndStreamingResponse();
        if (builder.ConversationConfig?.LastTurnCompressed == true)
        {
            _sessionTelemetry.InvalidateUsage();
            ConsoleRenderer.WriteInfo("[context compressed]");
        }

        WriteTurnStatusLine(memory);
    }

    /// <summary>
    /// One dim line after each turn: context utilization (real when the provider reports usage,
    /// ~estimated otherwise) and this turn's output/reasoning tokens.
    /// </summary>
    private static void WriteTurnStatusLine(ConversationMemoryManager memory)
    {
        int window = memory.EffectiveCompressionContextTokens;
        if (window <= 0)
            return;

        string marker;
        int contextTokens;
        if (_sessionTelemetry.EstimatedNextPromptTokens is int real)
        {
            marker = "";
            contextTokens = real;
        }
        else
        {
            marker = "~";
            contextTokens = memory.GetMessagesForAgent().Sum(CompressionStrategy.EstimateTokens);
        }

        int percent = (int)Math.Ceiling(contextTokens * 100.0 / window);
        string status = $"ctx {marker}{percent}% ({contextTokens:N0}/{window:N0})";
        if (_sessionTelemetry.TurnCompletionTokens > 0)
            status += $" · out {_sessionTelemetry.TurnCompletionTokens:N0}";
        if (_sessionTelemetry.TurnReasoningTokens > 0)
            status += $" · reasoning {_sessionTelemetry.TurnReasoningTokens:N0}";

        ConsoleRenderer.WriteDimStatus(status);
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
                ConsoleRenderer.OnToolInvoked(toolEvt.ToolCalled);
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerToolCompletedEvent toolCompletedEvt)
            {
                ConsoleRenderer.OnToolCompleted(toolCompletedEvt.ToolCall, toolCompletedEvt.ToolResult);
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerRequestPreparedEvent requestPreparedEvt)
            {
                _sessionTelemetry.OnRequestPrepared(requestPreparedEvt.Tokens);
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerUsageReceivedEvent usageEvt)
            {
                _sessionTelemetry.OnUsageReceived(usageEvt.Usage);
                _memoryManager?.ReportActualUsage(usageEvt.Usage.PromptTokens, usageEvt.Usage.CompletionTokens);
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerCancelledEvent)
            {
                ConsoleRenderer.EndStreamingResponse();
                ConsoleRenderer.WriteInfo("[interrupted — partial response kept]");
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerMaxTurnsReachedEvent)
            {
                ConsoleRenderer.WriteWarning("[agent stopped: maximum turns reached]");
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerMaxTokensReachedEvent)
            {
                ConsoleRenderer.WriteWarning("[agent stopped: context token limit reached — /context compress can free space]");
            }
            else if (runnerEvt.AgentRunnerEvent is AgentRunnerErrorEvent errorEvt)
            {
                // A user interrupt surfaces as an OCE inside the runner before the Cancelled
                // event fires; don't paint it as a scary error.
                if (errorEvt.Exception is not OperationCanceledException)
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
        _autoLoopController?.Stop();
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
