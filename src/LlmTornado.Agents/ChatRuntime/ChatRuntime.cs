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
    /// ID instance of the orchestration , used as a unique identifier for the agent in the API.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Thread ID of the main conversation thread for the response API
    /// </summary>
    //public string MainThreadId { get; set; } = string.Empty;

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

    ///// <summary>
    ///// History of the chat conversation.
    ///// </summary>
    //public ConcurrentStack<ChatMessage> ChatHistory { get; private set; } = new ConcurrentStack<ChatMessage>();

    public IRuntimeConfiguration RuntimeConfiguration { get; set; }

    /// <summary>
    /// Chat Orchestration to create processes flows with AI agents
    /// </summary>
    /// <param name="agentName"></param>
    /// <param name="configuration"></param>
    public ChatRuntime(IRuntimeConfiguration configuration)
    {
        RuntimeConfiguration = configuration;
        RuntimeConfiguration.cts = cts; 
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
        // ChatHistory = new ConcurrentStack<ChatMessage>();
        // MainThreadId = string.Empty;
        ResetCancellationTokenSource();
        RuntimeConfiguration.ClearMessages();
    }

    /// <summary>
    /// Cancels the execution of all current state machines.
    /// </summary>
    /// <remarks>This method signals a cancellation request to all state machines currently managed by
    /// this instance. It stops each state machine and cancels any ongoing operations. Ensure that the state
    /// machines can handle cancellation requests appropriately.</remarks>
    public void CancelExecution()
    {
        cts.Cancel(); // Signal cancellation to all state machines
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
    public async Task<ChatMessage> InvokeAsync(ChatMessage message)
    {
        // Invoke the StartingExecution event to signal the beginning of the execution process
        OnExecutionStarted?.Invoke();

        ResetCancellationTokenSource();

        await RuntimeConfiguration.AddToChatAsync(message);

        OnExecutionDone?.Invoke();

        return RuntimeConfiguration.GetMessages().Last();
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
}
