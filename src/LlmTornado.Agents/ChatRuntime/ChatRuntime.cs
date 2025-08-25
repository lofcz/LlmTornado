using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.ChatRuntime.Orchestration;
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

/// <summary>
/// Get an active chat runtime to manage conversations with AI agents, including orchestration and tool usage.
/// </summary>
public class ChatRuntime
{
    /// <summary>
    /// ID instance of the orchestration , used as a unique identifier for the agent in the API.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Main streaming event for the Control Agent to handle streaming messages for the Control Agent conversation.
    /// </summary>
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent;


    /// <summary>
    /// Gets or sets the runtime configuration for the application.
    /// </summary>
    public IRuntimeConfiguration RuntimeConfiguration { get; set; }

    /// <summary>
    /// Chat Orchestration to create processes flows with AI agents
    /// </summary>
    /// <param name="agentName"></param>
    /// <param name="configuration"></param>
    public ChatRuntime(IRuntimeConfiguration configuration)
    {
        RuntimeConfiguration = configuration;
        RuntimeConfiguration.Runtime = this;
        RuntimeConfiguration.OnRuntimeEvent = (rEvent) => { OnRuntimeEvent?.Invoke(rEvent); return Threading.ValueTaskCompleted; };
        RuntimeConfiguration.OnRuntimeInitialized();
    }


    /// <summary>
    /// Clears the messages, resets the main thread ID, and reinitializes the cancellation token source.
    /// </summary>
    /// <remarks>This method resets the state of the object by clearing the current result, setting the main
    /// thread ID to an empty string,  and ensuring the cancellation token source is reinitialized if it has been
    /// canceled.</remarks>
    public virtual void Clear()
    {
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
        RuntimeConfiguration.CancelRuntime();
        OnRuntimeEvent?.Invoke(new ChatRuntimeCancelledEvent(this.Id));
    }

    /// <summary>
    /// Sends a user message, with optional image data, to the conversation and returns the response asynchronously.
    /// </summary>
    /// <param name="message">The user message to send to the conversation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response string from the
    /// conversation.</returns>
    public async Task<ChatMessage> InvokeAsync(ChatMessage message)
    {
        // Invoke the StartingExecution event to signal the beginning of the execution process
        OnRuntimeEvent?.Invoke(new ChatRuntimeStartedEvent(this.Id));

        await RuntimeConfiguration.AddToChatAsync(message);

        OnRuntimeEvent?.Invoke(new ChatRuntimeCompletedEvent(this.Id));

        return RuntimeConfiguration.GetMessages().Last();
    }
}
