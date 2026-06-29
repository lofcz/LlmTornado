using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Commands;

internal sealed class CdCommand : ICliCommand
{
    public string Name => "cd";
    public string Description => "Change the working directory and reload the agent";
    public string Usage => "/cd [<path>]";

    private readonly CliAgentBuilder _builder;
    private readonly AgentDefinitionManager _agentManager;
    private readonly SkillManager _skillManager;
    private readonly McpConfigLoader _mcpLoader;
    private readonly AgentSettings _settings;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public CdCommand(
        CliAgentBuilder builder,
        AgentDefinitionManager agentManager,
        SkillManager skillManager,
        McpConfigLoader mcpLoader,
        AgentSettings settings,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _builder = builder;
        _agentManager = agentManager;
        _skillManager = skillManager;
        _mcpLoader = mcpLoader;
        _settings = settings;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteInfo($"Current directory: {Environment.CurrentDirectory}");
            return true;
        }

        string targetPath = string.Join(' ', args);
        string resolvedPath;

        try
        {
            resolvedPath = Path.GetFullPath(targetPath);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Invalid path: {ex.Message}");
            return true;
        }

        if (!Directory.Exists(resolvedPath))
        {
            ConsoleRenderer.WriteError($"Directory not found: {resolvedPath}");
            return true;
        }

        Environment.CurrentDirectory = resolvedPath;

        // Re-scan skills from project/global directories resolved for the new CWD (cwd/llmtornado/skills)
        string projectSkillsDir = SkillLoader.ResolveSkillsDirectory(_settings.SkillsDirectory);
        string globalSkillsDir = SkillLoader.ResolveGlobalSkillsDirectory();
        _skillManager.LoadSkills(projectSkillsDir, globalSkillsDir, ConsoleRenderer.WriteWarning);

        // Re-discover agent personas (cwd/llmtornado/agents) and AGENTS.md context for the new CWD
        string builtInAgentsDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string globalAgentsDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
        string projectAgentsDir = AgentDefinitionLoader.ResolveAgentsDirectory(_settings.AgentsDirectory);
        _agentManager.LoadAll(builtInAgentsDir, globalAgentsDir, projectAgentsDir, resolvedPath, ConsoleRenderer.WriteWarning);

        // Reload MCP using config resolution from the new CWD and refreshed policy sandbox
        McpSessionPolicy sessionPolicy = McpSessionPolicy.FromSettings(_settings, resolvedPath);
        _mcpLoader.Configure(_settings, sessionPolicy);

        string? localMcpConfigPath = McpConfigLoader.ResolveMcpConfigPath(_settings.McpConfigPath);
        string? globalMcpConfigPath = McpConfigLoader.ResolveGlobalMcpConfigPath();
        await _mcpLoader.LoadFromPathsAsync(localMcpConfigPath, globalMcpConfigPath, ConsoleRenderer.WriteInfo);

        // Rebuild the agent so the system prompt reflects the new CWD
        // and newly discovered project agent context, skills, and MCP tools are applied
        _builder.RebuildForAgentChange(_runtimeEventHandler);

        ConsoleRenderer.WriteSuccess($"Changed directory: {resolvedPath}");
        ConsoleRenderer.WriteInfo($"  Skills: {_skillManager.GetEnabledSkills().Count} enabled, {_skillManager.GetAllSkills().Count} total");
        ConsoleRenderer.WriteInfo($"  MCP tools: {_mcpLoader.AllTools.Count}");

        AgentDefinition? projectAgent = _agentManager.GetProjectContext();
        if (projectAgent is not null)
        {
            ConsoleRenderer.WriteInfo($"  Project AGENTS.md detected: {projectAgent.FilePath}");
        }

        return true;
    }
}
