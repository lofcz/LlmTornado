using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Blazor.Models;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    // ─────────────────────────────────────────────
    // Runtime event handling
    // ─────────────────────────────────────────────

    private ValueTask HandleRuntimeEvent(ChatRuntimeEvents evt)
    {
        if (Ui is null || _currentStreamingId is null) return ValueTask.CompletedTask;

        if (evt is ChatRuntimeAgentRunnerEvents agentEvt)
        {
            switch (agentEvt.AgentRunnerEvent)
            {
                case AgentRunnerStreamingEvent streaming:
                    HandleStreamingEvent(streaming);
                    break;

                case AgentRunnerToolInvokedEvent toolInvoked:
                    Ui.AddEventChip(new ChatUiEventChip
                    {
                        Id = toolInvoked.ToolCalled.ToolCall?.Id ?? Guid.NewGuid().ToString(),
                        Type = ChatUiChipType.ToolInvoked,
                        Title = toolInvoked.ToolCalled.Name,
                        Detail = toolInvoked.ToolCalled.Arguments ?? "",
                        Status = ChatUiChipStatus.InProgress
                    });
                    break;

                case AgentRunnerToolCompletedEvent toolCompleted:
                    string chipId = toolCompleted.ToolCall.ToolCall?.Id ?? "";
                    string resultText = toolCompleted.ToolResult?.Content?.ToString() ?? "(no output)";
                    if (resultText.Length > 2000)
                        resultText = resultText[..2000] + "\n[TRUNCATED]";

                    Ui.UpdateEventChip(chipId, new ChatUiEventChip
                    {
                        Id = chipId,
                        Type = ChatUiChipType.ToolCompleted,
                        Title = toolCompleted.ToolCall.Name,
                        Detail = $"Arguments:\n{toolCompleted.ToolCall.Arguments}\n\nResult:\n{resultText}",
                        Status = ChatUiChipStatus.Completed
                    });
                    break;

                case AgentRunnerErrorEvent error:
                    Ui.AddEventChip(new ChatUiEventChip
                    {
                        Type = ChatUiChipType.Error,
                        Title = "Error",
                        Detail = error.ErrorMessage,
                        Status = ChatUiChipStatus.Failed
                    });
                    break;

                case AgentRunnerUsageReceivedEvent usage:
                    Ui.AddEventChip(new ChatUiEventChip
                    {
                        Type = ChatUiChipType.Info,
                        Title = "Usage",
                        Detail = $"Input: {usage.InputTokens} | Output: {usage.OutputTokens} | Total: {usage.TokenUsageAmount}",
                        Status = ChatUiChipStatus.Completed
                    });
                    break;
            }
        }
        else if (evt is ChatRuntimeErrorEvent runtimeError)
        {
            Ui.AddEventChip(new ChatUiEventChip
            {
                Type = ChatUiChipType.Error,
                Title = "Runtime Error",
                Detail = runtimeError.Exception.Message,
                Status = ChatUiChipStatus.Failed
            });
        }

        return ValueTask.CompletedTask;
    }

    private void HandleStreamingEvent(AgentRunnerStreamingEvent streaming)
    {
        switch (streaming.ModelStreamingEvent)
        {
            case ModelStreamingOutputTextDeltaEvent delta:
                Ui!.AppendStreamingToken(_currentStreamingId!, delta.DeltaText ?? "");
                break;

            case ModelStreamingReasoningPartAddedEvent:
                Ui!.AddEventChip(new ChatUiEventChip
                {
                    Type = ChatUiChipType.Reasoning,
                    Title = "Thinking",
                    Status = ChatUiChipStatus.InProgress
                });
                break;

            case ModelStreamingFailedEvent failed:
                Ui!.AddEventChip(new ChatUiEventChip
                {
                    Type = ChatUiChipType.Error,
                    Title = "Stream Failed",
                    Detail = failed.ErrorMessage ?? "Unknown streaming error",
                    Status = ChatUiChipStatus.Failed
                });
                break;
        }
    }
}
