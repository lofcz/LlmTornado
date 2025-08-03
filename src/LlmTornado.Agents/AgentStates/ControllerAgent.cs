using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.StateMachines;
using static LlmTornado.Agents.TornadoRunner;

namespace LlmTornado.Agents.AgentStates;

/// <summary>
/// Represents a method that processes an input string asynchronously and returns a result string.
/// </summary>
/// <param name="input">The input string to be processed. Cannot be null.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the processed string.</returns>
public delegate Task<string> InputProcessorDelegate(string input);

public abstract class ControllerAgent
{
    private readonly string _agentName;
    public string AgentName => _agentName;

    private string _agentId = Guid.NewGuid().ToString();
    public string AgentId => _agentId;

    public List<ChatMessage> SharedModelItems = [];

    /// <summary>
    /// Current Agent in Control of the conversation.
    /// </summary>
    public TornadoAgent InitialAgent { get; set; }

    /// <summary>
    /// Agent used to manage the active conversation and report results of the state machines.
    /// </summary>
    public TornadoAgent CurrentAgent { get; set; }
    /// <summary>
    /// Used to handle The controlling state machine to process the input before it is sent to the model.
    /// </summary>
    public InputProcessorDelegate? InputPreprocessor { get; set; } = null!;
    /// <summary>
    /// Active state machines that are currently running in the agent.
    /// </summary>
    public List<StateMachine> CurrentStateMachines { get; set; } = [];
    /// <summary>
    /// Latest result from the ControlAgent run. (holds messages buffer for Chat API, response API uses threadID)
    /// </summary>
    public Conversation CurrentResult { get; set; } 
    /// <summary>
    /// Thread ID of the main conversation thread for the response API
    /// </summary>
    public string MainThreadId { get; set; } = "";

    /// <summary>
    /// Occurs when Agent gets a new message to process.
    /// </summary>
    public event Action? StartingExecution;
    /// <summary>
    /// Occurs when the execution process has completed.
    /// </summary>
    /// <remarks>Subscribe to this event to perform actions after the execution process finishes.  The
    /// event handler will be invoked when the execution is complete.</remarks>
    public event Action? FinishedExecution;

    /// <summary>
    /// Occurs when a verbose message related to the Control Agent  is generated.
    /// </summary>
    /// <remarks>This event is triggered to provide detailed logging information about the Control Agent
    /// Subscribers can use this event to capture and process verbose messages for diagnostic or logging
    /// purposes.</remarks>
    public event Action<string>? ControllerVerboseEvent;
    /// <summary>
    /// Main streaming event for the Control Agent to handle streaming messages for the Control Agent conversation.
    /// </summary>
    public event StreamingCallbacks? ControllerStreamingEvent;
    /// <summary>
    /// Occurs when a new state machine is added.
    /// </summary>
    /// <remarks>Subscribe to this event to perform actions when a state machine is added to the
    /// collection. The event handler receives an argument of type <see cref="StateMachine"/>,
    /// representing the added state machine.</remarks>
    public event Action<StateMachine>? StateMachineAdded;
    /// <summary>
    /// Occurs when a state machine is removed from the collection.
    /// </summary>
    /// <remarks>This event is triggered whenever a state machine is removed, allowing subscribers to
    /// perform any necessary cleanup or updates in response to the removal. Ensure that any event handlers attached
    /// to this event are thread-safe, as the event may be raised from different threads.</remarks>
    public event Action<StateMachine>? StateMachineRemoved;

    /// <summary>
    /// Used to handle streaming callbacks from the agent.
    /// </summary>
    public StreamingCallbacks? StreamingCallback;

    /// <summary>
    /// Used to get logging information from the runner probably will eventually be used to send status updates to the Control Agent.
    /// </summary>
    public RunnerVerboseCallbacks? VerboseCallback;
        
    /// <summary>
    /// Master Cancellation token source for the Control Agent and the rest of the state machines.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();


