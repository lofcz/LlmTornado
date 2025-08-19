using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Orchestration;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Images;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime;

public class ChatRuntime
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
    /// Main streaming event for the Control Agent to handle streaming messages for the Control Agent conversation.
    /// </summary>
    public Func<ModelStreamingEvents, ValueTask>? OnStreamingEvent;


    /// <summary>
    /// Master Cancellation token source for the Control Agent and the rest of the state machines.
    /// </summary>
    public CancellationTokenSource cts = new CancellationTokenSource();


    /// <summary>
    /// History of the chat conversation.
    /// </summary>
    public ConcurrentStack<ChatMessage> ChatHistory { get; private set; } = new ConcurrentStack<ChatMessage>();

    public AgentOrchestration Orchestrator { get; set; }


    /// <summary>
    /// Chat Orchestration to create processes flows with AI agents
    /// </summary>
    /// <param name="agentName"></param>
    /// <param name="orchestrator"></param>
    public ChatRuntime(string agentName, AgentOrchestration orchestrator)
    {
        // Initialize the agent and set up the callbacks
        AgentName = agentName;
        Orchestrator = orchestrator;
        SetupCallbacks();
    }

    private void SetupCallbacks()
    {
        foreach(var runnable in Orchestrator.Runnables.Values)
        {
            if(runnable is RunnableAgent agentRunnable)
            {
                agentRunnable.SubscribeStreamingChannel(HandleStreamingEvent);
            }
        }
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
        return Threading.ValueTaskCompleted;
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
        ChatHistory = new ConcurrentStack<ChatMessage>();
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
        Orchestrator.CancelRuntime();
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
    public async Task<string> InvokeAsync(ChatMessage message)
    {
        // Invoke the StartingExecution event to signal the beginning of the execution process
        OnExecutionStarted?.Invoke();

        //Run the ControlAgent with the current messages
        await InvokeConversation(message);

        OnExecutionDone?.Invoke();

        return ChatHistory.Last().Content ?? "Error getting Response";
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

    private async Task InvokeConversation(ChatMessage message)
    {
        ChatHistory.Push(message);

        ChatMessage response =  await InternalOnInvokeAgentsAsync(message);

        ChatHistory.Push(response);
    }

    private async ValueTask<ChatMessage> InternalOnInvokeAgentsAsync(ChatMessage message)
    {
        ResetCancellationTokenSource();
        
        await Orchestrator.Invoke(message);

        return Orchestrator.Results.LastOrDefault() ?? new ChatMessage();
    }
}
