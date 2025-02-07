using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Threads;

public class RunStreaming
{
}

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
    public Func<ValueTask>? OnDone { get; set; }
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