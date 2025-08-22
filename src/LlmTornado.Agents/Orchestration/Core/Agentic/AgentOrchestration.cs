using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration;

public class AgentOrchestration : Core.Orchestration<ChatMessage, ChatMessage>, IRuntimeConfiguration
{
    public CancellationTokenSource cts { get ; set; }
    public List<ChatMessage> MessageHistory { get; set; } = new List<ChatMessage>();
    public virtual ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        MessageHistory.Add(message);
        return new ValueTask<ChatMessage>(message);
    }

    public virtual void ClearMessages()
    {
        MessageHistory.Clear();
    }

    public virtual List<ChatMessage> GetMessages()
    {
        return MessageHistory;
    }
}
