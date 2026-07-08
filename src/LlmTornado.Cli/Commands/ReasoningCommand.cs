using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Commands;

/// <summary>
/// /reasoning — show or set the reasoning effort sent with every request. The setting has always
/// existed in settings.json; this makes it reachable without editing the file.
/// </summary>
internal sealed class ReasoningCommand : ICliCommand
{
    private static readonly string[] ValidLevels =
        ["none", "minimal", "low", "medium", "high", "xhigh", "max", "default"];

    public string Name => "reasoning";
    public string Description => "Show or set reasoning effort for models with extended thinking";
    public string Usage => $"/reasoning [{string.Join(" | ", ValidLevels)} | off]";

    private readonly AgentSettings _settings;
    private readonly Action<string?> _applyEffort;
    private readonly Action<AgentSettings> _persistSettings;

    public ReasoningCommand(
        AgentSettings settings,
        Action<string?> applyEffort,
        Action<AgentSettings>? persistSettings = null)
    {
        _settings = settings;
        _applyEffort = applyEffort;
        _persistSettings = persistSettings ?? SaveSettings;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            WriteStatus();
            return Task.FromResult(true);
        }

        string level = args[0].ToLowerInvariant();
        if (level == "off")
            level = "none";

        if (!ValidLevels.Contains(level))
        {
            ConsoleRenderer.WriteError($"Unknown level '{args[0]}'. Usage: {Usage}");
            return Task.FromResult(true);
        }

        // "default" means provider default = no explicit effort persisted.
        string? persisted = level == "default" ? null : level;
        _applyEffort(persisted);

        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save setting: {ex.Message}");
        }

        ConsoleRenderer.WriteSuccess($"Reasoning effort set to {level}.");
        return Task.FromResult(true);
    }

    private void WriteStatus() =>
        ConsoleRenderer.WriteInfo($"Reasoning effort: {_settings.ReasoningEffort ?? "default"}");

    private static void SaveSettings(AgentSettings settings) =>
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
}
