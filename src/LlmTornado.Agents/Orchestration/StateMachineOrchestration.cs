using LlmTornado.Agents.AgentStates;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.StateMachines;
using static LlmTornado.Agents.TornadoRunner;

namespace LlmTornado.Agents.Orchestration;

    /// <summary>
    /// Represents a method that processes an input string asynchronously and returns a result string.
    /// </summary>
    /// <param name="input">The input string to be processed. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the processed string.</returns>
    public delegate ValueTask<string> StateMachineRunner(string input);

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
        private StateMachineRunner? StateMachineRunnerMethod { get; set; } 
        /// <summary>
        /// Active state machines that are currently running in the agent.
        /// </summary>
        public List<StateMachine> CurrentStateMachines { get; set; } = [];

        /// <summary>
        /// Active state machines that are currently running in the agent.
        /// </summary>
        public StateMachine RootStateMachine { get; set; }


        public StateMachineOrchestration(string agentName, TornadoAgent agent) : base(agentName, agent)
        {
            StateMachineStreamingBus += InvokeStreamingCallback;  //Route State Agents streaming callbacks to the agent's event handler
            StateMachineVerboseBus += InvokeVerboseCallback; //Route State Agents verbose callbacks to the agent's event handler  
        }

        public void SetStateMachineRunnerMethod(StateMachineRunner runnerMethod)
        {
            StateMachineRunnerMethod = runnerMethod;
        }

        internal override async Task<List<ChatMessagePart>?> OnInvokedAsync(string userInput, bool streaming = true, string? base64image = null)
        {
            // Ensure that the ControlAgent is set before proceeding
            if (CurrentAgent == null)
            {
                throw new InvalidOperationException("ControlAgent is not set. Please set ControlAgent before adding to conversation.");
            }

            // Check if the cancellation token has been requested and reset it if necessary
            if (cts.Token.IsCancellationRequested)
            {
                if (!Threading.TryResetCancellationTokenSource(cts))
                {
                    cts.Dispose();
                    cts = new CancellationTokenSource();
                }
            }

            ChatMessage userMessage = new ChatMessage();
            //If userInput is not empty, create a new message item and add it to the conversation
            if (!string.IsNullOrEmpty(userInput))
            {
                userMessage = new ChatMessage(ChatMessageRoles.User, [new ChatMessagePart(userInput)]);

                string inputMessage = userInput;

                if (base64image is not null)
                {
                    userMessage.Parts?.Add(new ChatMessagePart(base64image, ImageDetail.Auto));
                }

                // If an input preprocessor is set, run it on the user input
                if (StateMachineRunnerMethod == null)
                {
                    throw new InvalidOperationException("StateMachineRunnerMethod is not set. Please set StateMachineRunnerMethod before invoking the orchestration.");
                }

                //Add in file content if provided
                if (base64image is not null)
                {
                    // If the message is a file, we need to describe it
                    string originalInstructions = CurrentAgent.Instructions;
                    CurrentAgent.Instructions = "I need you to take the input file and describe the file/image. Be the eyes for the next step who cannot see the image but needs context from within the file/image" +
                                                "Be as descriptive as possible.";
                    Conversation fileDescription = await RunAsync(CurrentAgent, messages: [userMessage], verboseCallback: InvokeVerboseCallback, cancellationToken: cts.Token);

                    //Restore the original instructions
                    CurrentAgent.Instructions = originalInstructions;

                    if (fileDescription.Messages.Count > 0)
                    {
                        // If a file description was generated, we use it to preprocess the input
                        inputMessage = $"USER QUESTION: {userInput} \n\n With provided context for Included File: {fileDescription.Messages.Last().Content}";
                    }
                }

                string? stateMachineResult = await RunStateMachine(inputMessage);
                stateMachineResult = "The following CONTEXT has been prepocessed by an Agent tasked to process the input[may or may not be relevent]. <PREPOCESSED RESULTS>" + stateMachineResult + "</PREPOCESSED RESULTS>";
                // Create a system message with the preprocessed input
                return [new ChatMessagePart(stateMachineResult)];
            }

            return null; 
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
        /// <remarks>This method invokes the <see cref="StateMachineRunnerMethod"/> delegate if it is set. The
        /// delegate is expected to perform an asynchronous operation and return a result of type <see cref="string"/>.
        /// If the delegate is not set, the method returns the original arguments.</remarks>
        /// <param name="args">The arguments to be processed by the input preprocessor.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the
        /// processed string if the input preprocessor is set; otherwise, returns the original <paramref name="args"/>.</returns>
        public async Task<string?> RunStateMachine(params object[]? args)
        {
            // Check if the StateMachineRunnerMethod delegate is set
            if (StateMachineRunnerMethod == null)
            {
                return string.Join("\n", args ?? ["N/A"]);
            }

            //Invoke the StateMachineRunnerMethod delegate with the provided arguments
            Task task = (Task)StateMachineRunnerMethod?.DynamicInvoke(args)!;

            // Wait for the task to complete
            await task.ConfigureAwait(false);

            // Get the Result property from the Task
            return (string?)StateMachineRunnerMethod?.Method.ReturnType.GetProperty("Result")?.GetValue(task);
        }
    }

