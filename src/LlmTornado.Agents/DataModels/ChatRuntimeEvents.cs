using LlmTornado.Agents.ChatRuntime.Orchestration;
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
    //Streaming,

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

/// <summary>
/// Base class for all events related to the Chat Runtime.
/// </summary>
public class ChatRuntimeEvents : EventArgs
{
    public ChatRuntimeEventTypes EventType { get; set; } = ChatRuntimeEventTypes.Unknown;
}

/// <summary>
/// Get Runtime Started Event
/// </summary>
public class  ChatRuntimeStartedEvent : ChatRuntimeEvents
{
    public ChatRuntimeStartedEvent()
    {
        EventType = ChatRuntimeEventTypes.Started;
    }
}

/// <summary>
/// Get Runtime Completed Event
/// </summary>
public class ChatRuntimeCompletedEvent : ChatRuntimeEvents
{
    public ChatRuntimeCompletedEvent()
    {
        EventType = ChatRuntimeEventTypes.Completed;
    }
}

/// <summary>
/// Represents an event that occurs when a runtime error is encountered in the chat system.
/// </summary>
/// <remarks>This event is triggered when an exception is thrown during the operation of the chat runtime. It
/// provides details about the exception that caused the error.</remarks>
public class ChatRuntimeErrorEvent : ChatRuntimeEvents
{
    public Exception Exception { get; set; }
    public ChatRuntimeErrorEvent(Exception ex)
    {
        Exception = ex;
        EventType = ChatRuntimeEventTypes.Error;
    }
}

/// <summary>
/// Get Runtime Cancelled Events
/// </summary>
public class ChatRuntimeCancelledEvent : ChatRuntimeEvents
{
    public ChatRuntimeCancelledEvent()
    {
        EventType = ChatRuntimeEventTypes.Cancelled;
    }
}

/// <summary>
/// Get Runtime Invoked Events
/// </summary>
public class ChatRuntimeInvokedEvent : ChatRuntimeEvents
{
    /// <summary>
    /// Message that was requested to be processed.
    /// </summary>
    public ChatMessage Message { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">Message that was requested to be processed.</param>
    public ChatRuntimeInvokedEvent(ChatMessage message)
    {
        Message = message;
        EventType = ChatRuntimeEventTypes.Invoked;
    }
}

///// <summary>
///// Get Model Streaming Events
///// </summary>
//public class  ChatRuntimeStreamingEvent : ChatRuntimeEvents
//{
//    public ModelStreamingEvents ModelStreamingEventData { get; set; }
//    /// <summary>
//    /// </summary>
//    /// <param name="modelEvent">Model Streaming Event</param>
//    public ChatRuntimeStreamingEvent(ModelStreamingEvents modelEvent)
//    {
//        ModelStreamingEventData = modelEvent;
//        EventType = ChatRuntimeEventTypes.Streaming;
//    }   
//}

/// <summary>
/// Represents an event related to orchestration within the chat runtime system.
/// </summary>
/// <remarks>This class encapsulates data specific to orchestration events and is used to handle and process such
/// events within the chat runtime environment. The <see cref="OrchestrationEventData"/> property contains the details
/// of the orchestration event.</remarks>
public class ChatRuntimeOrchestrationEvent : ChatRuntimeEvents
{
    public OrchestrationEvent OrchestrationEventData { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRuntimeOrchestrationEvent"/> class.
    /// </summary>
    /// <param name="orchestrationEvent">Orchestration Event</param>
    public ChatRuntimeOrchestrationEvent(OrchestrationEvent orchestrationEvent)
    {
        OrchestrationEventData = orchestrationEvent;
        EventType = ChatRuntimeEventTypes.Orchestration;
    }
}

/// <summary>
/// Get Agent Runner Events
/// </summary>
public class  ChatRuntimeAgentRunnerEvents : ChatRuntimeEvents
{
    public AgentRunnerEvents AgentRunnerEvent { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunnerEvent">Agent Runner Event</param>
    public ChatRuntimeAgentRunnerEvents(AgentRunnerEvents agentRunnerEvent)
    {
        AgentRunnerEvent = agentRunnerEvent;
        EventType = ChatRuntimeEventTypes.AgentRunner;
    }
}