using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Threads;

/// <summary>
/// SSE.
/// </summary>
public class ServerSentEvent
{
    /// <summary>
    /// Type of the event. Can be null if the provider doesn't use SSE protocol.
    /// </summary>
    public string? EventType { get; set; }
    
    /// <summary>
    /// Data of the event.
    /// </summary>
    public string Data { get; set; } = null!;

    /// <summary>
    /// Text representation.
    /// </summary>
    public override string ToString()
    {
        return EventType is null ? Data : $"Event type: {EventType}\n{Data}";
    }
}

/// <summary>
/// Represents an event handler responsible for handling events during a streaming run process in LlmTornado framework.
/// </summary>
/// <remarks>
/// This class provides a set of delegates that can be assigned to handle specific streaming-related events
/// such as changes in thread status, run status, run step status, message status, or receiving deltas, unknown events, and errors.
/// </remarks>
public class RunStreamEventHandler
{
    /// <summary>
    ///     Called whenever a successful HTTP request is made. In case of streaming requests this is called before the stream is read.
    /// </summary>
    public Func<HttpCallRequest, ValueTask>? OutboundHttpRequestHandler { get; set; }

    /// <summary>
    ///     If this is set, HTTP level exceptions are caught and returned via this handler.
    /// </summary>
    public Func<HttpFailedRequest, ValueTask>? HttpExceptionHandler { get; set; }

    /// <summary>
    /// Invoked when the status of a TornadoThread is updated.
    /// Provides the thread and its updated status to the handler for further processing.
    /// </summary>
    public Func<TornadoThread, RunStreamEventTypeStatus, ValueTask>? OnThreadStatusChanged { get; set; }

    /// <summary>
    /// Triggered when the status of a TornadoRun changes during the processing of a streaming event.
    /// This provides the updated status alongside the associated TornadoRun instance, allowing for
    /// real-time handling of changes such as creation, progress updates, completion, failure, or cancellation.
    /// </summary>
    public Func<TornadoRun, RunStreamEventTypeStatus, ValueTask>? OnRunStatusChanged { get; set; }

    /// <summary>
    /// Triggered when the status of a specific run step changes during the processing of a tornado stream.
    /// </summary>
    public Func<TornadoRunStep, RunStreamEventTypeStatus, ValueTask>? OnRunStepStatusChanged { get; set; }

    /// <summary>
    /// Invoked when the status of a message changes during a streaming event, providing
    /// the updated message and its associated status.
    /// </summary>
    public Func<AssistantMessage, RunStreamEventTypeStatus, ValueTask>? OnMessageStatusChanged { get; set; }

    /// <summary>
    /// Invoked when a new incremental update, represented by a <see cref="MessageDelta"/>, is received
    /// during the processing of a streaming event. This provides real-time updates to the ongoing message data.
    /// </summary>
    public Func<MessageDelta, ValueTask>? OnMessageDelta { get; set; }

    /// <summary>
    /// Triggered when a delta update is received for a run step.
    /// This includes changes or new data associated with a specific run step object.
    /// </summary>
    public Func<RunStepDelta, ValueTask>? OnRunStepDelta { get; set; }

    /// <summary>
    /// Called when an unknown event type is received during the handling of a RunStreamEvent.
    /// This allows for custom handling of events that do not match predefined types.
    /// </summary>
    public Func<string, object, ValueTask>? OnUnknownEventReceived { get; set; }

    /// <summary>
    /// Invoked when an error event is received during the streaming process.
    /// </summary>
    public Func<string, ValueTask>? OnErrorReceived { get; set; }

    /// <summary>
    /// Invoked when the streaming operation is completed and the "Done" event is received.
    /// </summary>
    public Func<ValueTask>? OnFinished { get; set; }
}

internal record OpenAiAssistantStreamEvent(RunStreamEventTypeObject ObjectType, RunStreamEventTypeStatus Status)
{
    public RunStreamEventTypeObject ObjectType { get; } = ObjectType;
    public RunStreamEventTypeStatus Status { get; } = Status;
}

