using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.StateMachines
{
    /// <summary>
    /// Base interface for events in the state machine.
    /// </summary>
    public interface IStateMachineEvent
    {
        string Type { get; set; }
    }

    /// <summary>
    /// Base class for events in the state machine.
    /// </summary>
    public class StateMachineEvent : IStateMachineEvent
    {
        /// <summary>
        /// References the type of event being triggered.
        /// </summary>
        public string Type { get; set; } = "StateMachineEvent";

    }

    /// <summary>
    /// Triggered when the state machine begins processing.
    /// </summary>
    public class OnBeginStateMachineEvent : StateMachineEvent
    {
        public OnBeginStateMachineEvent()
        {
            Type = "begin";
        }
    }

    /// <summary>
    /// Triggered when the state machine finishes processing.
    /// </summary>
    public class OnFinishedStateMachineEvent : StateMachineEvent
    {
        public OnFinishedStateMachineEvent()
        {
            Type = "finished";
        }
    }

    /// <summary>
    /// Triggered when the state machine is cancelled by user.
    /// </summary>
    public class OnCancelledStateMachineEvent : StateMachineEvent
    {
        public OnCancelledStateMachineEvent()
        {
            Type = "canceled";
        }
    }

    /// <summary>
    /// Triggered when an error occurs in the state machine.
    /// </summary>
    public class OnStateMachineErrorEvent : StateMachineEvent
    {
        /// <summary>
        /// Exception that occurred during the state machine's operation.
        /// </summary>
        public Exception? Exception { get; set; }
        /// <summary>
        /// Event triggered when an error occurs in the state machine.
        /// </summary>
        /// <param name="exception">Exception that was thrown</param>
        public OnStateMachineErrorEvent(Exception? exception = null)
        {
            Type = "error";
            Exception = exception;
        }
    }

    /// <summary>
    /// Triggered on each tick of the state machine.
    /// </summary>
    public class OnTickStateMachineEvent : StateMachineEvent
    {
        
        public OnTickStateMachineEvent()
        {
            Type = "tick";

        }
    }

    /// <summary>
    /// Verbose event for logging detailed information about the state machine's operations.
    /// </summary>
    public class OnVerboseStateMachineEvent : StateMachineEvent
    {
        /// <summary>
        /// Verbose message to log.
        /// </summary>
        public string? Message { get; set; }
        public OnVerboseStateMachineEvent(string? message)
        {
            Type = "verbose";
            Message = message;
        }

    }

    /// <summary>
    /// Triggered when a state is entered.
    /// </summary>
    public class OnStateEnteredEvent : StateMachineEvent
    {
        /// <summary>
        /// Process that is being ran.
        /// </summary>
        public StateProcess StateProcess { get; set; }
        public OnStateEnteredEvent(StateProcess process)
        {
            Type = "entered";
            StateProcess = process;

        }
    }

    /// <summary>
    /// Triggered when a state is exited.
    /// </summary>
    public class OnStateExitedEvent : StateMachineEvent
    {
        /// <summary>
        /// State that is being exited.
        /// </summary>
        public BaseState State { get; set; }
        public OnStateExitedEvent(BaseState state)
        {
            Type = "exited";
            State = state;
        }
    }

    /// <summary>
    /// Triggered when a state is invoked.
    /// </summary>
    public class OnStateInvokedEvent : StateMachineEvent
    {
        /// <summary>
        /// Process that is being invoked.
        /// </summary>
        public StateProcess StateProcess { get; set; }
        public OnStateInvokedEvent(StateProcess process)
        {
            Type = "invoked";
            StateProcess = process;
        }
    }

}