    public ControllerAgent(string agentName)
    {
        // Initialize the agent and set up the callbacks
        InitialAgent = InitializeAgent(); 
        _agentName = agentName;
        StreamingCallback += InvokeStreamingCallback;  //Route State Agents streaming callbacks to the agent's event handler
        VerboseCallback += InvokeVerboseCallback; //Route State Agents verbose callbacks to the agent's event handler  
        CurrentAgent = InitialAgent; // Set the initial agent as the current agent
        CurrentAgent.Options.CancellationToken = CancellationTokenSource.Token; // Set the cancellation token source for the Control Agent

    }

    /// <summary>
    /// Used to send streaming messages from the Control Agent
    /// </summary>
    /// <param name="message"></param>
    private ValueTask InvokeStreamingCallback(ModelStreamingEvents message)
    {
        StreamingCallback?.Invoke(message);
        return default; // Return a completed ValueTask
    }

    public async Task CheckForHandoff(List<ChatMessage> messages)
    {
        if(CurrentAgent.HandoffAgents.Count == 0)
        {
            return; // No handoff agents to process
        }

        string instructions = @$"
I need you to decide if you need to handoff the conversation to another agent.
If you do, please return the agent you want to handoff to and the reason for the handoff.
If not just return CurrentAgent
Out of the following Agents which agent should we Handoff the conversation too and why? 


{{""NAME"": ""CurrentAgent"",""Instructions"":""{CurrentAgent.Instructions}""}}


{string.Join("\n\n", CurrentAgent.HandoffAgents.Select(agent => $" {{\"NAME\": \"{agent.Name}\",\"Handoff Reason\":\"{agent.HandoffReason}\"}}"))}

";
        var handoffDecider = new TornadoAgent(CurrentAgent.Client, ChatModel.OpenAi.Gpt41.V41Nano, instructions);
        handoffDecider.Options.ResponseFormat = AgentHandoff.CreateHandoffResponseFormat(CurrentAgent.HandoffAgents.ToArray());
        handoffDecider.Options.CancellationToken = CancellationTokenSource.Token; // Set the cancellation token source for the Control Agent

        string prompt = "Current Conversation:\n";

        foreach(ChatMessage message in messages)
        {
            foreach(ChatMessagePart part in message.Parts ?? [])
            {
                if (part is not { Text: ""})
                {
                    prompt += $"{message.Role}: {part.Text}\n";
                }
            }
        }

        Conversation handoff = await TornadoRunner.RunAsync(handoffDecider, prompt,
            verboseCallback: InvokeVerboseCallback, streaming: false, cancellationToken: CancellationTokenSource);

        if (handoff.Messages.Count > 0 && handoff.Messages.Last().Content != null)
        {
            var selectedAgent = AgentHandoff.ParseHandoffResponse(handoff.Messages.Last().Content, out string? reasoning);
            CurrentAgent = CurrentAgent.HandoffAgents.FirstOrDefault(agent => agent.Name.Equals(selectedAgent, StringComparison.OrdinalIgnoreCase))?.Agent ?? CurrentAgent; 
        }
    }

    /// <summary>
    /// Used to send verbose logging messages from the Control Agent
    /// </summary>
    /// <param name="message"></param>
    private ValueTask InvokeVerboseCallback(string message)
    {
        VerboseCallback?.Invoke(message);
        return default; // Return a completed ValueTask
    }
    

    /// <summary>
    /// Invokes the streaming event with the specified message.
    /// </summary>
    /// <remarks>This method triggers the <see cref="streamingEvent"/> delegate, allowing subscribers
    /// to handle the message. Ensure that the <paramref name="message"/> is not null to avoid potential
    /// exceptions.</remarks>
    /// <param name="message">The message to be passed to the streaming event. Cannot be null.</param>
    private ValueTask HandleControllerStreamingEvent(ModelStreamingEvents message)
    {
        ControllerStreamingEvent?.Invoke(message);
        return default; // Return a completed ValueTask
    }

