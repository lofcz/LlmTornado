using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.DataModels;

public enum AgentRunnerEventTypes
{
    Started,
    Completed,
    Error,
    Cancelled,
    ToolInvoked,
    ToolCompleted,
    Streaming,
    MaxTurnsReached,
    GuardRailTriggered,
    UsageReceived,
    ComputerToolInvoked
}

/// <summary>
/// Base class for all events related to the Agent Runner.
/// </summary>
public class AgentRunnerEvents : EventArgs
{
    /// <summary>
    /// Type of Agent Runner event.
    /// </summary>
    public AgentRunnerEventTypes EventType { get; set; }

    /// <summary>
    /// Timestamp of when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


public class AgentRunnerComputerToolEvent : AgentRunnerEvents
{
    public IComputerAction ComputerAction { get; }
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunnerComputerToolEvent"/> class.
    /// </summary>
    public AgentRunnerComputerToolEvent(IComputerAction computerAction)
    {
        EventType = AgentRunnerEventTypes.ComputerToolInvoked;
        ComputerAction = computerAction;
    }
}

/// <summary>
/// Runner event indicating that the agent runner has started processing.
/// </summary>
public class AgentRunnerStartedEvent : AgentRunnerEvents
{
    public AgentRunnerStartedEvent()
    {
        EventType = AgentRunnerEventTypes.Started;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Agent Runner event indicating that the agent runner has completed processing.
/// </summary>
public class AgentRunnerCompletedEvent : AgentRunnerEvents
{
    /// <summary>
    /// Conversation after the agent runner has completed.
    /// </summary>
    public Conversation Conversation { get; set; }

    /// <summary>
    /// Conversation after the agent runner has completed.
    /// </summary>
    /// <param name="conversation">Current Conversation</param>
    public AgentRunnerCompletedEvent(Conversation conversation)
    {
        EventType = AgentRunnerEventTypes.Completed;
        Timestamp = DateTime.UtcNow;
        Conversation = conversation;
    }
}

/// <summary>
/// Agent Runner event indicating that the agent runner has been cancelled.
/// </summary>
public class AgentRunnerCancelledEvent : AgentRunnerEvents
{
    public AgentRunnerCancelledEvent()
    {
        EventType = AgentRunnerEventTypes.Cancelled;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Guardrail triggered event from bad input/output.
/// </summary>
public class  AgentRunnerGuardrailTriggeredEvent : AgentRunnerEvents
{
    /// <summary>
    /// Reason guardrail was triggered.
    /// </summary>
    public string Reason { get; set; }
    /// <summary>
    /// Guardrail triggered event from bad input/output.
    /// </summary>
    /// <param name="reason">Reason guardrail was triggered</param>
    public AgentRunnerGuardrailTriggeredEvent(string reason = "")
    {
        EventType = AgentRunnerEventTypes.GuardRailTriggered;
        Reason = reason;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents an event that occurs during the streaming phase of an agent runner's operation.
/// </summary>
/// <remarks>This event encapsulates information about the streaming process, including the associated  <see
/// cref="ModelStreamingEvents"/> instance. It is used to signal and handle streaming-related  activities within the
/// agent runner's lifecycle.</remarks>
public class  AgentRunnerStreamingEvent : AgentRunnerEvents
{
    /// <summary>
    /// Model streaming event associated with this agent runner streaming event.
    /// </summary>
    public ModelStreamingEvents ModelStreamingEvent { get; set; }

    /// <summary>
    /// Streaming event from the model during agent runner execution.
    /// </summary>
    /// <param name="modelStreamingEvent"></param>
    public AgentRunnerStreamingEvent(ModelStreamingEvents modelStreamingEvent)
    {
        ModelStreamingEvent = modelStreamingEvent;
        EventType = AgentRunnerEventTypes.Streaming;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Used when a tool is invoked by the agent runner.
/// </summary>
public class AgentRunnerToolInvokedEvent : AgentRunnerEvents
{
    /// <summary>
    /// Function call representing the tool that was invoked.
    /// </summary>
    public FunctionCall ToolCalled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunnerToolInvokedEvent"/> class, representing a tool invocation event.
    /// </summary>
    /// <param name="toolCall">The <see cref="FunctionCall"/> instance representing the tool call that has been invoked. This parameter must not be <c>null</c>.</param>
    public AgentRunnerToolInvokedEvent(FunctionCall toolCall)
    {
        EventType = AgentRunnerEventTypes.ToolInvoked;
        ToolCalled = toolCall;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Used when a tool has completed execution by the agent runner.
/// </summary>
public class AgentRunnerToolCompletedEvent : AgentRunnerEvents
{
    /// <summary>
    /// The function call representing the tool that was executed.
    /// </summary>
    public FunctionCall ToolCall { get; set; }
    /// <summary>
    /// The result of the tool execution.
    /// </summary>
    public FunctionResult ToolResult { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunnerToolCompletedEvent"/> class,  representing the
    /// completion of a tool call within the agent runner.
    /// </summary>
    /// <param name="toolCall">The <see cref="FunctionCall"/> instance representing the tool call that has completed.  This parameter must not
    /// be <c>null</c>.</param>
    public AgentRunnerToolCompletedEvent(FunctionCall toolCall)
    {
        EventType = AgentRunnerEventTypes.ToolCompleted;
        ToolCall = toolCall;
        ToolResult = toolCall.Result;
    }
}

/// <summary>
/// Used when an error occurs during the agent runner's operation.
/// </summary>
public class AgentRunnerErrorEvent : AgentRunnerEvents
{
    /// <summary>
    /// Error message describing the issue that occurred.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>
    /// Exception associated with the error, if available.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunnerErrorEvent"/> class, representing an error event that
    /// occurred during the execution of an agent runner.
    /// </summary>
    /// <remarks>The <see cref="AgentRunnerErrorEvent"/> class is used to encapsulate details about an error
    /// event,  including a descriptive message, an optional exception, and a timestamp indicating when the event
    /// occurred.</remarks>
    /// <param name="errorMessage">A message describing the error. This value cannot be <see langword="null"/> or empty.</param>
    /// <param name="exception">The exception associated with the error, if available. This parameter is optional and can be <see
    /// langword="null"/>.</param>
    public AgentRunnerErrorEvent(string errorMessage, Exception? exception = null)
    {
        EventType = AgentRunnerEventTypes.Error;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}

/// <summary>
/// Used when the agent runner reaches the maximum number of turns allowed.
/// </summary>
public class  AgentRunnerMaxTurnsReachedEvent : AgentRunnerEvents
{
    public AgentRunnerMaxTurnsReachedEvent()
    {
        EventType = AgentRunnerEventTypes.MaxTurnsReached;
        Timestamp = DateTime.UtcNow;
    }
}


/// <summary>
/// Used when the agent runner reaches the maximum number of turns allowed.
/// </summary>
public class AgentRunnerUsageReceivedEvent : AgentRunnerEvents
{
    public int TokenUsageAmount { get; private set; }
    public AgentRunnerUsageReceivedEvent(int usageAmount)
    {
        EventType = AgentRunnerEventTypes.UsageReceived;
        Timestamp = DateTime.UtcNow;
        TokenUsageAmount = usageAmount;
    }
}

