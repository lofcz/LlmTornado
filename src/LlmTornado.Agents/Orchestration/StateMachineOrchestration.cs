using LlmTornado.Agents.DataModels;
using LlmTornado.StateMachines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration
{
    public class StateMachineOrchestration : ChatOrchestration
    {
        /// <summary>
        /// Occurs when a new state machine is added.
        /// </summary>
        /// <remarks>Subscribe to this event to perform actions when a state machine is added to the
        /// collection. The event handler receives an argument of type <see cref="StateMachine"/>,
        /// representing the added state machine.</remarks>
        public Action<StateMachine>? OnStateMachineAdded;

        /// <summary>
        /// Occurs when a state machine is removed from the collection.
        /// </summary>
        /// <remarks>This event is triggered whenever a state machine is removed, allowing subscribers to
        /// perform any necessary cleanup or updates in response to the removal. Ensure that any event handlers attached
        /// to this event are thread-safe, as the event may be raised from different threads.</remarks>
        public Action<StateMachine>? OnStateMachineRemoved;

        /// <summary>
        /// Occurs when a state machine is removed from the collection.
        /// </summary>
        /// <remarks>This event is triggered whenever a state machine is removed, allowing subscribers to
        /// perform any necessary cleanup or updates in response to the removal. Ensure that any event handlers attached
        /// to this event are thread-safe, as the event may be raised from different threads.</remarks>
        public Action<ModelStreamingEvents>? OnStateMachineStreamingEvent;

        /// <summary>
        /// Occurs when a state machine is removed from the collection.
        /// </summary>
        /// <remarks>This event is triggered whenever a state machine is removed, allowing subscribers to
        /// perform any necessary cleanup or updates in response to the removal. Ensure that any event handlers attached
        /// to this event are thread-safe, as the event may be raised from different threads.</remarks>
        public Action<string>? OnStateMachineVerboseEvent;

        /// <summary>
        /// Used to handle streaming callbacks from the agent.
        /// </summary>
        public StreamingCallbacks? StateMachineStreamingBus;

        /// <summary>
        /// Used to get logging information from the runner probably will eventually be used to send status updates to the Control Agent.
        /// </summary>
        public RunnerVerboseCallbacks? StateMachineVerboseBus;

        /// <summary>
        /// Used to handle The controlling state machine to process the input before it is sent to the model.
        /// </summary>
        public InputProcessorDelegate? InputPreprocessor { get; set; } = null!;
        /// <summary>
        /// Active state machines that are currently running in the agent.
        /// </summary>
        public List<StateMachine> CurrentStateMachines { get; set; } = [];


        public StateMachineOrchestration(string agentName, TornadoAgent agent) : base(agentName, agent)
        {
            StateMachineStreamingBus += InvokeStreamingCallback;  //Route State Agents streaming callbacks to the agent's event handler
            StateMachineVerboseBus += InvokeVerboseCallback; //Route State Agents verbose callbacks to the agent's event handler  
        }

        /// <summary>
        /// Used to send streaming messages from the Control Agent
        /// </summary>
        /// <param name="message"></param>
        private ValueTask InvokeStreamingCallback(ModelStreamingEvents message)
        {
            StateMachineStreamingBus?.Invoke(message);
            return default; // Return a completed ValueTask
        }


        /// <summary>
        /// Used to send verbose logging messages from the Control Agent
        /// </summary>
        /// <param name="message"></param>
        private ValueTask InvokeVerboseCallback(string message)
        {
            StateMachineVerboseBus?.Invoke(message);
            return default; // Return a completed ValueTask
        }


        /// <summary>
        /// Adds a state machine to the current collection and triggers the StateMachineAdded event.
        /// </summary>
        /// <remarks>This method appends the specified state machine to the <c>CurrentStateMachines</c>
        /// collection and invokes the <c>StateMachineAdded</c> event, passing the added state machine as an
        /// argument.</remarks>
        /// <param name="stateMachine">The state machine to add. Cannot be null.</param>
        public void AddStateMachine(StateMachine stateMachine)
        {
            CurrentStateMachines.Add(stateMachine);
            OnStateMachineAdded?.Invoke(stateMachine);
        }

        /// <summary>
        /// Removes the specified state machine from the current collection and triggers the removal event.
        /// </summary>
        /// <remarks>This method removes the given state machine from the <c>CurrentStateMachines</c>
        /// collection and  invokes the <c>StateMachineRemoved</c> event to notify subscribers of the removal.</remarks>
        /// <param name="stateMachine">The state machine to be removed. Cannot be null.</param>
        public void RemoveStateMachine(StateMachine stateMachine)
        {
            OnStateMachineRemoved?.Invoke(stateMachine); // Trigger the StateMachineRemoved event
            CurrentStateMachines.Remove(stateMachine); // Remove the state machine from the collection
        }

        /// <summary>
        /// Cancels the execution of all current state machines.
        /// </summary>
        /// <remarks>This method signals a cancellation request to all state machines currently managed by
        /// this instance. It stops each state machine and cancels any ongoing operations. Ensure that the state
        /// machines can handle cancellation requests appropriately.</remarks>
        public override void CancelExecution()
        {
            cts.Cancel(); // Signal cancellation to all state machines

            foreach (StateMachine stateMachine in CurrentStateMachines)
            {
                stateMachine.Stop();
            }
        }

        /// <summary>
        /// Executes the input preprocessing operation using the specified arguments.
        /// </summary>
        /// <remarks>This method invokes the <see cref="InputPreprocessor"/> delegate if it is set. The
        /// delegate is expected to perform an asynchronous operation and return a result of type <see cref="string"/>.
        /// If the delegate is not set, the method returns the original arguments.</remarks>
        /// <param name="args">The arguments to be processed by the input preprocessor.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the
        /// processed string if the input preprocessor is set; otherwise, returns the original <paramref name="args"/>.</returns>
        public async Task<string?> RunPreprocess(params object[]? args)
        {
            // Check if the InputPreprocessor delegate is set
            if (InputPreprocessor == null)
            {
                return string.Join("\n", args ?? ["N/A"]);
            }

            //Invoke the InputPreprocessor delegate with the provided arguments
            Task task = (Task)InputPreprocessor?.DynamicInvoke(args)!;

            // Wait for the task to complete
            await task.ConfigureAwait(false);

            // Get the Result property from the Task
            return (string?)InputPreprocessor?.Method.ReturnType.GetProperty("Result")?.GetValue(task);
        }
    }
}
