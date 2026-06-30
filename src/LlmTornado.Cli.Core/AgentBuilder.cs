using System.Text;
using System.Text.Json;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Interactions;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.State;
using LlmTornado.Cli.Core.Tools;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Cli.Core;

/// <summary>
/// Assembles a TornadoAgent and ChatRuntime from all system components.
/// Shared between CLI and ACP Server.
/// </summary>
public sealed class AgentBuilder
{
    private readonly TornadoApi _api;
    private readonly SkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly IToolApproval _toolApproval;
    private readonly IUserInteractionHandler? _userInteraction;
    private readonly AgentDefinitionManager _agentManager;
    private readonly AgentSettings _settings;
    private readonly List<Tool>? _additionalTools;
    private readonly Memory.ConversationMemoryManager? _memoryManager;
    private readonly IAgentStateStore? _agentStateStore;

    private ChatModel _activeModel;
    private TornadoAgent? _agent;
    private ChatRuntime? _runtime;
    private ToolOptimizer? _toolOptimizer;
    private List<Tool>? _fullToolList;

    public TornadoAgent Agent => _agent ?? throw new InvalidOperationException("Agent not built");
    public ChatRuntime Runtime => _runtime ?? throw new InvalidOperationException("Runtime not built");
    public ChatModel ActiveModel => _activeModel;

    /// <summary>
    /// The managed conversation config when a memory manager is in use (CLI). Null otherwise (e.g. Blazor),
    /// in which case a plain <see cref="SingletonRuntimeConfiguration"/> is used.
    /// </summary>
    public Memory.ManagedConversationRuntimeConfiguration? ConversationConfig =>
        _runtime?.RuntimeConfiguration as Memory.ManagedConversationRuntimeConfiguration;

    /// <summary>
    /// Whether the tool optimizer is active (tool count exceeds threshold and optimizer is configured).
    /// </summary>
    public bool NeedsOptimization => _toolOptimizer is not null && _fullToolList is not null
                                     && _fullToolList.Count > _settings.MaxTools;

    /// <summary>
    /// Total number of tools before any optimization.
    /// </summary>
    public int TotalToolCount => _fullToolList?.Count ?? 0;

    /// <summary>
    /// The full set of tools registered on the agent, before any per-turn optimization.
    /// Use this (rather than <see cref="Agent"/>.ToolList) to enumerate every available tool,
    /// since the agent's live list may hold an optimized subset during a turn.
    /// </summary>
    public IReadOnlyList<Tool> FullToolList => _fullToolList ?? [];

