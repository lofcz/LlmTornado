using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Threads;

/// <summary>
/// See: https://platform.openai.com/docs/api-reference/assistants-streaming/events
/// </summary>
internal class RunStreamEvent
{
    public string EventType { get; set; } = null!;
    public string Data { get; set; } = null!;
}

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

    public Func<TornadoThread, RunStreamEventTypeStatus, ValueTask>? OnThreadStatusChanged { get; set; }
    public Func<TornadoRun, RunStreamEventTypeStatus, ValueTask>? OnRunStatusChanged { get; set; }
    public Func<TornadoRunStep, RunStreamEventTypeStatus, ValueTask>? OnRunStepStatusChanged { get; set; }
    public Func<Message, RunStreamEventTypeStatus, ValueTask>? OnMessageStatusChanged { get; set; }
    public Func<MessageDelta, ValueTask>? OnMessageDelta { get; set; }
    public Func<RunStepDelta, ValueTask>? OnRunStepDelta { get; set; }
    public Func<string, object, ValueTask>? OnUnknownEventReceived { get; set; }
    public Func<string, ValueTask>? OnErrorReceived { get; set; }
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

public enum RunStreamEventTypeStatus
{
    [EnumMember(Value = "created")]
    Created,

    [EnumMember(Value = "queued")]
    Queued,

    [EnumMember(Value = "in_progress")]
    InProgress,

    [EnumMember(Value = "requires_action")]
    RequiresAction,

    [EnumMember(Value = "completed")]
    Completed,

    [EnumMember(Value = "incomplete")]
    Incomplete,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "cancelling")]
    Cancelling,

    [EnumMember(Value = "cancelled")]
    Cancelled,

    [EnumMember(Value = "expired")]
    Expired,

    [EnumMember(Value = "delta")]
    Delta,
    
    [EnumMember(Value = "unknown")]
    Unknown
}