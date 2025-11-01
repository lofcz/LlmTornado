using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
///     Adaptive compression strategy that considers multiple factors.
/// </summary>
public class TornadoCompressionStrategy : IMessagesCompressionStrategy
{
    private readonly int messageThreshold;
    private readonly int characterThreshold;
    private  MessageCompressionOptions options;

    /// <summary>
    ///     Creates a new adaptive compression strategy.
    /// </summary>
    /// <param name="messageThreshold">Message count threshold (default: 20)</param>
    /// <param name="characterThreshold">Character count threshold (default: 50000)</param>
    /// <param name="options">Optional custom compression options</param>
    public TornadoCompressionStrategy(
        int messageThreshold = 20,
        int characterThreshold = 50000,
        MessageCompressionOptions? options = null)
    {
        this.messageThreshold = messageThreshold;
        this.characterThreshold = characterThreshold;
        this.options = options ?? new MessageCompressionOptions();
    }

    /// <summary>
    /// Determines whether the specified messages should be compressed based on message count and character length.
    /// </summary>
    /// <remarks>The method evaluates the messages against predefined thresholds for the number of
    /// messages and the total character count. Compression is recommended if either threshold is exceeded.</remarks>
    /// <param name="messages">The messages to evaluate for compression.</param>
    /// <returns><see langword="true"/> if the messages should be compressed; otherwise, <see langword="false"/>.</returns>
    public bool ShouldCompress(List<ChatMessage> messages)
    {
        int messageCount = messages.Count;
        int totalChars = messages.Sum(m => m.GetMessageTokens());

        // Compress if either threshold is exceeded
        return messageCount > messageThreshold || totalChars > characterThreshold;
    }


    public MessageCompressionOptions GetCompressionOptions(List<ChatMessage>messages)
    {
        // Adapt compression options based on messages size
        MessageCompressionOptions adaptedOptions = new MessageCompressionOptions
        {
            ChunkSize = options.ChunkSize,
            PreserveSystemmessages = options.PreserveSystemmessages,
            SummaryModel = options.SummaryModel,
            SummaryPrompt = options.SummaryPrompt,
            MaxSummaryTokens = options.MaxSummaryTokens
        };

        int totalChars = messages.Sum(m => m.GetMessageTokens());
        options = adaptedOptions;
        return adaptedOptions;
    }

    public MessageCompressionOptions? GetCompressionOptions()
    {
        return options;
    }
}
