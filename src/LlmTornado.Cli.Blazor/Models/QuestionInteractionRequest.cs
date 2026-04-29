using LlmTornado.Cli.Core.Interactions;

namespace LlmTornado.Cli.Blazor.Models;

public sealed class QuestionInteractionRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public AskQuestionsInteractionRequest Interaction { get; set; } = new();
    public TaskCompletionSource<AskQuestionsInteractionResponse> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}