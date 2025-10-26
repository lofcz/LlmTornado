using LlmTornado.Chat;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
///     Strategy for determining when messages compression should occur.
/// </summary>
public interface IMessagesCompressionStrategy
{
    /// <summary>
    ///     Determines if compression should occur based on the current messages state.
    /// </summary>
    /// <param name="messages">The messages to check</param>
    /// <returns>True if compression should occur, false otherwise</returns>
    bool ShouldCompress(List<ChatMessage> messages);

    /// <summary>
    ///     Gets the compression options to use.
    /// </summary>
    /// <param name="messages">The messages being compressed</param>
    /// <returns>Compression options</returns>
    MessageCompressionOptions GetCompressionOptions(List<ChatMessage>messages);
}