internal static class RunStreamEventTypeObjectCls
{
    /// <summary>
    /// See: https://platform.openai.com/docs/api-reference/assistants-streaming/events#assistants-streaming/events-thread-created
    /// </summary>
    public static readonly FrozenDictionary<string, OpenAiAssistantStreamEvent> EventsMap = new Dictionary<string, OpenAiAssistantStreamEvent>
    {
        { "thread.created", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Thread, RunStreamEventTypeStatus.Created) },
        { "thread.run.created", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Created) },
        { "thread.run.queued", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Queued) },
        { "thread.run.in_progress", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.InProgress) },
        { "thread.run.requires_action", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.RequiresAction) },
        { "thread.run.completed", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Completed) },
        { "thread.run.incomplete", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Incomplete) },
        { "thread.run.failed", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Failed) },
        { "thread.run.cancelling", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Cancelling) },
        { "thread.run.cancelled", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Cancelled) },
        { "thread.run.expired", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Run, RunStreamEventTypeStatus.Expired) },
        { "thread.run.step.created", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.RunStep, RunStreamEventTypeStatus.Created) },
        { "thread.run.step.in_progress", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.RunStep, RunStreamEventTypeStatus.InProgress) },
        { "thread.run.step.delta", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.RunStep, RunStreamEventTypeStatus.Delta) },
        { "thread.run.step.completed", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Thread, RunStreamEventTypeStatus.Completed) },
        { "thread.run.step.failed", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.RunStep, RunStreamEventTypeStatus.Failed) },
        { "thread.run.step.cancelled", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.RunStep, RunStreamEventTypeStatus.Cancelling) },
        { "thread.run.step.expired", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.RunStep, RunStreamEventTypeStatus.Expired) },
        { "thread.message.created", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Message, RunStreamEventTypeStatus.Created) },
        { "thread.message.in_progress", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Message, RunStreamEventTypeStatus.InProgress) },
        { "thread.message.delta", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Message, RunStreamEventTypeStatus.Delta) },
        { "thread.message.completed", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Message, RunStreamEventTypeStatus.Completed) },
        { "thread.message.incomplete", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Message, RunStreamEventTypeStatus.Incomplete) },
        { "error", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Error, RunStreamEventTypeStatus.Unknown) },
        { "done", new OpenAiAssistantStreamEvent(RunStreamEventTypeObject.Done, RunStreamEventTypeStatus.Unknown) }
    }.ToFrozenDictionary();
}

/// <summary>
/// Represents the type of an event occurring during a run stream process.
/// </summary>
public enum RunStreamEventTypeObject
{
    /// <summary>
    ///  Object that represents thread
    /// </summary>
    [EnumMember(Value = "thread")]
    Thread,

    /// <summary>
    /// Represents an event related to the execution of a run within a thread.
    /// </summary>
    [EnumMember(Value = "thread.run")]
    Run,

    /// <summary>
    /// Represents an event that is associated with a specific step in a run process.
    /// </summary>
    [EnumMember(Value = "thread.run.step")]
    RunStep,

    /// <summary>
    /// Object that represents a message event in the run stream.
    /// </summary>
    [EnumMember(Value = "thread.message")]
    Message,

    /// <summary>
    /// Represents an error event in the run stream. Used to signify that an error has occurred during a streaming process.
    /// </summary>
    [EnumMember(Value = "error")]
    Error,

    /// <summary>
    /// Represents the completion event in the run stream.
    /// </summary>
    [EnumMember(Value = "done")]
    Done,
    
    [EnumMember(Value = "unknown")]
    Unknown
}

/// <summary>
/// Represents the status of a streaming event in the system.
/// </summary>
public enum RunStreamEventTypeStatus
{
    /// <summary>
    /// Indicates that the event has been created.
    /// </summary>
    [EnumMember(Value = "created")]
    Created,
    
    /// <summary>
    /// Indicates that the event is in a queued state, waiting for processing.
    /// </summary>
    [EnumMember(Value = "queued")]
    Queued,
    
    /// <summary>
    /// Indicates that the event is currently in progress.
    /// </summary>
    [EnumMember(Value = "in_progress")]
    InProgress,
    
    /// <summary>
    /// Indicates a request for user or system action.
    /// </summary>
    [EnumMember(Value = "requires_action")]
    RequiresAction,
    
    /// <summary>
    /// Indicates that processing of the event has completed.
    /// </summary>
    [EnumMember(Value = "completed")]
    Completed,
    
    /// <summary>
    /// Indicates that the event has ended but is incomplete.
    /// </summary>
    [EnumMember(Value = "incomplete")]
    Incomplete,
    
    /// <summary>
    /// Indicates that the event processing has failed.
    /// </summary>
    [EnumMember(Value = "failed")]
    Failed,
    
    /// <summary>
    /// Indicates that the event is in the process of being canceled.
    /// </summary>
    [EnumMember(Value = "cancelling")]
    Cancelling,
    
    /// <summary>
    /// Indicates that the event has been canceled.
    /// </summary>
    [EnumMember(Value = "cancelled")]
    Cancelled,
    
    /// <summary>
    /// Indicates that the event has expired due to time constraints.
    /// </summary>
    [EnumMember(Value = "expired")]
    Expired,
    
    /// <summary>
    /// Indicates a partial or incremental update linked to the event.
    /// </summary>
    [EnumMember(Value = "delta")]
    Delta,
    
    /// <summary>
    /// Indicates an unknown or unspecified status.
    /// </summary>
    [EnumMember(Value = "unknown")]
    Unknown
}