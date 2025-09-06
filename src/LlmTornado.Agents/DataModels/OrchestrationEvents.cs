using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Agents.ChatRuntime.Orchestration;

namespace LlmTornado.Agents.DataModels;

/// <summary>
/// Base interface for events in the state machine.
/// </summary>
public interface IOrchestrationEvent
{
    /// <summary>
    /// Type of event being triggered by orchestration.
    /// </summary>
    string Type { get; set; }
}

/// <summary>
/// Base class for events in the state machine.
/// </summary>
public class OrchestrationEvent : IOrchestrationEvent
{
    /// <summary>
    /// References the type of event being triggered.
    /// </summary>
    public string Type { get; set; } = "OrchestrationEvent";

}


public class OnInitializedOrchestrationEvent : OrchestrationEvent
{
    public Orchestration Orchestration { get; set; }
    public OnInitializedOrchestrationEvent(Orchestration orchestration)
    {
        Type = "initialized";
        Orchestration = orchestration;
    }
}

/// <summary>
/// Triggered when the runtime begins processing.
/// </summary>
public class OnBeginOrchestrationEvent : OrchestrationEvent
{
    public OnBeginOrchestrationEvent()
    {
        Type = "begin";
    }
}

/// <summary>
/// Triggered when the runtime finishes processing.
/// </summary>
public class OnFinishedOrchestrationEvent : OrchestrationEvent
{
    public OnFinishedOrchestrationEvent()
    {
        Type = "finished";
    }
}

/// <summary>
/// Triggered when the runtime is cancelled by user.
/// </summary>
public class OnCancelledOrchestrationEvent : OrchestrationEvent
{
    public OnCancelledOrchestrationEvent()
    {
        Type = "canceled";
    }
}

/// <summary>
/// Triggered when an error occurs in the runtime.
/// </summary>
public class OnErrorOrchestrationEvent : OrchestrationEvent
{
    /// <summary>
    /// Exception that occurred during the runtime's operation.
    /// </summary>
    public Exception? Exception { get; set; }
    /// <summary>
    /// Event triggered when an error occurs in the runtime.
    /// </summary>
    /// <param name="exception">Exception that was thrown</param>
    public OnErrorOrchestrationEvent(Exception? exception = null)
    {
        Type = "error";
        Exception = exception;
    }
}

/// <summary>
/// Triggered on each tick of the runtime.
/// </summary>
public class OnTickOrchestrationEvent : OrchestrationEvent
{

    public OnTickOrchestrationEvent()
    {
        Type = "tick";

    }
}

/// <summary>
/// Verbose event for logging detailed information about the runtime's operations.
/// </summary>
public class OnVerboseOrchestrationEvent : OrchestrationEvent
{
    /// <summary>
    /// Verbose message to log.
    /// </summary>
    public string? Message { get; set; }
    public OnVerboseOrchestrationEvent(string? message)
    {
        Type = "verbose";
        Message = message;
    }

}

/// <summary>
/// Triggered when a state is entered.
/// </summary>
public class OnStartedRunnableEvent : OrchestrationEvent
{
    /// <summary>
    /// Process that is being ran.
    /// </summary>
    public OrchestrationRunnableBase RunnableBase { get; set; }
    public OnStartedRunnableEvent(OrchestrationRunnableBase runnableBase)
    {
        Type = "started";
        RunnableBase = runnableBase;
    }
}

/// <summary>
/// Triggered when a state is entered.
/// </summary>
public class OnStartedRunnableProcessEvent : OrchestrationEvent
{
    /// <summary>
    /// Process that is being ran.
    /// </summary>
    public RunnableProcess RunnableProcess { get; set; }
    public OnStartedRunnableProcessEvent(RunnableProcess process)
    {
        Type = "startedProcess";
        RunnableProcess = process;
    }
}

/// <summary>
/// Triggered when a state is entered.
/// </summary>
public class OnFinishedRunnableProcessEvent : OrchestrationEvent
{
    /// <summary>
    /// Process that is being ran.
    /// </summary>
    public RunnableProcess RunnableProcess { get; set; }
    public OnFinishedRunnableProcessEvent(RunnableProcess process)
    {
        Type = "finishedProcess";
        RunnableProcess = process;
    }
}


/// <summary>
/// Triggered when a state is exited.
/// </summary>
public class OnFinishedRunnableEvent : OrchestrationEvent
{
    /// <summary>
    /// State that is being exited.
    /// </summary>
    public OrchestrationRunnableBase Runnable { get; set; }
    public OnFinishedRunnableEvent(OrchestrationRunnableBase state)
    {
        Type = "exited";
        Runnable = state;
    }
}

/// <summary>
/// Triggered when a state is invoked.
/// </summary>
public class OnInvokedRunnableEvent : OrchestrationEvent
{
    /// <summary>
    /// Process that is being invoked.
    /// </summary>
    public RunnableProcess RunnableProcess { get; set; }
    public OnInvokedRunnableEvent(RunnableProcess process)
    {
        Type = "invoked";
        RunnableProcess = process;
    }
}
