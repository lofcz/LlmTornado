namespace LlmTornado.Cli.Core;

/// <summary>
/// Abstraction for settings persistence. CLI persists to disk, ACP server uses no-op.
/// </summary>
public interface ISettingsPersistence
{
    void SaveSettings(AgentSettings settings);
}
