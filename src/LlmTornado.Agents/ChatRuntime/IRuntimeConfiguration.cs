using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;

namespace LlmTornado.Agents.ChatRuntime;

/// <summary>
/// Controller for the runtime configuration of the chat runtime.
/// </summary>
public interface IRuntimeConfiguration
{
    /// <summary>
    /// Reference to the active ChatRuntime
    /// </summary>
    public ChatRuntime Runtime { get; set; }
    /// <summary>
    /// Add new message to the chat conversation.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runtime event handler for runtime events.
    /// </summary>
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }

    /// <summary>
    ///  Runtime event handler for tool permission handling.
    /// </summary>
    public Func<string, ValueTask<bool>>? OnRuntimeRequestEvent { get; set; }

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


    /// <summary>
    /// Runtime Initialize event.
    /// </summary>
    public void OnRuntimeInitialized();

    /// <summary>
    /// Trigger Cancellation of the runtime.
    /// </summary>
    public void CancelRuntime();
}
