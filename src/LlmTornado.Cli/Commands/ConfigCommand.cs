using System.Globalization;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Commands;

/// <summary>
/// /config — show the effective request configuration and set temperature, max output tokens,
/// or a system-prompt override file. Sampling options live-apply; a system-prompt change
/// rebuilds the agent (the prompt is baked into Instructions).
/// </summary>
internal sealed class ConfigCommand : ICliCommand
{
    public string Name => "config";
    public string Description => "Show or set request configuration (temperature, max output tokens, system prompt)";
    public string Usage => "/config [temperature <0..2|off> | max-output-tokens <n|off> | system-prompt <path|off>]";

    private readonly AgentSettings _settings;
    private readonly Action _applySamplingOptions;
    private readonly Action _rebuildAgent;
    private readonly Func<(string Name, int? ContextTokens)> _modelInfo;
    private readonly Action<AgentSettings> _persistSettings;

    public ConfigCommand(
        AgentSettings settings,
        Action applySamplingOptions,
        Action rebuildAgent,
        Func<(string Name, int? ContextTokens)> modelInfo,
        Action<AgentSettings>? persistSettings = null)
    {
        _settings = settings;
        _applySamplingOptions = applySamplingOptions;
        _rebuildAgent = rebuildAgent;
        _modelInfo = modelInfo;
        _persistSettings = persistSettings ?? SaveSettings;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            WriteEffectiveConfig();
            return Task.FromResult(true);
        }

        string key = args[0].ToLowerInvariant();
        string? value = args.Length > 1 ? args[1] : null;

        switch (key)
        {
            case "temperature" when value is not null:
                SetTemperature(value);
                break;

            case "max-output-tokens" when value is not null:
                SetMaxOutputTokens(value);
                break;

            case "system-prompt" when value is not null:
                SetSystemPrompt(string.Join(' ', args[1..]));
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }

    private void SetTemperature(string value)
    {
        if (IsOff(value))
        {
            _settings.Temperature = null;
        }
        else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double t) && t is >= 0 and <= 2)
        {
            _settings.Temperature = t;
        }
        else
        {
            ConsoleRenderer.WriteError("Temperature must be a number between 0 and 2, or 'off'.");
            return;
        }

        _applySamplingOptions();
        Persist();
        ConsoleRenderer.WriteSuccess($"Temperature: {FormatOrDefault(_settings.Temperature)}");
    }

    private void SetMaxOutputTokens(string value)
    {
        if (IsOff(value))
        {
            _settings.MaxOutputTokens = null;
        }
        else if (int.TryParse(value, out int n) && n >= 1)
        {
            _settings.MaxOutputTokens = n;
        }
        else
        {
            ConsoleRenderer.WriteError("Max output tokens must be a positive integer, or 'off'.");
            return;
        }

        _applySamplingOptions();
        Persist();
        ConsoleRenderer.WriteSuccess($"Max output tokens: {FormatOrDefault(_settings.MaxOutputTokens)}");
    }

    private void SetSystemPrompt(string value)
    {
        if (IsOff(value))
        {
            _settings.SystemPromptFile = null;
        }
        else
        {
            string path = Path.GetFullPath(value);
            if (!File.Exists(path))
            {
                ConsoleRenderer.WriteError($"System prompt file not found: {path}");
                return;
            }

            _settings.SystemPromptFile = path;
        }

        Persist();

        // The prompt is baked into agent Instructions — rebuild to take effect.
        _rebuildAgent();
        ConsoleRenderer.WriteSuccess(_settings.SystemPromptFile is null
            ? "System prompt: default (agent rebuilt)."
            : $"System prompt: {_settings.SystemPromptFile} (agent rebuilt).");
    }

    private void WriteEffectiveConfig()
    {
        (string modelName, int? contextTokens) = _modelInfo();
        ConsoleRenderer.WriteInfo($"Model:              {modelName}");
        ConsoleRenderer.WriteInfo($"Context window:     {(contextTokens is int ctx and > 0 ? ctx.ToString("N0") : "unknown")}");
        ConsoleRenderer.WriteInfo($"Temperature:        {FormatOrDefault(_settings.Temperature)}");
        ConsoleRenderer.WriteInfo($"Max output tokens:  {FormatOrDefault(_settings.MaxOutputTokens)}");
        ConsoleRenderer.WriteInfo($"Reasoning effort:   {_settings.ReasoningEffort ?? "default"}");
        ConsoleRenderer.WriteInfo($"System prompt:      {_settings.SystemPromptFile ?? "default"}");
        ConsoleRenderer.WriteInfo($"Tool result cap:    {(_settings.ToolResultTruncationEnabled ? $"{_settings.ToolResultMaxTokens:N0} tokens" : "off")}");
        ConsoleRenderer.WriteInfo($"Auto-resume:        {(_settings.AutoResume ? "on" : "off")}");
    }

    private void Persist()
    {
        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save setting: {ex.Message}");
        }
    }

    private static bool IsOff(string value) =>
        value.Equals("off", StringComparison.OrdinalIgnoreCase)
        || value.Equals("default", StringComparison.OrdinalIgnoreCase)
        || value.Equals("none", StringComparison.OrdinalIgnoreCase);

    private static string FormatOrDefault<T>(T? value) where T : struct =>
        value is null ? "default" : string.Create(CultureInfo.InvariantCulture, $"{value}");

    private static void SaveSettings(AgentSettings settings) =>
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
}