    /// <summary>
    /// Invokes the verbose event with the specified message.
    /// </summary>
    /// <remarks>This method triggers the <c>verboseEvent</c> if it has any subscribers. Ensure that
    /// the event is properly subscribed to before calling this method.</remarks>
    /// <param name="message">The message to be passed to the event handlers. Cannot be null.</param>
    private ValueTask HandleControllerVerboseEvent(string message)
    {
        ControllerVerboseEvent?.Invoke(message);
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
        StateMachineAdded?.Invoke(stateMachine);
    }

    /// <summary>
    /// Removes the specified state machine from the current collection and triggers the removal event.
    /// </summary>
    /// <remarks>This method removes the given state machine from the <c>CurrentStateMachines</c>
    /// collection and  invokes the <c>StateMachineRemoved</c> event to notify subscribers of the removal.</remarks>
    /// <param name="stateMachine">The state machine to be removed. Cannot be null.</param>
    public void RemoveStateMachine(StateMachine stateMachine)
    {
        StateMachineRemoved?.Invoke(stateMachine); // Trigger the StateMachineRemoved event
        CurrentStateMachines.Remove(stateMachine); // Remove the state machine from the collection
    }

    /// <summary>
    /// Cancels the execution of all current state machines.
    /// </summary>
    /// <remarks>This method signals a cancellation request to all state machines currently managed by
    /// this instance. It stops each state machine and cancels any ongoing operations. Ensure that the state
    /// machines can handle cancellation requests appropriately.</remarks>
    public void CancelExecution()
    {
        CancellationTokenSource.Cancel();

        foreach (StateMachine stateMachine in CurrentStateMachines)
        {
            stateMachine.Stop();
        }
    }

    /// <summary>
    /// Initializes the agent, preparing it for operation.
    /// </summary>
    /// <remarks>This method must be called before the agent can be used. It sets up necessary
    /// resources and configurations.</remarks>
    public abstract TornadoAgent InitializeAgent();

