using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Utility;
using LlmTornado.Chat;
using Microsoft.Extensions.Configuration;
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
    public PersistedConversation MessageHistory { get; set; } 
    public string? MessageHistoryFileLocation { get; set; }
    public Func<OrchestrationRuntimeConfiguration, ValueTask>? CustomInitialization { get; set; }

    public OrchestrationRuntimeConfiguration()
    {

    }

    private void LoadMessageHistory()
    {
        if(MessageHistoryFileLocation != null)
            MessageHistory = new PersistedConversation(MessageHistoryFileLocation);
    }

    public virtual void OnRuntimeInitialized()
    {
        OnOrchestrationEvent += (e) =>
        {
            // Forward orchestration events to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeOrchestrationEvent(e, Runtime?.Id ?? string.Empty));
        };

        LoadMessageHistory();

        CustomInitialization?.Invoke(this).GetAwaiter().GetResult();
    }

    public void CancelRuntime()
    {
        cts.Cancel();
        OnRuntimeEvent?.Invoke(new ChatRuntimeCancelledEvent(Runtime.Id));
    }

    public virtual async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        // Invoke the StartingExecution event to signal the beginning of the execution process
        OnRuntimeEvent?.Invoke(new ChatRuntimeStartedEvent(Runtime.Id));

        MessageHistory.AppendMessage(message);

        await InvokeAsync(message);

        MessageHistory.AppendMessage(Results?.Last() ?? new ChatMessage(Code.ChatMessageRoles.Assistant, "Some sort of error"));

        MessageHistory.SaveChanges();

        OnRuntimeEvent?.Invoke(new ChatRuntimeCompletedEvent(Runtime.Id));

        return GetLastMessage();
    }

    public virtual void ClearMessages()
    {
        MessageHistory.Clear();
    }

    public virtual List<ChatMessage> GetMessages()
    {
        return MessageHistory.Messages;
    }

    public virtual ChatMessage GetLastMessage()
    {
        return MessageHistory.Messages.Last();
    }
}
