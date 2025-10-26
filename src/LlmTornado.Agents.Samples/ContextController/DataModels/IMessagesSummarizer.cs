using LlmTornado.Chat;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
///     Handles the summarization of messages messages.
/// </summary>
public interface IMessagesSummarizer
{
    /// <summary>
    ///     Summarizes a list of messages into one or more summary messages.
    /// </summary>
    /// <param name="messages">messages to summarize</param>
    /// <param name="options">Compression options</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of summary messages</returns>
    Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage>messages, MessageCompressionOptions options, CancellationToken token = default);
}
