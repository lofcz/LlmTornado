using System.Text;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.Tools;
using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Code;

namespace LlmTornado.Cli;

class Program
{
    private static McpConfigLoader? _mcpLoader;
    private static ConversationMemoryManager? _memoryManager;
    private static ConversationStore? _conversationStore;
    private static CliAgentBuilder? _agentBuilder;

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

        ConsoleRenderer.WriteProviderSummary(providerResult);

        // ─── Step 4: Skills ───
        SkillManager skillManager = new(settings, persistence);
        string skillsDir = SkillLoader.ResolveSkillsDirectory(settings.SkillsDirectory);
        string globalSkillsDir = SkillLoader.ResolveGlobalSkillsDirectory();
        skillManager.LoadSkills(skillsDir, globalSkillsDir);
        ConsoleRenderer.WriteInfo(
            $"Skills: {skillManager.GetEnabledSkills().Count} enabled, " +
            $"{skillManager.GetAllSkills().Count} total (project: {skillsDir}, global: {globalSkillsDir})");

        // ─── Step 4b: Agent Discovery ───
        AgentDefinitionManager agentManager = new(settings, persistence);
        string agentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(settings.AgentsDirectory);
        string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string globalAgentsDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
        agentManager.LoadAll(builtInDir, globalAgentsDir, agentsDir, Environment.CurrentDirectory);

        AgentDefinition? projectContext = agentManager.GetProjectContext();
        ConsoleRenderer.WriteInfo(
            $"Agents: {agentManager.GetAllPersonas().Count} personas available" +
            $"{(projectContext is not null ? ", project AGENTS.md detected" : "")}");

        // ─── Step 5: MCP ───
        _mcpLoader = new McpConfigLoader();
        string? mcpConfigPath = McpConfigLoader.ResolveMcpConfigPath(settings.McpConfigPath);
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
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);

        // ─── Step 7: Conversation Memory ───
        _memoryManager = new ConversationMemoryManager(
            providerResult.Api,
            providerResult.ActiveModel,
            providerResult.ActiveModel.ContextTokens,
            CliStorage.CurrentConversationPath);
        _conversationStore = new ConversationStore(CliStorage.ConversationsDirectory);

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
            _memoryManager,
            agentManager,
            settings,
            providerResult.OptimizerModel);

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
        dispatcher.Register(new SkillCommand(skillManager, _agentBuilder, runtimeEventHandler));
        dispatcher.Register(new AgentCommand(
            agentManager, skillManager, _agentBuilder,
            settings, runtimeEventHandler));
        dispatcher.Register(new ConversationCommand(_memoryManager, _conversationStore, _agentBuilder));
        dispatcher.Register(new ToolsCommand(toolApproval, _agentBuilder, settings, providerResult));
        dispatcher.Register(new McpCommand(_mcpLoader, _agentBuilder, settings, runtimeEventHandler));
        dispatcher.Register(new CdCommand(_agentBuilder, agentManager, runtimeEventHandler));
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
        while (true)
        {
            ConsoleRenderer.WritePrompt(builder.ActiveModel.Name);

            string? input = Console.ReadLine();
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
                ChatMessage userMessage = new(ChatMessageRoles.User, input);
                memory.AddMessage(userMessage);

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
                    ChatMessage response = await runtime.InvokeAsync(userMessage);
                    ConsoleRenderer.EndStreamingResponse();

                    memory.AddMessage(response);
                }
                finally
                {
                    // Always restore full tool list after the turn
                    if (optimized)
                        builder.RestoreFullTools();
                }

                bool summarized = await memory.MaybeSummarize();
                if (summarized)
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
