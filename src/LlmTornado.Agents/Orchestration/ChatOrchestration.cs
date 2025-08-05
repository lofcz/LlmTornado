using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
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
public delegate Task<string> InputProcessorDelegate(string input);

public class ChatOrchestration
{
    public string AgentName { get; }

    public string AgentId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Agent used to manage the active conversation and report results of the state machines.
    /// </summary>
    public TornadoAgent CurrentAgent { get; set; }

    /// <summary>
    /// Latest result from the ControlAgent run. (holds messages buffer for Chat API, response API uses threadID)
    /// </summary>
    public Conversation CurrentResult { get; set; }
    /// <summary>
    /// Thread ID of the main conversation thread for the response API
    /// </summary>
    public string MainThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Occurs when Agent gets a new message to process.
    /// </summary>
    public Action? OnExecutionStarted;

    /// <summary>
    /// Occurs when the execution process has completed.
    /// </summary>
    /// <remarks>Subscribe to this event to perform actions after the execution process finishes.  The
    /// event handler will be invoked when the execution is complete.</remarks>
    public Action? OnExecutionDone;

    /// <summary>
    /// Occurs when a verbose message related to the Control Agent  is generated.
    /// </summary>
    /// <remarks>This event is triggered to provide detailed logging information about the Control Agent
    /// Subscribers can use this event to capture and process verbose messages for diagnostic or logging
    /// purposes.</remarks>
    public Action<string>? OnVerboseEvent;

    /// <summary>
    /// Main streaming event for the Control Agent to handle streaming messages for the Control Agent conversation.
    /// </summary>
    public StreamingCallbacks? OnStreamingEvent;


    /// <summary>
    /// Master Cancellation token source for the Control Agent and the rest of the state machines.
    /// </summary>
    public CancellationTokenSource cts = new CancellationTokenSource();

    public ChatOrchestration(string agentName, TornadoAgent agent)
    {
        // Initialize the agent and set up the callbacks
        AgentName = agentName;
        CurrentAgent = agent; // Set the initial agent as the current agent
        CurrentAgent.Options.CancellationToken = cts.Token; // Set the cancellation token source for the Control Agent

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
        OnStreamingEvent?.Invoke(message);
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
        OnVerboseEvent?.Invoke(message);
        return default; // Return a completed ValueTask
    }


