using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Runtime
{
    /// <summary>
    /// Base interface for events in the state machine.
    /// </summary>
    public interface IRuntimeEvent
    {
        string Type { get; set; }
    }

    /// <summary>
    /// Base class for events in the state machine.
    /// </summary>
    public class RuntimeEvent : IRuntimeEvent
    {
        /// <summary>
        /// References the type of event being triggered.
        /// </summary>
        public string Type { get; set; } = "RuntimeEvent";

    }

    /// <summary>
    /// Triggered when the runtime begins processing.
    /// </summary>
    public class OnBeginRuntimeEvent : RuntimeEvent
    {
        public OnBeginRuntimeEvent()
        {
            Type = "begin";
        }
    }

    /// <summary>
    /// Triggered when the runtime finishes processing.
    /// </summary>
    public class OnFinishedRuntimeEvent : RuntimeEvent
    {
        public OnFinishedRuntimeEvent()
        {
            Type = "finished";
        }
    }

    /// <summary>
    /// Triggered when the runtime is cancelled by user.
    /// </summary>
    public class OnCancelledRuntimeEvent : RuntimeEvent
    {
        public OnCancelledRuntimeEvent()
        {
            Type = "canceled";
        }
    }

    /// <summary>
    /// Triggered when an error occurs in the runtime.
    /// </summary>
    public class OnRuntimeErrorEvent : RuntimeEvent
    {
        /// <summary>
        /// Exception that occurred during the runtime's operation.
        /// </summary>
        public Exception? Exception { get; set; }
        /// <summary>
        /// Event triggered when an error occurs in the runtime.
        /// </summary>
        /// <param name="exception">Exception that was thrown</param>
        public OnRuntimeErrorEvent(Exception? exception = null)
        {
            Type = "error";
            Exception = exception;
        }
    }

    /// <summary>
    /// Triggered on each tick of the runtime.
    /// </summary>
    public class OnTickRuntimeEvent : RuntimeEvent
    {

        public OnTickRuntimeEvent()
        {
            Type = "tick";

        }
    }

    /// <summary>
    /// Verbose event for logging detailed information about the runtime's operations.
    /// </summary>
    public class OnVerboseRuntimeEvent : RuntimeEvent
    {
        /// <summary>
        /// Verbose message to log.
        /// </summary>
        public string? Message { get; set; }
        public OnVerboseRuntimeEvent(string? message)
        {
            Type = "verbose";
            Message = message;
        }

    }

    /// <summary>
    /// Triggered when a state is entered.
    /// </summary>
    public class OnRunnableStartedEvent : RuntimeEvent
    {
        /// <summary>
        /// Process that is being ran.
        /// </summary>
        public RunnableProcess RunnableProcess { get; set; }
        public OnRunnableStartedEvent(RunnableProcess process)
        {
            Type = "started";
            RunnableProcess = process;

        }
    }

    /// <summary>
    /// Triggered when a state is exited.
    /// </summary>
    public class OnRunnableFinishedEvent : RuntimeEvent
    {
        /// <summary>
        /// State that is being exited.
        /// </summary>
        public BaseRunner State { get; set; }
        public OnRunnableFinishedEvent(BaseRunner state)
        {
            Type = "exited";
            State = state;
        }
    }

    /// <summary>
    /// Triggered when a state is invoked.
    /// </summary>
    public class OnStateInvokedEvent : RuntimeEvent
    {
        /// <summary>
        /// Process that is being invoked.
        /// </summary>
        public RunnableProcess StateProcess { get; set; }
        public OnStateInvokedEvent(RunnableProcess process)
        {
            Type = "invoked";
            StateProcess = process;
        }
    }

}
