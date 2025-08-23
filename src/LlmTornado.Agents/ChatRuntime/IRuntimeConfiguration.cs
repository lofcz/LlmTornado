using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime;

/// <summary>
/// Controller for the runtime configuration of the chat runtime.
/// </summary>
public interface IRuntimeConfiguration
{
    /// <summary>
    /// Add new message to the chat conversation.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancellation token source for the entire runtime configuration.
    /// </summary>
    public CancellationTokenSource cts { get; set; }

    /// <summary>
    /// Runtime event handler for runtime events.
    /// </summary>
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }

    /// <summary>
    /// Gets the current list of messages in the conversation.
    /// </summary>
    /// <returns></returns>
    public List<ChatMessage> GetMessages();

    /// <summary>
    /// Get the last message in the conversation.
    /// </summary>
    /// <returns></returns>
    public ChatMessage GetLastMessage();

    /// <summary>
    /// Clears all messages from the conversation.
    /// </summary>
    public void ClearMessages();
}
