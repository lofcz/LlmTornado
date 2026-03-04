using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    // ─────────────────────────────────────────────
    // Tool approval (UI-facing)
    // ─────────────────────────────────────────────

    public Task RespondToToolApprovalAsync(string requestId, bool approved)
    {
        if (_pendingApprovals.TryRemove(requestId, out ToolApprovalRequest? request))
        {
            request.Completion.SetResult(approved);
        }
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // IToolApproval implementation
    // ─────────────────────────────────────────────

    void IToolApproval.PreApproveSkillTools(IEnumerable<string> toolNames)
    {
        foreach (string name in toolNames)
            _preApprovedTools.Add(name);
    }

    bool IToolApproval.IsAutoApproved(string toolName)
    {
        return _preApprovedTools.Contains(toolName);
    }

    async ValueTask<bool> IToolApproval.HandleToolPermissionRequest(string requestMessage)
    {
        // If auto-approved, allow immediately
        // (The runtime calls this for every tool — we check our pre-approved set)

        if (Ui is null) return true; // No UI bound → auto-approve

        // Create an approval request with a TaskCompletionSource
        var request = new ToolApprovalRequest
        {
            ToolName = ExtractToolName(requestMessage),
            RequestMessage = requestMessage
        };

        _pendingApprovals[request.Id] = request;

        // Push to UI
        Ui.ShowToolApproval(request);

        // Await user's decision (blocks this async flow until approved/denied)
        return await request.Completion.Task;
    }
}
