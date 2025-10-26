using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ContextContainer
{
    public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public string? Goal { get; set; }
    public string? CurrentTask { get; set; } = "";
    public ChatModel CurrentModel { get; set; } = ChatModel.OpenAi.Gpt5.V5Nano;
}