    /// <summary>
    /// Adds a user input to the conversation and processes it through the control agent.
    /// </summary>
    /// <remarks>This method processes the user input by optionally preprocessing it and then running
    /// it through the control agent. If a message is provided, it is assumed to be an image or file and is added
    /// directly to the conversation.</remarks>
    /// <param name="userInput">The text input provided by the user to be added to the conversation.</param>
    /// <param name="message">An optional <see cref="ModelItem"/> representing a message to be added. If null, a new message is created
    /// from the user input.</param>
    /// <param name="streaming">A boolean value indicating whether the response should be streamed. <see langword="true"/> to enable
    /// streaming; otherwise, <see langword="false"/>.</param>
    /// <returns>A <see cref="Task{String}"/> representing the asynchronous operation. The task result contains the processed
    /// conversation response text.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <c>ControlAgent</c> is not set before adding to the conversation.</exception>
    public async Task<string> AddToConversation(string userInput, ChatMessage message = null, bool streaming = true)
    {
        // Ensure that the ControlAgent is set before proceeding
        if (CurrentAgent == null)
        {
            throw new InvalidOperationException("ControlAgent is not set. Please set ControlAgent before adding to conversation.");
        }

        // Invoke the StartingExecution event to signal the beginning of the execution process
        StartingExecution?.Invoke();

        // Check if the cancellation token has been requested and reset it if necessary
        if (CancellationTokenSource.Token.IsCancellationRequested)
        {
            if (!Threading.TryResetCancellationTokenSource(CancellationTokenSource))
            {
                CancellationTokenSource.Dispose();
                CancellationTokenSource = new CancellationTokenSource();
            }
        }

       
        

        ChatMessage userMessage = new ChatMessage();
        //If userInput is not empty, create a new message item and add it to the conversation
        if (!string.IsNullOrEmpty(userInput))
        {
            userMessage = new ChatMessage(ChatMessageRoles.User, [new ChatMessagePart(userInput)]);
            // Check for any handoff agents that need to be processed before adding the user input
            if (CurrentResult == null)
            {
                await CheckForHandoff([userMessage]);
            }
            else
            {
                await CheckForHandoff([..CurrentResult.Messages, userMessage]);
            }

            string inputMessage = userInput;
            // If an input preprocessor is set, run it on the user input
            if (InputPreprocessor != null)
            {
                //Add in file content if provided
                if (message != null)
                {
                    // If the message is a file, we need to describe it
                    string originalInstructions = CurrentAgent.Instructions;
                    CurrentAgent.Instructions = "I need you to take the input file and describe the file/image. Be the eyes for the next step who cannot see the image but needs context from within the file/image" +
                                                "Be as descriptive as possible.";
                    Conversation fileDescription = await RunAsync(CurrentAgent, messages: [message], verboseCallback: InvokeVerboseCallback, cancellationToken: CancellationTokenSource);

                    //Restore the original instructions
                    CurrentAgent.Instructions = originalInstructions;
                        

                    if (fileDescription.Messages.Count > 0)
                    {
                        // If a file description was generated, we use it to preprocess the input
                        inputMessage = $"USER QUESTION: {userInput} \n\n With provided context for Included File: {fileDescription.Messages.Last().Content}";
                    }
                }

                string? preprocessedInput = await RunPreprocess(inputMessage);
                preprocessedInput = "The following CONTEXT has been prepocessed by an Agent tasked to process the input[may or may not be relevent]. <PREPOCESSED RESULTS>" + preprocessedInput + "</PREPOCESSED RESULTS>";
                // Create a system message with the preprocessed input
                userMessage.Parts.Add(new ChatMessagePart(preprocessedInput));
            }

            if (message != null)
            {
                //Add file to the conversation
                userMessage.Parts?.Add(message.Parts?.FirstOrDefault()!);
            }
        }
        if(CurrentResult != null)
        {
            //Run the ControlAgent with the current messages
            CurrentResult = await RunAsync(CurrentAgent, messages: [..CurrentResult.Messages], verboseCallback: HandleControllerVerboseEvent,
                streaming: streaming, streamingCallback: HandleControllerStreamingEvent, cancellationToken: CancellationTokenSource, responseId: string.IsNullOrEmpty(MainThreadId) ? "" : MainThreadId);
        }
        else
        {
            //Run the ControlAgent with the current messages
            CurrentResult = await RunAsync(CurrentAgent, messages: [userMessage], verboseCallback: HandleControllerVerboseEvent,
                streaming: streaming, streamingCallback: HandleControllerStreamingEvent, cancellationToken: CancellationTokenSource, responseId: string.IsNullOrEmpty(MainThreadId) ? "" : MainThreadId);
        }


        if (CurrentResult.MostRecentApiResult != null)
        {
            MainThreadId = CurrentResult.MostRecentApiResult.RequestId ?? MainThreadId;
        }

        //Trigger the FinishedExecution event to signal the end of the execution process
        FinishedExecution?.Invoke();

        return CurrentResult.Messages.Last().Content ?? "Error getting Response";
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
            return string.Join("\n",args ??["N/A"]);
        }

        //Invoke the InputPreprocessor delegate with the provided arguments
        Task task = (Task)InputPreprocessor?.DynamicInvoke(args)!;

        // Wait for the task to complete
        await task.ConfigureAwait(false);

