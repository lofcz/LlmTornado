using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Providers;

namespace LlmTornado.Cli.Commands;

internal sealed class ToolsCommand : ICliCommand
{
    public string Name => "tools";
    public string Description => "View tool approvals, reset permissions, and manage tool optimization";
    public string Usage => "/tools [list | approvals | reset [tool-name] | optimize [on|off|threshold <n>|status]]";

    private readonly ToolApprovalManager _toolApproval;
    private readonly CliAgentBuilder _builder;
    private readonly AgentSettings _settings;
    private readonly ProviderDetectionResult _providerResult;

    public ToolsCommand(
        ToolApprovalManager toolApproval,
        CliAgentBuilder builder,
        AgentSettings settings,
        ProviderDetectionResult providerResult)
    {
        _toolApproval = toolApproval;
        _builder = builder;
        _settings = settings;
        _providerResult = providerResult;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // Show agent's registered tools
            Dictionary<string, Common.Tool?> toolList = _builder.Agent.ToolList;
            int totalTools = _builder.TotalToolCount;
            ConsoleRenderer.WriteInfo($"{toolList.Count} tools registered (total before optimization: {totalTools}).");

            if (_builder.NeedsOptimization)
                ConsoleRenderer.WriteInfo($"  Tool optimizer: active (threshold: {_settings.MaxTools})");
            else if (!_settings.ToolOptimizerEnabled)
                ConsoleRenderer.WriteInfo("  Tool optimizer: disabled");
            else
                ConsoleRenderer.WriteInfo($"  Tool optimizer: not needed (tools within limit of {_settings.MaxTools})");

            ConsoleRenderer.WriteInfo("Run /tools list to see every tool, or /tools approvals for recorded decisions.");
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                ListAllTools();
                break;

            case "approvals":
                ListApprovals();
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

            case "optimize":
                HandleOptimizeSubcommand(args.Length > 1 ? args[1..] : []);
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }

    /// <summary>List every registered tool (the full set, before optimization) with its approval state.</summary>
    private void ListAllTools()
    {
        IReadOnlyList<Common.Tool> tools = _builder.FullToolList;
        if (tools.Count == 0)
        {
            ConsoleRenderer.WriteInfo("No tools registered.");
            return;
        }

        Dictionary<string, ToolApprovalState> approvals = _toolApproval.GetAllApprovals();

        List<string> names = tools
            .Select(t => t.ResolvedName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ConsoleRenderer.WriteInfo($"{names.Count} tools available:");
        foreach (string name in names)
        {
            string stateStr = approvals.TryGetValue(name, out ToolApprovalState state)
                ? FormatState(state)
                : "· ask each time";
            ConsoleRenderer.WriteInfo($"  {name,-44} {stateStr}");
        }
    }

    /// <summary>List only the tools that have a recorded approval decision.</summary>
    private void ListApprovals()
    {
        Dictionary<string, ToolApprovalState> approvals = _toolApproval.GetAllApprovals();
        if (approvals.Count == 0)
        {
            ConsoleRenderer.WriteInfo("No tool approval decisions recorded yet.");
            return;
        }
        foreach ((string tool, ToolApprovalState state) in approvals.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            ConsoleRenderer.WriteInfo($"  {tool,-44} {FormatState(state)}");
    }

    private static string FormatState(ToolApprovalState state) => state switch
    {
        ToolApprovalState.AlwaysAllow => "✓ always allow",
        ToolApprovalState.AlwaysDeny => "✗ always deny",
        _ => "· ask each time",
    };

    private void HandleOptimizeSubcommand(string[] args)
    {
        if (args.Length == 0)
        {
            // Show optimization status
            WriteOptimizerStatus();
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "on":
                _builder.SetOptimizerEnabled(true, _providerResult.OptimizerModel);
                _settings.ToolOptimizerEnabled = true;
                CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
                ConsoleRenderer.WriteSuccess("Tool optimizer enabled.");
                WriteOptimizerStatus();
                break;

            case "off":
                _builder.SetOptimizerEnabled(false);
                _settings.ToolOptimizerEnabled = false;
                CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
                ConsoleRenderer.WriteSuccess("Tool optimizer disabled.");
                break;

            case "threshold" when args.Length >= 2 && int.TryParse(args[1], out int threshold) && threshold > 0:
                _builder.SetMaxTools(threshold, _providerResult.OptimizerModel);
                _settings.MaxTools = threshold;
                CliStorage.SaveJson(CliStorage.SettingsPath, _settings);
                ConsoleRenderer.WriteSuccess($"Tool optimizer threshold set to {threshold}.");
                WriteOptimizerStatus();
                break;

            case "threshold":
                ConsoleRenderer.WriteError("Usage: /tools optimize threshold <number>");
                break;

            case "status":
                WriteOptimizerStatus();
                break;

            default:
                ConsoleRenderer.WriteError("Usage: /tools optimize [on|off|threshold <n>|status]");
                break;
        }
    }

    private void WriteOptimizerStatus()
    {
        ConsoleRenderer.WriteInfo("Tool Optimizer Status:");
        ConsoleRenderer.WriteInfo($"  Enabled:    {_settings.ToolOptimizerEnabled}");
        ConsoleRenderer.WriteInfo($"  Threshold:  {_settings.MaxTools}");
        ConsoleRenderer.WriteInfo($"  Total tools: {_builder.TotalToolCount}");
        ConsoleRenderer.WriteInfo($"  Active:     {_builder.NeedsOptimization}");

        if (_providerResult.OptimizerModel is not null)
            ConsoleRenderer.WriteInfo($"  Model:      {_providerResult.OptimizerModel.Name}");
        else
            ConsoleRenderer.WriteError("  Model:      none (no suitable provider detected)");
    }
}
