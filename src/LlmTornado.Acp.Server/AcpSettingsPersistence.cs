using LlmTornado.Cli.Core;

namespace LlmTornado.Acp.Server;

/// <summary>
/// No-op settings persistence for ACP server. The ACP server does not persist settings
/// since each process is ephemeral — launched by the IDE and terminated when no longer needed.
/// </summary>
internal sealed class AcpSettingsPersistence : ISettingsPersistence
{
    public void SaveSettings(AgentSettings settings)
    {
        // No-op: ACP server does not persist settings to disk
    }
}
