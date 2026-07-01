using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Commands;

internal sealed class MaxToolsCommand : ICliCommand
{
    public string Name => "max-tools";
    public string Description => "Show or set max tools sent before tool optimization";
    public string Usage => "/max-tools [number | status]";

    private readonly AgentSettings _settings;
    private readonly Action<int> _applyMaxTools;
    private readonly Func<int> _getTotalTools;
    private readonly Func<bool> _getNeedsOptimization;
    private readonly Action<AgentSettings> _persistSettings;

    public MaxToolsCommand(
        AgentSettings settings,
        Action<int> applyMaxTools,
        Func<int> getTotalTools,
        Func<bool> getNeedsOptimization,
        Action<AgentSettings>? persistSettings = null)
    {
        _settings = settings;
        _applyMaxTools = applyMaxTools;
        _getTotalTools = getTotalTools;
        _getNeedsOptimization = getNeedsOptimization;
        _persistSettings = persistSettings ?? SaveSettings;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || string.Equals(args[0], "status", StringComparison.OrdinalIgnoreCase))
        {
            WriteStatus();
            return Task.FromResult(true);
        }

        if (!int.TryParse(args[0], out int maxTools) || maxTools <= 0)
        {
            ConsoleRenderer.WriteError($"Usage: {Usage}");
            return Task.FromResult(true);
        }

        SetMaxTools(maxTools);
        return Task.FromResult(true);
    }

    private void SetMaxTools(int maxTools)
    {
        _applyMaxTools(maxTools);
        _settings.MaxTools = maxTools;

        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save setting: {ex.Message}");
        }

        ConsoleRenderer.WriteSuccess($"Max tools before optimization set to {maxTools}.");
        WriteStatus();
    }

    private void WriteStatus()
    {
        ConsoleRenderer.WriteInfo($"Max tools before optimization: {_settings.MaxTools}");
        ConsoleRenderer.WriteInfo($"Total tools registered: {_getTotalTools()}");
        ConsoleRenderer.WriteInfo($"Tool optimizer enabled: {_settings.ToolOptimizerEnabled}");
        ConsoleRenderer.WriteInfo($"First-run optimization pending: {_getNeedsOptimization()}");
    }

    private static void SaveSettings(AgentSettings settings) =>
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
}
