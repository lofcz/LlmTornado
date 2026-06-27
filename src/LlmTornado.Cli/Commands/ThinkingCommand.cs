using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Commands;

internal sealed class ThinkingCommand : ICliCommand
{
    public string Name => "thinking";
    public string Description => "Show, hide, or toggle streamed thinking tokens";
    public string Usage => "/thinking [on | off | toggle | status]";

    private readonly AgentSettings _settings;
    private readonly Func<bool> _getShowThinking;
    private readonly Action<bool> _setShowThinking;
    private readonly Action<AgentSettings> _persistSettings;

    public ThinkingCommand(
        AgentSettings settings,
        Func<bool> getShowThinking,
        Action<bool> setShowThinking,
        Action<AgentSettings>? persistSettings = null)
    {
        _settings = settings;
        _getShowThinking = getShowThinking;
        _setShowThinking = setShowThinking;
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
                SetThinkingEnabled(true);
                break;

            case "off":
                SetThinkingEnabled(false);
                break;

            case "toggle":
                SetThinkingEnabled(!_getShowThinking());
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

    private void SetThinkingEnabled(bool enabled)
    {
        _setShowThinking(enabled);
        _settings.ShowThinking = enabled;

        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save setting: {ex.Message}");
        }

        ConsoleRenderer.WriteSuccess(enabled
            ? "Thinking token display enabled."
            : "Thinking token display disabled.");
        WriteStatus();
    }

    private void WriteStatus() =>
        ConsoleRenderer.WriteInfo($"Thinking tokens: {(_getShowThinking() ? "on" : "off")}");

    private static void SaveSettings(AgentSettings settings) =>
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
}