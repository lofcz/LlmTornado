using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration;

public class AgentOrchestration : Orchestration<ChatMessage, ChatMessage>
{
    public ConcurrentStack<ChatMessage> MessageHistory { get; set; } = new ConcurrentStack<ChatMessage>();
}
