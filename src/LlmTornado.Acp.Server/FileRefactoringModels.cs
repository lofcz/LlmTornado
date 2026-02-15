using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Acp.Server;

internal sealed class RefactorAnalysis
{
    public string OriginalPrompt { get; set; } = string.Empty;
    public string AnalysisSummary { get; set; } = string.Empty;
}

internal sealed class RefactorPlan
{
    public string OriginalPrompt { get; set; } = string.Empty;
    public string AnalysisSummary { get; set; } = string.Empty;
    public string PlanText { get; set; } = string.Empty;
    public int Attempt { get; set; } = 1;
    public int MaxAttempts { get; set; } = 2;
}

internal sealed class RefactorEditResult
{
    public RefactorPlan Plan { get; set; } = new();
    public string EditSummary { get; set; } = string.Empty;
}

internal sealed class RefactorVerificationResult
{
    public bool IsSuccess { get; set; }
    public ChatMessage FinalMessage { get; set; } = new(ChatMessageRoles.Assistant, string.Empty);
    public RefactorPlan? NextPlan { get; set; }
}
