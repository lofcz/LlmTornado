using LlmTornado.Chat;

namespace LlmTornado.Agents.DataModels;

public enum AgentTokenMeasurementSource
{
    ProviderTokenizer,
    EstimatorFallback
}

public sealed class AgentRequestTokenTelemetry
{
    public int? ContextTokensBeforeInput { get; init; }
    public int RequestTokensBeforeSend { get; init; }
    public int ContextWindowTokens { get; init; }
    public double ContextWindowUtilization { get; init; }
    public AgentTokenMeasurementSource Source { get; init; }
    public string ModelName { get; init; } = string.Empty;
}

public sealed class AgentUsageTelemetry
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
    public int? CacheCreationTokens { get; init; }
    public int? CacheReadTokens { get; init; }
    public int? PromptCachedTokens { get; init; }
    public int? PromptAudioTokens { get; init; }
    public int? PromptImageTokens { get; init; }
    public int? PromptTextTokens { get; init; }
    public int? CompletionReasoningTokens { get; init; }
    public int? CompletionAudioTokens { get; init; }
    public int? CompletionTextTokens { get; init; }
    public int? CompletionAcceptedPredictionTokens { get; init; }
    public int? CompletionRejectedPredictionTokens { get; init; }
    public int? ToolUseTokens { get; init; }

    public static AgentUsageTelemetry Empty { get; } = new();

    public static AgentUsageTelemetry FromChatUsage(ChatUsage? usage)
    {
        if (usage is null)
        {
            return Empty;
        }

        return new AgentUsageTelemetry
        {
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            TotalTokens = usage.TotalTokens,
            CacheCreationTokens = usage.CacheCreationTokens,
            CacheReadTokens = usage.CacheReadTokens,
            PromptCachedTokens = usage.PromptTokenDetails?.CachedTokens,
            PromptAudioTokens = usage.PromptTokenDetails?.AudioTokens,
            PromptImageTokens = usage.PromptTokenDetails?.ImageTokens,
            PromptTextTokens = usage.PromptTokenDetails?.TextTokens,
            CompletionReasoningTokens = usage.CompletionTokensDetails?.ReasoningTokens,
            CompletionAudioTokens = usage.CompletionTokensDetails?.AudioTokens,
            CompletionTextTokens = usage.CompletionTokensDetails?.TextTokens,
            CompletionAcceptedPredictionTokens = usage.CompletionTokensDetails?.AcceptedPredictionTokens,
            CompletionRejectedPredictionTokens = usage.CompletionTokensDetails?.RejectedPredictionTokens,
            ToolUseTokens = usage.CompletionTokensDetails?.ToolsUseTokens
        };
    }
}