    /// <summary>
    /// Optional override for the current working directory in the system prompt.
    /// If null, uses Environment.CurrentDirectory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    public AgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        SkillManager skillManager,
        McpConfigLoader mcpLoader,
        IToolApproval toolApproval,
        IUserInteractionHandler? userInteraction,
        AgentDefinitionManager agentManager,
        AgentSettings settings,
        ChatModel? optimizerModel,
        List<Tool>? additionalTools = null,
        Memory.ConversationMemoryManager? memoryManager = null,
        IAgentStateStore? agentStateStore = null)
    {
        _api = api;
        _activeModel = activeModel;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _toolApproval = toolApproval;
        _userInteraction = userInteraction;
        _agentManager = agentManager;
        _settings = settings;
        _additionalTools = additionalTools;
        _memoryManager = memoryManager;
        _agentStateStore = agentStateStore;

        if (settings.ToolOptimizerEnabled && optimizerModel is not null)
        {
            _toolOptimizer = new ToolOptimizer(api, optimizerModel, settings.MaxTools);
        }
    }

    /// <summary>
    /// Build or rebuild the agent and runtime.
    /// </summary>
    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        string systemPrompt = BuildSystemPrompt();
        List<Tool> allTools = CollectTools();

        // Store the full tool list for optimization swap/restore
        _fullToolList = allTools;

        _agent = new TornadoAgent(
            client: _api,
            model: _activeModel,
            name: "Agent",
            instructions: systemPrompt,
            streaming: true);

        // Apply reasoning effort if configured
        ApplyReasoningEffort(_agent);

        // Add tools
        foreach (Tool tool in allTools)
        {
            _agent.AddTool(tool);
        }

        // Configure tool permissions (all tools require approval).
        // Use ResolvedName which covers MCP tools (Function.Name), delegate tools (ToolName),
        // and any other tool variant. This ensures ToolPermissionRequired has entries for
        // every tool the LLM might call, preventing KeyNotFoundException at runtime.
        foreach (Tool tool in allTools)
        {
            string resolvedName = tool.ResolvedName;
            if (!string.IsNullOrEmpty(resolvedName))
                _agent.ToolPermissionRequired[resolvedName] = true;
        }

        // Pre-approve skill allowed-tools
        foreach (Skill skill in _skillManager.GetEnabledSkills())
        {
            if (skill.AllowedTools.Count > 0)
                _toolApproval.PreApproveSkillTools(skill.AllowedTools);
        }

        // Create runtime. When a memory manager is supplied (CLI), use the managed config so the
        // conversation lifecycle (sync → compress → budget → persist) is owned in one place; otherwise
        // fall back to the plain singleton config.
        SingletonRuntimeConfiguration runtimeConfig = _memoryManager is not null
            ? new Memory.ManagedConversationRuntimeConfiguration(_agent, _memoryManager)
            : new SingletonRuntimeConfiguration(_agent);
        runtimeConfig.OnRuntimeEvent = onRuntimeEvent;
        runtimeConfig.OnRuntimeRequestEvent = _toolApproval.HandleToolPermissionRequest;

        _runtime = new ChatRuntime(runtimeConfig);
        return _runtime;
    }

    public ChatRuntime SetModel(ChatModel model, Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        _activeModel = model;
        return Build(onRuntimeEvent);
    }

    public ChatRuntime RebuildForSkillChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return Build(onRuntimeEvent);
    }

    /// <summary>
    /// Rebuild after agent persona switch.
    /// Applies capability baseline before rebuilding.
    /// </summary>
    public ChatRuntime RebuildForAgentChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        _agentManager.ApplyCapabilityBaseline(_skillManager, _toolApproval);
        return Build(onRuntimeEvent);
    }

    /// <summary>
    /// Run the LLM-based tool optimizer for the current user turn.
    /// Swaps agent.Options.Tools with the optimized subset.
    /// Call <see cref="RestoreFullTools"/> after the turn completes.
    /// </summary>
    public async Task<ToolOptimizationResult?> OptimizeToolsForTurn(string userMessage, CancellationToken ct = default)
    {
        if (_toolOptimizer is null || _agent is null || _fullToolList is null)
            return null;

        if (_fullToolList.Count <= _settings.MaxTools)
            return null;

        ToolOptimizationResult result = await _toolOptimizer.OptimizeAsync(_fullToolList, userMessage, ct);

        if (result.WasOptimized)
            ApplyToolSet(result.Tools);

        return result;
    }

    /// <summary>
    /// Restore the full tool list after an optimized turn completes.
    /// </summary>
    public void RestoreFullTools()
    {
        if (_fullToolList is not null)
            ApplyToolSet(_fullToolList);
    }

    /// <summary>
    /// Swap the agent's live tool list to <paramref name="tools"/> and (re)register their permissions.
    /// <see cref="TornadoAgent.ClearTools"/> does not clear <c>ToolPermissionRequired</c>, but we still
    /// re-populate it so every tool (including MCP tools, which skip default permission registration
    /// because their ToolName is null) has an entry and never throws at call time.
    /// </summary>
    private void ApplyToolSet(IReadOnlyList<Tool> tools)
    {
        if (_agent is null)
            return;

        _agent.ClearTools();
        foreach (Tool tool in tools)
            _agent.AddTool(tool);

        foreach (Tool tool in tools)
        {
            string resolvedName = tool.ResolvedName;
            if (!string.IsNullOrEmpty(resolvedName))
                _agent.ToolPermissionRequired[resolvedName] = true;
        }
    }

    /// <summary>
    /// Describe the full tool catalog (every tool registered before per-turn optimization), marking
    /// which are currently loaded. Backs the <c>list_all_tools</c> built-in tool so the agent can
    /// discover capabilities that optimization may have left out of the current turn.
    /// </summary>
    public string DescribeAllTools()
    {
        if (_fullToolList is null || _fullToolList.Count == 0)
            return "No tools are registered.";

        HashSet<string> loaded = new(
            _agent?.ToolList.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        StringBuilder sb = new();
        sb.AppendLine($"All {_fullToolList.Count} tools (✓ = loaded this turn, otherwise call select_tools to load it):");
        foreach (Tool tool in _fullToolList.OrderBy(t => t.ResolvedName, StringComparer.OrdinalIgnoreCase))
        {
            string name = tool.ResolvedName;
            if (string.IsNullOrEmpty(name))
                continue;
            string mark = loaded.Contains(name) ? "✓" : " ";
            string desc = tool.ResolvedDescription ?? string.Empty;
            sb.AppendLine(string.IsNullOrEmpty(desc) ? $"  {mark} {name}" : $"  {mark} {name}: {desc}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Run the tool selector with an agent-authored <paramref name="query"/> and load the matching
    /// tools into the live tool set. Backs the <c>select_tools</c> built-in tool. When optimization is
    /// inactive (no optimizer, or the catalog already fits the budget) all tools are simply (re)loaded.
    /// </summary>
    public async Task<string> SelectToolsForQueryAsync(string query, CancellationToken ct = default)
    {
        if (_agent is null || _fullToolList is null)
            return "No tools are available.";
        if (string.IsNullOrWhiteSpace(query))
            return "ERROR: provide a short query describing the capability you need.";

        if (_toolOptimizer is null || _fullToolList.Count <= _settings.MaxTools)
        {
            ApplyToolSet(_fullToolList);
            return $"All {_fullToolList.Count} tools are already available — nothing to narrow.";
        }

        ToolOptimizationResult result = await _toolOptimizer.OptimizeAsync(_fullToolList, query, ct);
        ApplyToolSet(result.Tools);

        IEnumerable<string> names = result.Tools
            .Select(t => t.ResolvedName)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        return $"Loaded {result.Tools.Count} tool(s) for query \"{query}\":\n"
               + string.Join("\n", names.Select(n => $"  - {n}"))
               + "\nCall list_all_tools to see everything, or select_tools again with a different query.";
    }

    /// <summary>
    /// Enable or disable the tool optimizer at runtime.
    /// </summary>
    public void SetOptimizerEnabled(bool enabled, ChatModel? optimizerModel = null)
    {
        _settings.ToolOptimizerEnabled = enabled;

        if (enabled && optimizerModel is not null)
        {
            _toolOptimizer = new ToolOptimizer(_api, optimizerModel, _settings.MaxTools);
        }
        else if (!enabled)
        {
            _toolOptimizer = null;
        }
    }

    /// <summary>
    /// Update the max tools threshold at runtime.
    /// </summary>
    public void SetMaxTools(int maxTools, ChatModel? optimizerModel = null)
    {
        _settings.MaxTools = maxTools;

        if (_settings.ToolOptimizerEnabled && optimizerModel is not null)
        {
            _toolOptimizer = new ToolOptimizer(_api, optimizerModel, maxTools);
        }
    }

    /// <summary>
    /// Parse and apply the reasoning effort from settings to the agent's request options.
    /// </summary>
    private void ApplyReasoningEffort(TornadoAgent agent)
    {
        if (string.IsNullOrWhiteSpace(_settings.ReasoningEffort))
        {
            agent.Options.ReasoningEffort = null;
            return;
        }

        ChatReasoningEfforts? parsed = _settings.ReasoningEffort.ToLowerInvariant() switch
        {
            "none" => ChatReasoningEfforts.None,
            "minimal" => ChatReasoningEfforts.Minimal,
            "low" => ChatReasoningEfforts.Low,
            "medium" => ChatReasoningEfforts.Medium,
            "high" => ChatReasoningEfforts.High,
            "xhigh" => ChatReasoningEfforts.XHigh,
            "max" => ChatReasoningEfforts.Max,
            "default" => ChatReasoningEfforts.Default,
            _ => null
        };

        agent.Options.ReasoningEffort = parsed;
    }

    private string BuildSystemPrompt()
    {
        StringBuilder sb = new();

        // Layer 1: Agent instructions (persona + project context)
        string agentInstructions = _agentManager.BuildInstructionsBlock();
        if (!string.IsNullOrEmpty(agentInstructions))
        {
            sb.Append(agentInstructions);
        }
        else
        {
            sb.AppendLine("You are a helpful assistant with access to skills and tools.");
            sb.AppendLine("You can activate skills to gain specialized knowledge and capabilities.");
            sb.AppendLine();
        }

        // Layer 2: Skills catalog
        List<Skill> enabledSkills = _skillManager.GetEnabledSkills();
        if (enabledSkills.Count > 0)
        {
            sb.AppendLine(_skillManager.BuildSkillsContextXml());
            sb.AppendLine();
            sb.AppendLine("When a user's task matches a skill, use the `load_skill` tool to activate it.");
            sb.AppendLine("The skill's full instructions will be returned and you should follow them.");
            sb.AppendLine();
            sb.AppendLine("When you need structured clarification from the user, use the `ask_question` tool.");
            sb.AppendLine("It supports asking multiple follow-up questions in one tool call.");
            sb.AppendLine();
        }

        sb.AppendLine("You have a built-in `web_search` tool for internet searches.");
        sb.AppendLine("Use it when current or external information is needed, and cite source URLs from the results.");
        sb.AppendLine();

        if (_toolOptimizer is not null)
        {
            sb.AppendLine("To save context, only a subset of tools may be loaded on any given turn.");
            sb.AppendLine("Call `list_all_tools` to see the full catalog, and `select_tools` with a short query");
            sb.AppendLine("describing what you need (e.g. \"read and edit files\") to load the relevant tools before calling them.");
            sb.AppendLine();
        }

        if (_agentStateStore is not null)
        {
            sb.AppendLine("You have built-in CLI-local `memory`, `state`, and `state_snapshot` tools (each takes an `action`).");
            sb.AppendLine("Use `memory` for durable notes, user/project preferences, and facts worth retaining:");
            sb.AppendLine("action=recall for semantic/hybrid recall (keep max_tokens small), action=search for exact keyword lookup, action=store to save.");
            sb.AppendLine("Use `state` for current task state and `state_snapshot` for checkpoints and restoring past state.");
            sb.AppendLine();
        }

        string cwd = WorkingDirectory ?? Environment.CurrentDirectory;
        sb.AppendLine($"The user's current working directory is: {cwd}");
        return sb.ToString();
    }

    private List<Tool> CollectTools()
    {
        List<Tool> tools = [];

        // Built-in skill management tools
        tools.Add(BuildLoadSkillTool());
        tools.Add(BuildListSkillsTool());
        tools.Add(BuildReadReferenceTool());
        if (_userInteraction is not null)
            tools.Add(BuildAskQuestionTool());
        tools.Add(BuiltInWebSearchTool.Build());

        // Built-in tool-discovery tools (so the agent can see and pull in tools that per-turn
        // optimization left out of the current turn).
        tools.Add(BuildListAllToolsTool());
        tools.Add(BuildSelectToolsTool());

        if (_agentStateStore is not null)
            tools.AddRange(BuildAgentStateTools());

        // Script tools from enabled skills (gated by approval system)
        List<Skill> enabledSkills = _skillManager.GetEnabledSkills();
        tools.AddRange(ScriptToolBuilder.BuildScriptTools(enabledSkills, _toolApproval));

        // MCP tools
        tools.AddRange(_mcpLoader.AllTools);

        // Additional tools (e.g. filesystem tools from ACP server)
        if (_additionalTools is not null)
            tools.AddRange(_additionalTools);

        // Filter by active agent persona's tool curation
        return tools.Where(t =>
        {
            string? name = t.Function?.Name;
            return name is null || _agentManager.IsToolAllowed(name);
        }).ToList();
    }

    private Tool BuildLoadSkillTool()
    {
        return new Tool(
            new Func<string, string>(skillName =>
            {
                string? instructions = _skillManager.ActivateSkill(skillName);
                return instructions ?? $"Skill '{skillName}' not found or not enabled. Use list_skills to see available skills.";
            }),
            "load_skill",
            "Load and activate a skill by name. Returns the skill's full instructions. " +
            "Use this when a user's task matches a skill from the available_skills list.");
    }

    private Tool BuildListSkillsTool()
    {
        return new Tool(
            new Func<string>(() =>
            {
                List<Skill> skills = _skillManager.GetEnabledSkills();
                if (skills.Count == 0)
                    return "No skills are currently enabled.";

                StringBuilder sb = new();
                sb.AppendLine("Available skills:");
                foreach (Skill skill in skills)
                {
                    string status = skill.Activated ? " [active]" : "";
                    sb.AppendLine($"  - {skill.Name}: {skill.Description}{status}");
                    if (skill.Scripts.Count > 0)
                        sb.AppendLine($"    Scripts: {string.Join(", ", skill.Scripts.Select(s => s.FileName))}");
                }
                return sb.ToString();
            }),
            "list_skills",
            "List all enabled skills with their descriptions and available scripts.");
    }

    private Tool BuildListAllToolsTool()
    {
        return new Tool(
            new Func<string>(DescribeAllTools),
            "list_all_tools",
            "List the full catalog of available tools (name + description), including tools not currently " +
            "loaded because per-turn tool optimization narrowed the set. Use this to discover what exists, " +
            "then call select_tools to load the ones you need.");
    }

    private Tool BuildSelectToolsTool()
    {
        return new Tool(
            new Func<string, Task<string>>(query => SelectToolsForQueryAsync(query, CancellationToken.None)),
            "select_tools",
            "Load the tools most relevant to a query you write. When tool optimization is active only a " +
            "subset of tools is loaded each turn; if you need a capability that isn't loaded, call this with " +
            "a short description of what you need (e.g. \"read and edit files\", \"search the web\", " +
            "\"query the database\"). The matching tools are loaded so you can call them. This replaces the " +
            "current optimized set, so include everything you need for the next steps in one query.");
    }

    private Tool BuildReadReferenceTool()
    {
        return new Tool(
            new Func<string, string, string>((skillName, relativePath) =>
            {
                Skill? skill = _skillManager.GetSkill(skillName);
                if (skill is null)
                    return $"Skill '{skillName}' not found.";

                string fullPath = Path.GetFullPath(Path.Combine(skill.DirectoryPath, relativePath));
                if (!fullPath.StartsWith(skill.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                    return "Access denied: path is outside the skill directory.";

                if (!File.Exists(fullPath))
                    return $"File not found: {relativePath}";

                string content = File.ReadAllText(fullPath);
                if (content.Length > 30_000)
                    content = content[..30_000] + "\n[TRUNCATED]";

                return content;
            }),
            "read_reference",
            "Read a reference file from a skill's directory. Provide the skill name and relative path.");
    }

    private Tool BuildAskQuestionTool()
    {
        return new Tool(
            new Func<AskQuestionToolRequest, Task<string>>(request => AskQuestionAsync(request, CancellationToken.None)),
            "ask_question",
            "Ask the user one or more follow-up questions. Supports single choice, multi-select, free text, yes/no, numeric input, and optional custom answers.");
    }

    private List<Tool> BuildAgentStateTools()
    {
        if (_agentStateStore is null)
            return [];

        return
        [
            new Tool(new Func<MemoryToolRequest, string>(Memory), "memory",
                "Durable CLI-local memory of notes (user preferences, project facts, important decisions). " +
                "Set `action` to one of: " +
                "store (requires content; optional key, tags), " +
                "recall (semantic/hybrid recall; requires query; optional tag, limit, max_tokens — keep small), " +
                "search (exact keyword/tag lookup; optional query, tag, limit), " +
                "list (optional tag, limit), " +
                "get (requires id), " +
                "delete (requires id), " +
                "reindex (rebuild local vectors)."),

            new Tool(new Func<StateToolRequest, string>(State), "state",
                "CLI-local key/value task state. Set `action` to one of: " +
                "set (requires key; optional value, content_type), " +
                "get (requires key), " +
                "list (optional prefix, limit), " +
                "delete (requires key)."),

            new Tool(new Func<StateSnapshotToolRequest, string>(StateSnapshot), "state_snapshot",
                "Point-in-time snapshots of all CLI-local state. Set `action` to one of: " +
                "create (optional label), " +
                "list (optional limit), " +
                "get (requires id), " +
                "restore (requires id).")
        ];
    }

    private string Memory(MemoryToolRequest request)
    {
        if (_agentStateStore is null)
            return "Memory tools are not available.";

        switch (NormalizeAction(request.Action))
        {
            case "store":
                if (string.IsNullOrWhiteSpace(request.Content))
                    return "ERROR: 'content' is required for action 'store'.";
                return SerializeToolResult(_agentStateStore.StoreMemory(
                    request.Key, request.Content, request.Tags ?? [], _memoryManager?.ConversationId));

            case "recall":
                if (string.IsNullOrWhiteSpace(request.Query))
                    return "ERROR: 'query' is required for action 'recall'.";
                return SerializeToolResult(_agentStateStore.RecallMemories(
                    request.Query, request.Tag, request.Limit ?? 8, request.MaxTokens ?? 1_500));

            case "search":
                return SerializeToolResult(_agentStateStore.SearchMemories(
                    request.Query, request.Tag, request.Limit ?? 20));

            case "list":
                return SerializeToolResult(_agentStateStore.SearchMemories(
                    null, request.Tag, request.Limit ?? 20));

            case "get":
                if (request.Id is null)
                    return "ERROR: 'id' is required for action 'get'.";
                AgentMemoryRecord? record = _agentStateStore.GetMemory(request.Id.Value);
                return record is null ? $"Memory '{request.Id}' not found." : SerializeToolResult(record);

            case "delete":
                if (request.Id is null)
                    return "ERROR: 'id' is required for action 'delete'.";
                return SerializeToolResult(new { deleted = _agentStateStore.DeleteMemory(request.Id.Value), id = request.Id.Value });

            case "reindex":
                return SerializeToolResult(new
                {
                    reindexed = _agentStateStore.ReindexMemoryVectors(),
                    provider = LocalMemoryVectorizer.Provider
                });

            default:
                return "ERROR: unknown action. Use store, recall, search, list, get, delete, or reindex.";
        }
    }

    private string State(StateToolRequest request)
    {
        if (_agentStateStore is null)
            return "State tools are not available.";

        switch (NormalizeAction(request.Action))
        {
            case "set":
                if (string.IsNullOrWhiteSpace(request.Key))
                    return "ERROR: 'key' is required for action 'set'.";
                return SerializeToolResult(_agentStateStore.SetState(request.Key, request.Value ?? "", request.ContentType));

            case "get":
                if (string.IsNullOrWhiteSpace(request.Key))
                    return "ERROR: 'key' is required for action 'get'.";
                AgentStateRecord? record = _agentStateStore.GetState(request.Key);
                return record is null ? $"State key '{request.Key}' not found." : SerializeToolResult(record);

            case "list":
                return SerializeToolResult(_agentStateStore.ListState(request.Prefix, request.Limit ?? 50));

            case "delete":
                if (string.IsNullOrWhiteSpace(request.Key))
                    return "ERROR: 'key' is required for action 'delete'.";
                return SerializeToolResult(new { deleted = _agentStateStore.DeleteState(request.Key), key = request.Key });

            default:
                return "ERROR: unknown action. Use set, get, list, or delete.";
        }
    }

    private string StateSnapshot(StateSnapshotToolRequest request)
    {
        if (_agentStateStore is null)
            return "State tools are not available.";

        switch (NormalizeAction(request.Action))
        {
            case "create":
                return SerializeToolResult(_agentStateStore.CreateSnapshot(request.Label));

            case "list":
                return SerializeToolResult(_agentStateStore.ListSnapshots(request.Limit ?? 20));

            case "get":
                if (request.Id is null)
                    return "ERROR: 'id' is required for action 'get'.";
                AgentStateSnapshotRecord? snapshot = _agentStateStore.GetSnapshot(request.Id.Value);
                return snapshot is null ? $"State snapshot '{request.Id}' not found." : SerializeToolResult(snapshot);

            case "restore":
                if (request.Id is null)
                    return "ERROR: 'id' is required for action 'restore'.";
                return SerializeToolResult(new { restored = _agentStateStore.RestoreSnapshot(request.Id.Value), id = request.Id.Value });

            default:
                return "ERROR: unknown action. Use create, list, get, or restore.";
        }
    }

    private static string NormalizeAction(string? action) => (action ?? string.Empty).Trim().ToLowerInvariant();

    private static string SerializeToolResult(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });

    private async Task<string> AskQuestionAsync(AskQuestionToolRequest request, CancellationToken cancellationToken)
    {
        if (_userInteraction is null)
            return "Interactive question handling is not available in this host.";

        List<string> validationErrors = [];
        AskQuestionsInteractionRequest interactionRequest = BuildInteractionRequest(request, validationErrors);

        if (validationErrors.Count > 0)
            return string.Join("\n", validationErrors);

        AskQuestionsInteractionResponse response = await _userInteraction.AskQuestionsAsync(interactionRequest, cancellationToken);

        Dictionary<string, object?> answerMap = new(StringComparer.OrdinalIgnoreCase);
        List<object> detailedAnswers = [];

        foreach (InteractiveQuestionAnswer answer in response.Answers)
        {
            object? value = answer.Type switch
            {
                InteractiveQuestionInputType.MultiSelect => answer.SelectedValues,
                InteractiveQuestionInputType.YesNo => answer.BooleanValue,
                InteractiveQuestionInputType.Number => answer.NumberValue,
                _ => answer.TextValue,
            };

            answerMap[answer.Key] = value;
            detailedAnswers.Add(new
            {
                key = answer.Key,
                type = ToToolTypeString(answer.Type),
                value,
                usedCustomAnswer = answer.UsedCustomAnswer,
            });
        }

        return JsonSerializer.Serialize(new
        {
            answers = answerMap,
            detailedAnswers,
        }, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    private static AskQuestionsInteractionRequest BuildInteractionRequest(AskQuestionToolRequest request, List<string> validationErrors)
    {
        AskQuestionsInteractionRequest interactionRequest = new()
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Questions" : request.Title.Trim(),
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
        };

        if (request.Questions.Count == 0)
        {
            validationErrors.Add("ask_question requires at least one question.");
            return interactionRequest;
        }

        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (AskQuestionToolQuestion question in request.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.Key))
            {
                validationErrors.Add("Each ask_question item requires a non-empty key.");
                continue;
            }

            if (!keys.Add(question.Key))
            {
                validationErrors.Add($"Duplicate ask_question key '{question.Key}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(question.Prompt))
            {
                validationErrors.Add($"Question '{question.Key}' requires a prompt.");
                continue;
            }

            if (!TryParseQuestionType(question.Type, out InteractiveQuestionInputType inputType))
            {
                validationErrors.Add($"Question '{question.Key}' has unsupported type '{question.Type}'. Use single_choice, multi_select, text, yes_no, or number.");
                continue;
            }

            List<InteractiveQuestionOption> options = question.Options
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Select(option => new InteractiveQuestionOption
                {
                    Value = option.Trim(),
                    Label = option.Trim(),
                })
                .ToList();

            if ((inputType is InteractiveQuestionInputType.SingleChoice or InteractiveQuestionInputType.MultiSelect) && options.Count == 0)
            {
                validationErrors.Add($"Question '{question.Key}' requires at least one option for type '{question.Type}'.");
                continue;
            }

            interactionRequest.Questions.Add(new InteractiveQuestionDefinition
            {
                Key = question.Key.Trim(),
                Prompt = question.Prompt.Trim(),
                Description = string.IsNullOrWhiteSpace(question.Description) ? null : question.Description.Trim(),
                Type = inputType,
                Required = question.Required,
                AllowCustomAnswer = question.AllowCustomAnswer,
                Placeholder = string.IsNullOrWhiteSpace(question.Placeholder) ? null : question.Placeholder.Trim(),
                Options = options,
                MinValue = question.MinValue,
                MaxValue = question.MaxValue,
            });
        }

        if (interactionRequest.Questions.Count == 0 && validationErrors.Count == 0)
            validationErrors.Add("ask_question did not contain any valid questions.");

        return interactionRequest;
    }

    private static bool TryParseQuestionType(string? rawType, out InteractiveQuestionInputType inputType)
    {
        switch (rawType?.Trim().ToLowerInvariant())
        {
            case "single_choice":
            case "singlechoice":
            case "choice":
                inputType = InteractiveQuestionInputType.SingleChoice;
                return true;
            case "multi_select":
            case "multiselect":
                inputType = InteractiveQuestionInputType.MultiSelect;
                return true;
            case "text":
                inputType = InteractiveQuestionInputType.Text;
                return true;
            case "yes_no":
            case "yesno":
            case "boolean":
            case "bool":
                inputType = InteractiveQuestionInputType.YesNo;
                return true;
            case "number":
            case "numeric":
                inputType = InteractiveQuestionInputType.Number;
                return true;
            default:
                inputType = InteractiveQuestionInputType.Text;
                return false;
        }
    }

    private static string ToToolTypeString(InteractiveQuestionInputType type)
    {
        return type switch
        {
            InteractiveQuestionInputType.SingleChoice => "single_choice",
            InteractiveQuestionInputType.MultiSelect => "multi_select",
            InteractiveQuestionInputType.YesNo => "yes_no",
            InteractiveQuestionInputType.Number => "number",
            _ => "text",
        };
    }
}

/// <summary>Unified request for the consolidated <c>memory</c> tool. <see cref="Action"/> selects the operation.</summary>
public sealed class MemoryToolRequest
{
    /// <summary>store | recall | search | list | get | delete | reindex.</summary>
    public string Action { get; set; } = "";
    public string? Key { get; set; }
    public string? Content { get; set; }
    public List<string>? Tags { get; set; }
    public string? Query { get; set; }
    public string? Tag { get; set; }
    public long? Id { get; set; }
    public int? Limit { get; set; }
    public int? MaxTokens { get; set; }
}

/// <summary>Unified request for the consolidated <c>state</c> tool. <see cref="Action"/> selects the operation.</summary>
public sealed class StateToolRequest
{
    /// <summary>set | get | list | delete.</summary>
    public string Action { get; set; } = "";
    public string? Key { get; set; }
    public string? Value { get; set; }
    public string? ContentType { get; set; }
    public string? Prefix { get; set; }
    public int? Limit { get; set; }
}

/// <summary>Unified request for the consolidated <c>state_snapshot</c> tool. <see cref="Action"/> selects the operation.</summary>
public sealed class StateSnapshotToolRequest
{
    /// <summary>create | list | get | restore.</summary>
    public string Action { get; set; } = "";
    public string? Label { get; set; }
    public long? Id { get; set; }
    public int? Limit { get; set; }
}
