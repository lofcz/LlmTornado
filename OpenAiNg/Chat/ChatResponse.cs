namespace OpenAiNg.Chat;

public enum ChatResponseKinds
{
    Message,
    Function
}

public class ChatResponse
{
    public ChatResponseKinds Kind { get; set; }
    public string? Message { get; set; }
    public FunctionResult? FunctionResult { get; set; }
}