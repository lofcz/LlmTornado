using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Agents.Samples.ContextController;

public class AgentContext
{
    public List<Tool>? Tools { get; set; } = new List<Tool>();
    public ChatModel? Model { get; set; }
    public List<ChatMessage>? ChatMessages { get; set; } = new List<ChatMessage>();
    public string? Instructions { get; set; } = string.Empty;
}
