namespace LlmTornado.Cli.Core.Interactions;

public interface IUserInteractionHandler
{
    ValueTask<AskQuestionsInteractionResponse> AskQuestionsAsync(AskQuestionsInteractionRequest request, CancellationToken cancellationToken = default);
}

public enum InteractiveQuestionInputType
{
    SingleChoice,
    MultiSelect,
    Text,
    YesNo,
    Number,
}

public sealed class AskQuestionsInteractionRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Questions";
    public string? Message { get; set; }
    public List<InteractiveQuestionDefinition> Questions { get; set; } = [];
}

public sealed class InteractiveQuestionDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InteractiveQuestionInputType Type { get; set; } = InteractiveQuestionInputType.Text;
    public bool Required { get; set; } = true;
    public bool AllowCustomAnswer { get; set; }
    public string? Placeholder { get; set; }
    public List<InteractiveQuestionOption> Options { get; set; } = [];
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}

public sealed class InteractiveQuestionOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class AskQuestionsInteractionResponse
{
    public List<InteractiveQuestionAnswer> Answers { get; set; } = [];
}

public sealed class InteractiveQuestionAnswer
{
    public string Key { get; set; } = string.Empty;
    public InteractiveQuestionInputType Type { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public decimal? NumberValue { get; set; }
    public List<string> SelectedValues { get; set; } = [];
    public bool UsedCustomAnswer { get; set; }
}

public sealed class AskQuestionToolRequest
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public List<AskQuestionToolQuestion> Questions { get; set; } = [];
}

public sealed class AskQuestionToolQuestion
{
    public string Key { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public string? Description { get; set; }
    public bool Required { get; set; } = true;
    public bool AllowCustomAnswer { get; set; }
    public string? Placeholder { get; set; }
    public List<string> Options { get; set; } = [];
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}