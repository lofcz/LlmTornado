using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;

public class SingletonRuntimeConfiguration : IRuntimeConfiguration
{
    public ChatRuntime Runtime { get; set; }
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }
    public Func<string, ValueTask<bool>>? OnRuntimeRequestEvent { get; set; }
    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

    /// <summary>
    /// Current conversation state being managed by the sequential agents.
    /// </summary>
    public Conversation Conversation { get; set; }

    /// <summary>
    /// List of agents that will process messages sequentially.
    /// </summary>
    public TornadoAgent Agent { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialRuntimeConfiguration"/> class with the specified agents.
    /// </summary>
    /// <param name="agents">Agents to run in order</param>
    public SingletonRuntimeConfiguration(TornadoAgent agent)
    {
        Agent = agent;
        Conversation = agent.Client.Chat.CreateConversation(agent.Options);
    }

    public void OnRuntimeInitialized()
    {

    }

    public void CancelRuntime()
    {
        cts.Cancel();
        OnRuntimeEvent?.Invoke(new ChatRuntimeCancelledEvent(Runtime.Id));
    }

    public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        OnRuntimeEvent?.Invoke(new ChatRuntimeStartedEvent(Runtime.Id));

        Conversation.AppendMessage(message);

        Conversation = await Agent.RunAsync(
            appendMessages: Conversation.Messages.ToList(),
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime.Id));
                return Threading.ValueTaskCompleted;
            },
            cancellationToken: cancellationToken
            );

        OnRuntimeEvent?.Invoke(new ChatRuntimeCompletedEvent(Runtime.Id));
        return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
    }

    public ChatMessage GetLastMessage()
    {
        return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
    }

    public List<ChatMessage> GetMessages()
    {
        return Conversation?.Messages.ToList() ?? new List<ChatMessage>();
    }

    public void ClearMessages()
    {
        Conversation?.Clear();
    }
}
