using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Interactions;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    // ─────────────────────────────────────────────
    // Tool approval (UI-facing)
    // ─────────────────────────────────────────────

    public Task RespondToToolApprovalAsync(string requestId, bool approved, bool alwaysAllow = false)
    {
        if (_pendingApprovals.TryRemove(requestId, out ToolApprovalRequest? request))
        {
            if (approved && alwaysAllow && !string.IsNullOrEmpty(request.ToolName))
            {
                _preApprovedTools.Add(request.ToolName);
            }

            request.Completion.SetResult(approved);
        }
        return Task.CompletedTask;
    }

    public Task RespondToQuestionInteractionAsync(string requestId, AskQuestionsInteractionResponse response)
    {
        if (_pendingQuestionInteractions.TryRemove(requestId, out QuestionInteractionRequest? request))
            request.Completion.SetResult(response);

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

    async ValueTask<AskQuestionsInteractionResponse> IUserInteractionHandler.AskQuestionsAsync(AskQuestionsInteractionRequest request, CancellationToken cancellationToken)
    {
        if (Ui is null)
            return new AskQuestionsInteractionResponse();

        var pendingRequest = new QuestionInteractionRequest
        {
            Interaction = request,
        };

        _pendingQuestionInteractions[pendingRequest.Id] = pendingRequest;
        Ui.ShowQuestionInteraction(pendingRequest);

        using CancellationTokenRegistration registration = cancellationToken.Register(() => pendingRequest.Completion.TrySetCanceled(cancellationToken));
        return await pendingRequest.Completion.Task;
    }
}