    /// <summary>
    /// Cancels the execution of all current state machines.
    /// </summary>
    /// <remarks>This method signals a cancellation request to all state machines currently managed by
    /// this instance. It stops each state machine and cancels any ongoing operations. Ensure that the state
    /// machines can handle cancellation requests appropriately.</remarks>
    public virtual void CancelExecution()
    {
        cts.Cancel(); // Signal cancellation to all state machines
    }

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
    public async Task<string> AddToConversation(string userInput, ChatMessage? message = null, bool streaming = true)
    {
        // Ensure that the ControlAgent is set before proceeding
        if (CurrentAgent == null)
        {
            throw new InvalidOperationException("ControlAgent is not set. Please set ControlAgent before adding to conversation.");
        }

        // Invoke the StartingExecution event to signal the beginning of the execution process
        OnExecutionStarted?.Invoke();

        // Check if the cancellation token has been requested and reset it if necessary
        if (cts.Token.IsCancellationRequested)
        {
            if (!Threading.TryResetCancellationTokenSource(cts))
            {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
        }
        List<ChatMessage> messages = new List<ChatMessage>();

        if (CurrentResult != null)
        {
            if (CurrentResult.Messages.Count > 0)
            {
                // If there are existing messages, append the new user message to the existing messages
                messages.AddRange(CurrentResult.Messages);
            }
        }

        if (message != null)
        {
            // If a message is provided, add it to the messages list
            messages.Add(message);
        }
        else if (!string.IsNullOrEmpty(userInput))
        {
            messages.Add(new ChatMessage(ChatMessageRoles.User, [new ChatMessagePart(userInput)]));
        }

        //Run the ControlAgent with the current messages
        CurrentResult = await RunAsync(CurrentAgent, messages: messages, verboseCallback: HandleControllerVerboseEvent,
            streaming: streaming, streamingCallback: HandleControllerStreamingEvent, cancellationToken: cts.Token, responseId: string.IsNullOrEmpty(MainThreadId) ? "" : MainThreadId);

        if (CurrentResult.MostRecentApiResult != null)
        {
            MainThreadId = CurrentResult.MostRecentApiResult.RequestId ?? MainThreadId;
        }

        //Trigger the FinishedExecution event to signal the end of the execution process
        OnExecutionDone?.Invoke();

        return CurrentResult.Messages.Last().Content ?? "Error getting Response";
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
    /// <param name="filePath">The identifier of the file to be added to the conversation. This should be a valid path to the file on disk.</param>
    /// <param name="streaming">A boolean value indicating whether the operation should be performed in streaming mode. The default is <see
    /// langword="true"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string that identifies the
    /// message added to the conversation.</returns>
    public async Task<string> AddImageToConversation(string userInput, string filePath, bool streaming = true, string threadID = "")
    {
        if (!string.IsNullOrEmpty(threadID))
        {
            MainThreadId = threadID;
        }

        //import image from disk
#if MODERN
        await using FileStream fileStream = new FileStream(fileId, FileMode.Open, FileAccess.Read);
#else
        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
#endif
        byte[] data = new byte[fileStream.Length];
        _ = await fileStream.ReadAsync(data, 0, (int)fileStream.Length);
        string base64EncodedData = Convert.ToBase64String(data.ToArray());
        string dataurl = $"data:image/{Path.GetExtension(filePath).Replace(".", "")};base64,{base64EncodedData}";

        ChatMessagePart imageContent = new ChatMessagePart(dataurl, ImageDetail.Auto);
        ChatMessagePart chatMessagePart = new ChatMessagePart(userInput);
        ChatMessage message = new ChatMessage(ChatMessageRoles.User, [chatMessagePart, imageContent]);

        return await AddToConversation(userInput, message, streaming: streaming);
    }

    /// <summary>
    /// Adds a file to the conversation with the specified user input and file identifier.
    /// </summary>
    /// <remarks>This method reads the specified file from disk and adds it to the conversation as an
    /// image file content. The user input is included as text content in the same message.</remarks>
    /// <param name="userInput">The text input provided by the user to accompany the file.</param>
    /// <param name="base64">Base64 encoded image.</param>
    /// <param name="streaming">A boolean value indicating whether the operation should be performed in streaming mode. The default is <see
    /// langword="true"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string that identifies the
    /// message added to the conversation.</returns>
    public async Task<string> AddBase64ImageToConversation(string userInput, string base64, bool streaming = true, string threadID = "")
    {
        if (!string.IsNullOrEmpty(threadID))
        {
            MainThreadId = threadID;
        }

        ChatMessagePart imageContent = new ChatMessagePart(base64, ImageDetail.Auto);
        ChatMessagePart chatMessagePart = new ChatMessagePart(userInput);
        ChatMessage message = new ChatMessage(ChatMessageRoles.User, [chatMessagePart, imageContent]);
        return await AddToConversation(userInput, message, streaming: streaming);
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
        return await AddToConversation(userInput, streaming: streaming);
    }

    public async Task<string> StartNewConversation(string userInput, string base64, bool streaming = true)
    {
        CurrentResult.Clear();
        MainThreadId = "";
        ChatMessagePart imageContent = new ChatMessagePart(base64, ImageDetail.Auto);
        ChatMessagePart chatMessagePart = new ChatMessagePart(userInput);
        ChatMessage message = new ChatMessage(ChatMessageRoles.User, [chatMessagePart, imageContent]);
        return await AddToConversation(userInput, message, streaming: streaming);
    }
}
