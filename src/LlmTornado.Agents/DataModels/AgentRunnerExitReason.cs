using System;
using System.Collections.Generic;
using System.Text;

namespace LlmTornado.Agents.DataModels
{
    public enum AgentRunnerExitTypes
    {
        Completed,
        Cancelled,
        Error,
        ToolError,
        MaxTurnsReached,
        MaxTokensReached,
        InputGuardRailTriggered,
        RequestError,
        ResponseError
    }

    public class AgentRunnerExitReason
    {
        public AgentRunnerExitTypes ExitType { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }
        public AgentRunnerExitReason(AgentRunnerExitTypes exitType, string? message = null, Exception? exception = null)
        {
            ExitType = exitType;
            Message = message;
            Exception = exception;
        }
    }

    public class InputGuardRailTriggeredExitReason : AgentRunnerExitReason
    {
        public string GuardRailResult { get; set; }
        public GuardRailTriggerException? TriggerException { get; set; }
        public InputGuardRailTriggeredExitReason(string guardrailResult, GuardRailTriggerException? exception = null)
            : base(AgentRunnerExitTypes.InputGuardRailTriggered, guardrailResult, exception)
        {
            GuardRailResult = guardrailResult;
            TriggerException = exception;
        }
    }

    public class CompletedExitReason : AgentRunnerExitReason
    {
        public CompletedExitReason(string? message = null) : base(AgentRunnerExitTypes.Completed, message)
        {
        }
    }


    public class CancelledExitReason : AgentRunnerExitReason
    {
        public CancelledExitReason(string? message = null) : base(AgentRunnerExitTypes.Cancelled, message)
        {
        }
    }

    public class ErrorExitReason : AgentRunnerExitReason
    {
        public ErrorExitReason(string? message = null, Exception? exception = null) : base(AgentRunnerExitTypes.Error, message, exception)
        {
        }
    }
    
    public class MaxTurnsReachedExitReason : AgentRunnerExitReason
    {
        public MaxTurnsReachedExitReason(string? message = null) : base(AgentRunnerExitTypes.MaxTurnsReached, message)
        {
        }
    }
    
    public class MaxTokensReachedExitReason : AgentRunnerExitReason
    {
        public MaxTokensReachedExitReason(string? message = null) : base(AgentRunnerExitTypes.MaxTokensReached, message)
        {
        }
    }

    public class ToolErrorExitReason : AgentRunnerExitReason 
    { 
        public string ToolName { get; set; }

        public ToolErrorExitReason(string toolName, string? message = null, Exception? exception = null) : base(AgentRunnerExitTypes.ToolError, message, exception)
        {
                        ToolName = toolName;
        }
    }

    public class ResponseErrorExitReason : AgentRunnerExitReason
    {
        public ResponseErrorExitReason(Exception? exception = null) : base(AgentRunnerExitTypes.ResponseError, exception?.Message, exception)
        {
        }
    }

    public class RequestErrorExitReason : AgentRunnerExitReason
     {
        public RequestErrorExitReason(Exception? exception = null) : base(AgentRunnerExitTypes.RequestError, exception?.Message, exception)
        {
        }
    }

}
