using System.Text;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Mcp;
using LlmTornado.Cli.Memory;
using LlmTornado.Cli.Skills;
using LlmTornado.Common;

namespace LlmTornado.Cli;

/// <summary>
/// Assembles a TornadoAgent and ChatRuntime from all system components.
/// </summary>
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
    public ChatModel ActiveModel => _activeModel;

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
    /// Build or rebuild the agent and runtime.
    /// </summary>
    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        string systemPrompt = BuildSystemPrompt();
        List<Tool> allTools = CollectTools();

        _agent = new TornadoAgent(
            client: _api,
            model: _activeModel,
            name: "CLI-Agent",
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
        foreach (CliSkill skill in _skillManager.GetEnabledSkills())
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
        _memoryManager.UpdateModel(model, model.ContextTokens);
        return Build(onRuntimeEvent);
    }

    public ChatRuntime RebuildForSkillChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return Build(onRuntimeEvent);
    }

    private string BuildSystemPrompt()
    {
        StringBuilder sb = new();

        sb.AppendLine("You are a helpful CLI assistant with access to skills and tools.");
        sb.AppendLine("You can activate skills to gain specialized knowledge and capabilities.");
        sb.AppendLine();

        List<CliSkill> enabledSkills = _skillManager.GetEnabledSkills();
        if (enabledSkills.Count > 0)
        {
            sb.AppendLine(_skillManager.BuildSkillsContextXml());
            sb.AppendLine();
            sb.AppendLine("When a user's task matches a skill, use the `load_skill` tool to activate it.");
            sb.AppendLine("The skill's full instructions will be returned and you should follow them.");
            sb.AppendLine();
        }

        sb.AppendLine($"The user's current working directory is: {Environment.CurrentDirectory}");
        return sb.ToString();
    }

    private List<Tool> CollectTools()
    {
        List<Tool> tools = [];

        // Built-in skill management tools
        tools.Add(BuildLoadSkillTool());
        tools.Add(BuildListSkillsTool());
        tools.Add(BuildReadReferenceTool());

        // Script tools from enabled skills
        List<CliSkill> enabledSkills = _skillManager.GetEnabledSkills();
        tools.AddRange(ScriptToolBuilder.BuildScriptTools(enabledSkills));

        // MCP tools
        tools.AddRange(_mcpLoader.AllTools);

        return tools;
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
                List<CliSkill> skills = _skillManager.GetEnabledSkills();
                if (skills.Count == 0)
                    return "No skills are currently enabled.";

                StringBuilder sb = new();
                sb.AppendLine("Available skills:");
                foreach (CliSkill skill in skills)
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
                CliSkill? skill = _skillManager.GetSkill(skillName);
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
