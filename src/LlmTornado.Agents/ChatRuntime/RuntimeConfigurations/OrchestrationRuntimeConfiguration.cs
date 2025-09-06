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

    public OrchestrationRuntimeConfiguration()
    {

    }

    private void LoadMessageHistory()
    {
        if(MessageHistoryFileLocation != null)
            MessageHistory = new PersistedConversation(MessageHistoryFileLocation);
    }

    //NOT CORRECT YET
    public virtual void OnRuntimeInitialized()
    {
        OnOrchestrationEvent += (e) =>
        {
            // Forward orchestration events to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeOrchestrationEvent(e, Runtime?.Id ?? string.Empty));
        };

        LoadMessageHistory();
    }

    public void CancelRuntime()
    {
        cts.Cancel();
    }

    public virtual async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        MessageHistory.AppendMessage(message);

        await InvokeAsync(message);

        MessageHistory.AppendMessage(Results?.Last() ?? new ChatMessage(Code.ChatMessageRoles.Assistant, "Some sort of error"));

        MessageHistory.SaveChanges();

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
