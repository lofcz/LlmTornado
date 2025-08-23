using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.DataModels;

public enum ChatRuntimeEventTypes
{
    /// <summary>
    /// Event raised when the runtime starts processing.
    /// </summary>
    Started,
    /// <summary>
    /// Event raised when the runtime has completed processing.
    /// </summary>
    Completed,
    /// <summary>
    /// Event raised when the runtime encounters an error during processing.
    /// </summary>
    Error,
    /// <summary>
    /// Event raised to provide intermediate updates during processing.
    /// </summary>
    Update,
    /// <summary>
    /// Event raised when the runtime is cancelled.
    /// </summary>
    Cancelled,
    /// <summary>
    /// Occurs when the associated action or event is triggered.
    /// </summary>
    /// <remarks>This event can be used to execute custom logic when the corresponding action is invoked.
    /// Ensure that any event handlers attached to this event are thread-safe if the event may be raised from multiple
    /// threads.</remarks>
    Invoked,
    /// <summary>
    /// Represents a Agent streaming event
    /// </summary>
    Streaming,

    /// <summary>
    /// Represents an orchestration event
    /// </summary>
    Orchestration,

    /// <summary>
    /// Represents an agent runner event
    /// </summary>
    AgentRunner,

    /// <summary>
    /// Default value, should not be used.
    /// </summary>
    Unknown,
}

public class ChatRuntimeEvents : EventArgs
{
    public ChatRuntimeEventTypes EventType { get; set; } = ChatRuntimeEventTypes.Unknown;
}

public class  ChatRuntimeStartedEvent : ChatRuntimeEvents
{
    public ChatRuntimeStartedEvent()
    {
        EventType = ChatRuntimeEventTypes.Started;
    }
}

public class ChatRuntimeCompletedEvent : ChatRuntimeEvents
{
    public ChatRuntimeCompletedEvent()
    {
        EventType = ChatRuntimeEventTypes.Completed;
    }
}
public class ChatRuntimeErrorEvent : ChatRuntimeEvents
{
    public Exception Exception { get; set; }
    public ChatRuntimeErrorEvent(Exception ex)
    {
        Exception = ex;
        EventType = ChatRuntimeEventTypes.Error;
    }
}
public class ChatRuntimeUpdateEvent : ChatRuntimeEvents
{
    public string UpdateMessage { get; set; }
    public ChatRuntimeUpdateEvent(string message)
    {
        UpdateMessage = message;
        EventType = ChatRuntimeEventTypes.Update;
    }
}

public class ChatRuntimeCancelledEvent : ChatRuntimeEvents
{
    public ChatRuntimeCancelledEvent()
    {
        EventType = ChatRuntimeEventTypes.Cancelled;
    }
}

public class ChatRuntimeInvokedEvent : ChatRuntimeEvents
{
    public ChatMessage Message { get; set; }
    public ChatRuntimeInvokedEvent(ChatMessage message)
    {
        Message = message;
        EventType = ChatRuntimeEventTypes.Invoked;
    }
}

public class  ChatRuntimeStreamingEvent : ChatRuntimeEvents
{
    public ModelStreamingEvents ModelStreamingEventData { get; set; }

    public ChatRuntimeStreamingEvent(ModelStreamingEvents modelEvent)
    {
        ModelStreamingEventData = modelEvent;
        EventType = ChatRuntimeEventTypes.Streaming;
    }   
}

public class ChatRuntimeOrchestrationEvent : ChatRuntimeEvents
{
    public OrchestrationEvent OrchestrationEventData { get; set; }
    public ChatRuntimeOrchestrationEvent(OrchestrationEvent orchestrationEvent)
    {
        OrchestrationEventData = orchestrationEvent;
        EventType = ChatRuntimeEventTypes.Orchestration;
    }
}

public class  ChatRuntimeAgentRunnerEvents : ChatRuntimeEvents
{
    public AgentRunnerEvents AgentRunnerEvent { get; set; }
    public ChatRuntimeAgentRunnerEvents(AgentRunnerEvents agentRunnerEvent)
    {
        AgentRunnerEvent = agentRunnerEvent;
        EventType = ChatRuntimeEventTypes.AgentRunner;
    }
}