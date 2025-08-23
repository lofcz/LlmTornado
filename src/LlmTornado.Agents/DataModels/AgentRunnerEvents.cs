using LlmTornado.ChatFunctions;
using LlmTornado.Common;
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
    Debug,
    ToolInvoked,
    ToolCompleted,
    ToolError,
    Streaming,
    MaxTurnsReached,
}

public class AgentRunnerEvents : EventArgs
{
    public AgentRunnerEventTypes EventType { get; set; }
}

public class AgentRunnerStartedEvent : AgentRunnerEvents
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AgentRunnerStartedEvent()
    {
        EventType = AgentRunnerEventTypes.Started;
        Timestamp = DateTime.UtcNow;
    }
}

public class AgentRunnerCompletedEvent : AgentRunnerEvents
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AgentRunnerCompletedEvent()
    {
        EventType = AgentRunnerEventTypes.Completed;
        Timestamp = DateTime.UtcNow;
    }
}

public class AgentRunnerCancelledEvent : AgentRunnerEvents
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AgentRunnerCancelledEvent()
    {
        EventType = AgentRunnerEventTypes.Cancelled;
        Timestamp = DateTime.UtcNow;
    }
}

public class  AgentRunnerStreamingEvent : AgentRunnerEvents
{
    public ModelStreamingEvents ModelStreamingEvent { get; set; }

    public AgentRunnerStreamingEvent(ModelStreamingEvents modelStreamingEvent)
    {
        ModelStreamingEvent = modelStreamingEvent;
        EventType = AgentRunnerEventTypes.Streaming;
    }
}

public class AgentRunnerToolInvokedEvent : AgentRunnerEvents
{
    public FunctionCall ToolCalled { get; set; } 
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AgentRunnerToolInvokedEvent(FunctionCall toolCall)
    {
        EventType = AgentRunnerEventTypes.ToolInvoked;
        ToolCalled = toolCall;
        Timestamp = DateTime.UtcNow;
    }
}
public class AgentRunnerToolCompletedEvent : AgentRunnerEvents
{
    public FunctionCall ToolCall { get; set; }
    public FunctionResult ToolResult { get; set; } 
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AgentRunnerToolCompletedEvent(FunctionCall toolCall)
    {
        EventType = AgentRunnerEventTypes.ToolCompleted;
        ToolCall = toolCall;
        ToolResult = toolCall.Result;
        Timestamp = DateTime.UtcNow;
    }
}
public class AgentRunnerErrorEvent : AgentRunnerEvents
{
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AgentRunnerErrorEvent(string errorMessage, Exception? exception = null)
    {
        EventType = AgentRunnerEventTypes.Error;
        ErrorMessage = errorMessage;
        Exception = exception;
        Timestamp = DateTime.UtcNow;
    }
}

public class  AgentRunnerMaxTurnsReachedEvent : AgentRunnerEvents
{
    public AgentRunnerMaxTurnsReachedEvent()
    {
        EventType = AgentRunnerEventTypes.MaxTurnsReached;
    }
}

