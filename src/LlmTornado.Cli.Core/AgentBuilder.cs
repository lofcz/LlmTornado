using System.Text;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.Tools;
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
    private readonly AgentDefinitionManager _agentManager;
    private readonly AgentSettings _settings;
    private readonly List<Tool>? _additionalTools;

    private ChatModel _activeModel;
    private TornadoAgent? _agent;
    private ChatRuntime? _runtime;
    private ToolOptimizer? _toolOptimizer;
    private List<Tool>? _fullToolList;

    public TornadoAgent Agent => _agent ?? throw new InvalidOperationException("Agent not built");
    public ChatRuntime Runtime => _runtime ?? throw new InvalidOperationException("Runtime not built");
    public ChatModel ActiveModel => _activeModel;

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
        AgentDefinitionManager agentManager,
        AgentSettings settings,
        ChatModel? optimizerModel,
        List<Tool>? additionalTools = null)
    {
        _api = api;
        _activeModel = activeModel;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _toolApproval = toolApproval;
        _agentManager = agentManager;
        _settings = settings;
        _additionalTools = additionalTools;

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

        // Add tools
        foreach (Tool tool in allTools)
        {
            _agent.AddTool(tool);
        }

        // Configure tool permissions (all tools require approval)
        foreach (Tool tool in allTools)
        {
            string? toolName = tool.Function?.Name;
            if (toolName is not null)
                _agent.ToolPermissionRequired[toolName] = true;
        }

        // Pre-approve skill allowed-tools
        foreach (Skill skill in _skillManager.GetEnabledSkills())
        {
            if (skill.AllowedTools.Count > 0)
                _toolApproval.PreApproveSkillTools(skill.AllowedTools);
        }

        // Create runtime
        SingletonRuntimeConfiguration runtimeConfig = new(_agent);
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
        {
            // Swap the agent's tool list to the optimized subset
            _agent.ClearTools();
            foreach (Tool tool in result.Tools)
            {
                _agent.AddTool(tool);
            }
        }

        return result;
    }

    /// <summary>
    /// Restore the full tool list after an optimized turn completes.
    /// </summary>
    public void RestoreFullTools()
    {
        if (_agent is null || _fullToolList is null)
            return;

        _agent.ClearTools();
        foreach (Tool tool in _fullToolList)
        {
            _agent.AddTool(tool);
        }
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
}
