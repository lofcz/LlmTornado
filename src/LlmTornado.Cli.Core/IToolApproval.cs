namespace LlmTornado.Cli.Core;

/// <summary>
/// Abstraction for tool approval. CLI prompts interactively, ACP auto-approves.
/// </summary>
internal interface IToolApproval
{
    /// <summary>
    /// Pre-approve a set of tools (e.g. from skill allowed-tools or persona auto-approve-tools).
    /// </summary>
    void PreApproveSkillTools(IEnumerable<string> toolNames);

    /// <summary>
    /// Check if a tool is already auto-approved.
    /// </summary>
    bool IsAutoApproved(string toolName);

    /// <summary>
    /// Handle a tool permission request. Return true to allow, false to deny.
    /// </summary>
    ValueTask<bool> HandleToolPermissionRequest(string requestMessage);
}
