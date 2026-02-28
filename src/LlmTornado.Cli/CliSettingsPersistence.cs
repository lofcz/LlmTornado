using LlmTornado.Cli.Core;

namespace LlmTornado.Cli;

/// <summary>
/// CLI implementation of settings persistence — writes to disk via CliStorage.
/// </summary>
internal sealed class CliSettingsPersistence : ISettingsPersistence
{
    public void SaveSettings(AgentSettings settings)
    {
        CliStorage.SaveJson(CliStorage.SettingsPath, settings);
    }
}
