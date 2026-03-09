using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Blazor.Models;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    private ChatUiTokenTelemetry? _currentUserTokenTelemetry;
    private ChatUiTokenTelemetry? _currentAssistantTokenTelemetry;

    private void ResetCurrentTurnTokenTelemetry()
    {
        _currentUserTokenTelemetry = null;
        _currentAssistantTokenTelemetry = null;
    }

    private void ApplyPreflightTokenTelemetry(AgentRunnerRequestPreparedEvent prepared)
    {
        _currentUserTokenTelemetry ??= new ChatUiTokenTelemetry();
        _currentUserTokenTelemetry.ContextTokensBeforeInput = prepared.Tokens.ContextTokensBeforeInput;
        _currentUserTokenTelemetry.RequestTokensBeforeSend = prepared.Tokens.RequestTokensBeforeSend;
        _currentUserTokenTelemetry.ContextWindowTokens = prepared.Tokens.ContextWindowTokens;
        _currentUserTokenTelemetry.ContextWindowUtilization = prepared.Tokens.ContextWindowUtilization;
        _currentUserTokenTelemetry.CountingMethod = prepared.Tokens.Source == AgentTokenMeasurementSource.ProviderTokenizer
            ? "Exact tokenizer"
            : "Estimated fallback";

        PublishUserTokenTelemetry();
        UpdateContextWindowStatus(_agentBuilder?.ActiveModel, _currentUserTokenTelemetry);
    }

    private void ApplyUsageTokenTelemetry(AgentRunnerUsageReceivedEvent usage)
    {
        _currentAssistantTokenTelemetry ??= new ChatUiTokenTelemetry();
        _currentAssistantTokenTelemetry.ActualRequestCount++;
        _currentAssistantTokenTelemetry.ActualInputTokens += usage.Usage.PromptTokens;
        _currentAssistantTokenTelemetry.ActualOutputTokens += usage.Usage.CompletionTokens;
        _currentAssistantTokenTelemetry.ActualTotalTokens += usage.Usage.TotalTokens;
        _currentAssistantTokenTelemetry.CacheCreationTokens = SumNullable(_currentAssistantTokenTelemetry.CacheCreationTokens, usage.Usage.CacheCreationTokens);
        _currentAssistantTokenTelemetry.CacheReadTokens = SumNullable(_currentAssistantTokenTelemetry.CacheReadTokens, usage.Usage.CacheReadTokens);
        _currentAssistantTokenTelemetry.PromptCachedTokens = SumNullable(_currentAssistantTokenTelemetry.PromptCachedTokens, usage.Usage.PromptCachedTokens);
        _currentAssistantTokenTelemetry.PromptAudioTokens = SumNullable(_currentAssistantTokenTelemetry.PromptAudioTokens, usage.Usage.PromptAudioTokens);
        _currentAssistantTokenTelemetry.PromptImageTokens = SumNullable(_currentAssistantTokenTelemetry.PromptImageTokens, usage.Usage.PromptImageTokens);
        _currentAssistantTokenTelemetry.PromptTextTokens = SumNullable(_currentAssistantTokenTelemetry.PromptTextTokens, usage.Usage.PromptTextTokens);
        _currentAssistantTokenTelemetry.CompletionReasoningTokens = SumNullable(_currentAssistantTokenTelemetry.CompletionReasoningTokens, usage.Usage.CompletionReasoningTokens);
        _currentAssistantTokenTelemetry.CompletionAudioTokens = SumNullable(_currentAssistantTokenTelemetry.CompletionAudioTokens, usage.Usage.CompletionAudioTokens);
        _currentAssistantTokenTelemetry.CompletionTextTokens = SumNullable(_currentAssistantTokenTelemetry.CompletionTextTokens, usage.Usage.CompletionTextTokens);
        _currentAssistantTokenTelemetry.CompletionAcceptedPredictionTokens = SumNullable(_currentAssistantTokenTelemetry.CompletionAcceptedPredictionTokens, usage.Usage.CompletionAcceptedPredictionTokens);
        _currentAssistantTokenTelemetry.CompletionRejectedPredictionTokens = SumNullable(_currentAssistantTokenTelemetry.CompletionRejectedPredictionTokens, usage.Usage.CompletionRejectedPredictionTokens);
        _currentAssistantTokenTelemetry.ToolUseTokens = SumNullable(_currentAssistantTokenTelemetry.ToolUseTokens, usage.Usage.ToolUseTokens);

        PublishAssistantTokenTelemetry();
    }

    private void PublishUserTokenTelemetry()
    {
        if (Ui is null || _currentUserMessageId is null || _currentUserTokenTelemetry is null)
        {
            return;
        }

        Ui.UpdateMessageTokenTelemetry(_currentUserMessageId, _currentUserTokenTelemetry);
    }

    private void PublishAssistantTokenTelemetry()
    {
        if (Ui is null || _currentStreamingId is null || _currentAssistantTokenTelemetry is null)
        {
            return;
        }

        Ui.UpdateMessageTokenTelemetry(_currentStreamingId, _currentAssistantTokenTelemetry);
    }

    private static int? SumNullable(int? left, int? right)
    {
        if (left is null && right is null)
        {
            return null;
        }

        return (left ?? 0) + (right ?? 0);
    }
}