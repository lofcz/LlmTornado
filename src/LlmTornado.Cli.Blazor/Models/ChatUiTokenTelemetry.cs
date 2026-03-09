namespace LlmTornado.Cli.Blazor.Models;

public sealed class ChatUiTokenTelemetry
{
    public int? ContextTokensBeforeInput { get; set; }
    public int? RequestTokensBeforeSend { get; set; }
    public int? ContextWindowTokens { get; set; }
    public double? ContextWindowUtilization { get; set; }
    public string? CountingMethod { get; set; }
    public int ActualRequestCount { get; set; }
    public int ActualInputTokens { get; set; }
    public int ActualOutputTokens { get; set; }
    public int ActualTotalTokens { get; set; }
    public int? CacheCreationTokens { get; set; }
    public int? CacheReadTokens { get; set; }
    public int? PromptCachedTokens { get; set; }
    public int? PromptAudioTokens { get; set; }
    public int? PromptImageTokens { get; set; }
    public int? PromptTextTokens { get; set; }
    public int? CompletionReasoningTokens { get; set; }
    public int? CompletionAudioTokens { get; set; }
    public int? CompletionTextTokens { get; set; }
    public int? CompletionAcceptedPredictionTokens { get; set; }
    public int? CompletionRejectedPredictionTokens { get; set; }
    public int? ToolUseTokens { get; set; }

    public bool HasPreflight => RequestTokensBeforeSend is not null || ContextTokensBeforeInput is not null;
    public bool HasUsage => ActualRequestCount > 0;
}