        // Get the Result property from the Task
        return (string?)InputPreprocessor?.Method.ReturnType.GetProperty("Result")?.GetValue(task);
    }

    /// <summary>
    /// Adds a user's input to the conversation thread and returns the response.
    /// </summary>
    /// <param name="userInput">The input provided by the user to be added to the conversation.</param>
    /// <param name="threadId">The identifier of the conversation thread to which the input is added.</param>
    /// <param name="streaming">A value indicating whether the response should be streamed. Defaults to <see langword="true"/>.</param>
    /// <returns>A task representing the asynchronous operation, with a string result containing the response from the
    /// conversation.</returns>
    public async Task<string> AddToConversation(string userInput, string threadId, bool streaming = true)
    {
        MainThreadId = threadId;
        return await AddToConversation(userInput, streaming: streaming);
    }

    /// <summary>
    /// Adds a file to the conversation with the specified user input and file identifier.
    /// </summary>
    /// <remarks>This method reads the specified file from disk and adds it to the conversation as an
    /// image file content. The user input is included as text content in the same message.</remarks>
    /// <param name="userInput">The text input provided by the user to accompany the file.</param>
    /// <param name="fileId">The identifier of the file to be added to the conversation. This should be a valid path to the file on disk.</param>
    /// <param name="streaming">A boolean value indicating whether the operation should be performed in streaming mode. The default is <see
    /// langword="true"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string that identifies the
    /// message added to the conversation.</returns>
    public async Task<string> AddImageToConversation(string userInput, string fileId, bool streaming = true, string threadID = "")
    {
        if (!string.IsNullOrEmpty(threadID))
        {
            MainThreadId = threadID;
        }
        //import image from disk
        using (FileStream fileStream = new FileStream(fileId, FileMode.Open, FileAccess.Read))
        {
            byte[] data = new byte[fileStream.Length];
            await fileStream.ReadAsync(data, 0, (int)fileStream.Length);
            string base64EncodedData = Convert.ToBase64String(data.ToArray());
            string dataurl = $"data:image/{Path.GetExtension(fileId).Replace(".", "")};base64,{base64EncodedData}";


            ChatMessagePart imageContent = new ChatMessagePart(dataurl, ImageDetail.Auto);
            ChatMessage fileItem = new ChatMessage(ChatMessageRoles.User, [imageContent]);

            return await AddToConversation(userInput, fileItem, streaming: streaming);
        }
    }


    /// <summary>
    /// Adds a file to the conversation with the specified user input and file identifier.
    /// </summary>
    /// <remarks>This method reads the specified file from disk and adds it to the conversation as an
    /// image file content. The user input is included as text content in the same message.</remarks>
    /// <param name="userInput">The text input provided by the user to accompany the file.</param>
    /// <param name="fileId">The identifier of the file to be added to the conversation. This should be a valid path to the file on disk.</param>
    /// <param name="streaming">A boolean value indicating whether the operation should be performed in streaming mode. The default is <see
    /// langword="true"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string that identifies the
    /// message added to the conversation.</returns>
    public async Task<string> AddBase64ImageToConversation(string userInput, string base64, bool streaming = true, string threadID = "")
    {
        if(!string.IsNullOrEmpty(threadID))
        {
            MainThreadId = threadID;
        }
        ChatMessagePart imageContent = new ChatMessagePart(base64, ImageDetail.Auto);
        ChatMessage fileItem = new ChatMessage(ChatMessageRoles.User, [imageContent]);
        return await AddToConversation(userInput, fileItem, streaming: streaming);
    }

    /// <summary>
    /// Initiates a new conversation with the specified user input.
    /// </summary>
    /// <param name="userInput">The initial input provided by the user to start the conversation.</param>
    /// <param name="streaming">A boolean value indicating whether the conversation should be streamed.  true to enable streaming;
    /// otherwise, false.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string  representing the
    /// response to the user's input.</returns>
    public async Task<string> StartNewConversation(string userInput, bool streaming = true)
    {
        CurrentResult.Clear(); 
        MainThreadId = "";
        return await AddToConversation(userInput, streaming:streaming);
    }


    public async Task<string> StartNewConversation(string userInput, string base64, bool streaming = true)
    {
        CurrentResult.Clear();
        MainThreadId = "";
        ChatMessagePart imageContent = new ChatMessagePart(base64, ImageDetail.Auto);
        ChatMessage fileItem = new ChatMessage(ChatMessageRoles.User, [imageContent]);
        return await AddToConversation(userInput, fileItem, streaming: streaming);
    }
}