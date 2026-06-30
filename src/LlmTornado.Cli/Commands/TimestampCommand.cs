using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Commands;

internal sealed class TimestampCommand : ICliCommand
{
    public string Name => "timestamp";
    public string Description => "Show, hide, or toggle the [timestamp] prefix added to every message";
    public string Usage => "/timestamp [on | off | toggle | status]";

    private readonly AgentSettings _settings;
    private readonly Func<bool> _getEnabled;
    private readonly Action<bool> _setEnabled;
    private readonly Action<AgentSettings> _persistSettings;

    public TimestampCommand(
        AgentSettings settings,
        Func<bool> getEnabled,
        Action<bool> setEnabled,
        Action<AgentSettings>? persistSettings = null)
    {
        _settings = settings;
        _getEnabled = getEnabled;
        _setEnabled = setEnabled;
        _persistSettings = persistSettings ?? SaveSettings;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            WriteStatus();
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "on":
                SetTimestampsEnabled(true);
                break;

            case "off":
                SetTimestampsEnabled(false);
                break;

            case "toggle":
                SetTimestampsEnabled(!_getEnabled());
                break;

            case "status":
                WriteStatus();
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }

    private void SetTimestampsEnabled(bool enabled)
    {
        _setEnabled(enabled);
        _settings.ShowTimestamps = enabled;

        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save setting: {ex.Message}");
        }

        ConsoleRenderer.WriteSuccess(enabled
            ? "Timestamp prefix enabled."
            : "Timestamp prefix disabled.");
        WriteStatus();
    }

    private void WriteStatus() =>
        ConsoleRenderer.WriteInfo($"Timestamp prefix: {(_getEnabled() ? "on" : "off")}");

    private static void SaveSettings(AgentSettings settings) =>
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
}
