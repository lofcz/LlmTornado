namespace LlmTornado.Cli.Commands;

internal sealed class ToolsCommand : ICliCommand
{
    public string Name => "tools";
    public string Description => "View tool approvals and reset permissions";
    public string Usage => "/tools [list | reset [tool-name]]";

    private readonly ToolApprovalManager _toolApproval;
    private readonly CliAgentBuilder _builder;

    public ToolsCommand(ToolApprovalManager toolApproval, CliAgentBuilder builder)
    {
        _toolApproval = toolApproval;
        _builder = builder;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // Show agent's registered tools
            Dictionary<string, Common.Tool?> toolList = _builder.Agent.ToolList;
            ConsoleRenderer.WriteInfo($"{toolList.Count} tools registered.");
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                Dictionary<string, ToolApprovalState> approvals = _toolApproval.GetAllApprovals();
                if (approvals.Count == 0)
                {
                    ConsoleRenderer.WriteInfo("No tool approval decisions recorded yet.");
                    break;
                }
                foreach ((string tool, ToolApprovalState state) in approvals.OrderBy(x => x.Key))
                {
                    string stateStr = state switch
                    {
                        ToolApprovalState.AlwaysAllow => "✓ always allow",
                        ToolApprovalState.AlwaysDeny => "✗ always deny",
                        _ => "? unknown",
                    };
                    ConsoleRenderer.WriteInfo($"  {tool,-40} {stateStr}");
                }
                break;

            case "reset" when args.Length >= 2:
                if (_toolApproval.ResetTool(args[1]))
                    ConsoleRenderer.WriteSuccess($"Reset approval for: {args[1]}");
                else
                    ConsoleRenderer.WriteError($"No approval found for: {args[1]}");
                break;

            case "reset":
                _toolApproval.ResetAll();
                ConsoleRenderer.WriteSuccess("All tool approvals reset.");
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }
}
