using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Agent State Machine orchestration to manage the state of an agent conversation.
/// </summary>
public class AgentOrchestration : Orchestration<ChatMessage, ChatMessage>
{
    /// <summary>
    /// Message history for the agent orchestration.
    /// </summary>
    public ConcurrentStack<ChatMessage> MessageHistory { get; set; } = new ConcurrentStack<ChatMessage>();
}
