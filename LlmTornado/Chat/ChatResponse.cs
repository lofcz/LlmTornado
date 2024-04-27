using System.Collections.Generic;
using LlmTornado.Common;

namespace LlmTornado.Chat;

public enum ChatResponseKinds
{
    Message,
    Function
}

public class ChatBlocksResponse
{
    public List<ChatResponse> Blocks { get; set; } = [];
}

public class ChatResponse
{
    public ChatResponseKinds Kind { get; set; }
    public string? Message { get; set; }
    public FunctionResult? FunctionResult { get; set; }
}