using LlmTornado.Cli.Core;

namespace LlmTornado.Acp.Server;

/// <summary>
/// Auto-approve tool approval for ACP server. All tools are auto-approved since
/// the ACP server runs non-interactively — there is no human to prompt for approval.
/// </summary>
internal sealed class AcpToolApproval : IToolApproval
{
    public void PreApproveSkillTools(IEnumerable<string> toolNames)
    {
        // No-op: all tools are already approved
    }

    public bool IsAutoApproved(string toolName) => true;

    public ValueTask<bool> HandleToolPermissionRequest(string requestMessage) => ValueTask.FromResult(true);
}
