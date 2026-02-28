using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Agents;

namespace LlmTornado.Cli.Commands;

internal sealed class CdCommand : ICliCommand
{
    public string Name => "cd";
    public string Description => "Change the working directory and reload the agent";
    public string Usage => "/cd [<path>]";

    private readonly CliAgentBuilder _builder;
    private readonly AgentDefinitionManager _agentManager;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public CdCommand(
        CliAgentBuilder builder,
        AgentDefinitionManager agentManager,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _builder = builder;
        _agentManager = agentManager;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteInfo($"Current directory: {Environment.CurrentDirectory}");
            return Task.FromResult(true);
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
            return Task.FromResult(true);
        }

        if (!Directory.Exists(resolvedPath))
        {
            ConsoleRenderer.WriteError($"Directory not found: {resolvedPath}");
            return Task.FromResult(true);
        }

        string previousDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = resolvedPath;

        // Re-scan for AGENTS.md in the new directory hierarchy
        _agentManager.RefreshProjectContext(resolvedPath);

        // Rebuild the agent so the system prompt reflects the new CWD
        // and any newly discovered project agent context is applied
        _builder.RebuildForAgentChange(_runtimeEventHandler);

        ConsoleRenderer.WriteSuccess($"Changed directory: {resolvedPath}");

        CliAgentDefinition? projectAgent = _agentManager.GetProjectContext();
        if (projectAgent is not null)
        {
            ConsoleRenderer.WriteInfo($"  Project AGENTS.md detected: {projectAgent.FilePath}");
        }

        return Task.FromResult(true);
    }
}
