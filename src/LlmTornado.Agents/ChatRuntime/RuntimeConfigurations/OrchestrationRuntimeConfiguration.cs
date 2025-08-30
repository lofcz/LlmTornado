using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;

/// <summary>
/// Used to create StateMachine like orchestrations for chat runtimes.
/// </summary>
public class OrchestrationRuntimeConfiguration : Orchestration<ChatMessage, ChatMessage>, IRuntimeConfiguration
{
    public ChatRuntime Runtime { get; set; }
    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }
    public ConcurrentStack<ChatMessage> MessageHistory { get; set; } = new ConcurrentStack<ChatMessage>();

    public OrchestrationRuntimeConfiguration()
    {
        
    }

    public virtual void OnRuntimeInitialized()
    {
        OnOrchestrationEvent += (e) =>
        {
            // Forward orchestration events to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeOrchestrationEvent(e, Runtime?.Id ?? string.Empty));
        };
    }

    public void CancelRuntime()
    {
        cts.Cancel();
    }

    public virtual async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        MessageHistory.Push(message);

        await InvokeAsync(message);

        MessageHistory.Push(Results?.Last() ?? new ChatMessage(Code.ChatMessageRoles.Assistant, "Some sort of error"));

        return GetLastMessage();
    }

    public virtual void ClearMessages()
    {
        MessageHistory.Clear();
    }

    public virtual List<ChatMessage> GetMessages()
    {
        return MessageHistory.ToList();
    }

    public virtual ChatMessage GetLastMessage()
    {
        return MessageHistory.TryPeek(out var lastMessage) ? lastMessage : new ChatMessage();
    }
}
