using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.StateMachines;
using static LlmTornado.Agents.TornadoRunner;

namespace LlmTornado.Agents.Orchestration;


public class ChatOrchestration
{
    /// <summary>
    /// Name of the Agent defined by this orchestration.
    /// </summary>
    public string AgentName { get; }

    /// <summary>
    /// ID instance of the orchestration , used as a unique identifier for the agent in the API.
    /// </summary>
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
    public Func<ModelStreamingEvents, ValueTask>? OnStreamingEvent;


    /// <summary>
    /// Master Cancellation token source for the Control Agent and the rest of the state machines.
    /// </summary>
    public CancellationTokenSource cts = new CancellationTokenSource();

    /// <summary>
    /// Chat Orchestration to create processes flows with AI agents
    /// </summary>
    /// <param name="agentName"></param>
    /// <param name="agent"></param>
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
    private ValueTask HandleStreamingEvent(ModelStreamingEvents message)
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
    private ValueTask HandleVerboseEvent(string message)
    {
        OnVerboseEvent?.Invoke(message);
        return default; // Return a completed ValueTask
    }

    /// <summary>
    /// Clears the messages, resets the main thread ID, and reinitializes the cancellation token source.
    /// </summary>
    /// <remarks>This method resets the state of the object by clearing the current result, setting the main
    /// thread ID to an empty string,  and ensuring the cancellation token source is reinitialized if it has been
    /// canceled.</remarks>
    public virtual void Clear()
    {
        // Clear the current result and reset the main thread ID
        CurrentResult = null;
        MainThreadId = string.Empty;
        ResetCancellationTokenSource();
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
    /// Used to customize the behavior of the orchestration when invoked for custom Orchestration methods to extend the Orchestration library.
    /// </summary>
    /// <param name="userInput">The user's input message to process. Cannot be <see langword="null"/>.</param>
    /// <param name="streaming"><see langword="true"/> to enable streaming of the response; otherwise, <see langword="false"/> to return the
    /// complete response after processing.</param>
    /// <param name="base64Image">An optional base64-encoded image to include with the user input. May be <see langword="null"/> if no image is
    /// provided.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
    /// cref="ChatMessagePart"/> objects representing the response, or <see langword="null"/> if no response is
    /// generated.</returns>
    internal virtual async Task<List<ChatMessagePart>?> OnInvokedAsync(string userInput, bool streaming = true, string ? base64Image = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sends a user message, with optional image data, to the conversation and returns the response asynchronously.
    /// </summary>
    /// <param name="userInput">The text input from the user to include in the message. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="streaming"><see langword="true"/> to enable streaming of the response as it is generated; <see langword="false"/> to
    /// receive the complete response after processing.</param>
    /// <param name="base64Image">An optional base64-encoded image string to include with the message. If <see langword="null"/> or empty, no
    /// image is attached.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response string from the
    /// conversation.</returns>
    public async Task<string> InvokeAsync(string userInput, bool streaming = true, string? base64Image = null)
    {
        // Invoke the StartingExecution event to signal the beginning of the execution process
        OnExecutionStarted?.Invoke();

       ChatMessage? inputMessage = await CreateInputMessage(userInput, streaming, base64Image)!;

        //Run the ControlAgent with the current messages
        await InvokeConversation(CurrentAgent, messages: new List<ChatMessage>() { inputMessage },
            verboseCallback: HandleVerboseEvent, streaming: streaming,
            streamingCallback: HandleStreamingEvent, cancellationToken: cts.Token);

        OnExecutionDone?.Invoke();

        return CurrentResult.Messages.Last().Content ?? "Error getting Response";
    }

    private async Task<ChatMessage>? CreateInputMessage(string userInput, bool streaming = true, string? base64Image = null)
    {
        List<ChatMessagePart> parts = [new ChatMessagePart(userInput)];

        if (base64Image is not null && !string.IsNullOrEmpty(base64Image))
        {
            parts.Add(new ChatMessagePart(base64Image, ImageDetail.Auto));
        }

        List<ChatMessagePart> processParts = await OnInvokedAsync(userInput, streaming, base64Image) ?? [];

        parts.AddRange(processParts);

        return new ChatMessage(ChatMessageRoles.User, parts);
    }

    private void ResetCancellationTokenSource()
    {
        // Reset the cancellation token source if it has been canceled
        if (cts.IsCancellationRequested)
        {
            if (!Threading.TryResetCancellationTokenSource(cts))
            {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
        }
    }


    private async Task InvokeConversation(TornadoAgent agent, List<ChatMessage>? messages = null, 
        Func<string, ValueTask>? verboseCallback = null, bool streaming = true, 
        Func<ModelStreamingEvents, ValueTask>? streamingCallback = null, 
        CancellationToken cancellationToken = default, string responseId = "")
    {
        // Ensure that the ControlAgent is set before proceeding
        if (CurrentAgent == null)
        {
            throw new InvalidOperationException("ControlAgent is not set. Please set ControlAgent before adding to conversation.");
        }

        ResetCancellationTokenSource();

        //Run the ControlAgent with the current messages
        CurrentResult = await RunAsync(CurrentAgent, conversation: CurrentResult, messages: messages, verboseCallback: HandleVerboseEvent,
            streaming: streaming, streamingCallback: HandleStreamingEvent, cancellationToken: cts.Token, responseId: string.IsNullOrEmpty(MainThreadId) ? "" : MainThreadId);

        if (CurrentResult.MostRecentApiResult != null)
        {
            MainThreadId = CurrentResult.MostRecentApiResult.RequestId ?? MainThreadId;
        }
    }
}
