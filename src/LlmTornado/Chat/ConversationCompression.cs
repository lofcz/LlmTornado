using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
///     Options for conversation compression.
/// </summary>
public class ConversationCompressionOptions
{
    /// <summary>
    ///     The approximate character count per chunk (default: 10000).
    /// </summary>
    public int ChunkSize { get; set; } = 10000;

    /// <summary>
    ///     Whether to preserve system messages (default: true).
    /// </summary>
    public bool PreserveSystemMessages { get; set; } = true;

    /// <summary>
    ///     Whether to Compress Tool call messages (default: true).
    /// </summary>
    public bool CompressToolCallMessages { get; set; } = true;

    /// <summary>
    ///     The model to use for summarization.
    /// </summary>
    public ChatModel? SummaryModel { get; set; }

    /// <summary>
    ///     Custom prompt for summarization.
    /// </summary>
    public string SummaryPrompt { get; set; } = "Summarize this chat conversation concisely, preserving key information:";

    /// <summary>
    ///     Maximum tokens for each summary (default: 1000).
    /// </summary>
    public int MaxSummaryTokens { get; set; } = 1000;
}

/// <summary>
///     Strategy for determining when conversation compression should occur.
/// </summary>
public interface IConversationCompressionStrategy
{
    /// <summary>
    ///     Determines if compression should occur based on the current conversation state.
    /// </summary>
    /// <param name="conversation">The conversation to check</param>
    /// <returns>True if compression should occur, false otherwise</returns>
    bool ShouldCompress(Conversation conversation);

    /// <summary>
    ///     Gets the compression options to use.
    /// </summary>
    /// <param name="conversation">The conversation being compressed</param>
    /// <returns>Compression options</returns>
    ConversationCompressionOptions GetCompressionOptions(Conversation conversation);
}

/// <summary>
///     Handles the summarization of conversation messages.
/// </summary>
public interface IConversationSummarizer
{
    /// <summary>
    ///     Summarizes a list of messages into one or more summary messages.
    /// </summary>
    /// <param name="messages">Messages to summarize</param>
    /// <param name="options">Compression options</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of summary messages</returns>
    Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage> messages, ConversationCompressionOptions options, CancellationToken token = default);
}

/// <summary>
///     Adaptive compression strategy that considers multiple factors.
/// </summary>
public class TornadoCompressionStrategy : IConversationCompressionStrategy
{
    private readonly int messageThreshold;
    private readonly int characterThreshold;
    private readonly ConversationCompressionOptions options;

    /// <summary>
    ///     Creates a new adaptive compression strategy.
    /// </summary>
    /// <param name="messageThreshold">Message count threshold (default: 20)</param>
    /// <param name="characterThreshold">Character count threshold (default: 50000)</param>
    /// <param name="options">Optional custom compression options</param>
    public TornadoCompressionStrategy(
        int messageThreshold = 20,
        int characterThreshold = 50000,
        ConversationCompressionOptions? options = null)
    {
        this.messageThreshold = messageThreshold;
        this.characterThreshold = characterThreshold;
        this.options = options ?? new ConversationCompressionOptions();
    }

    /// <summary>
    /// Determines whether the specified conversation should be compressed based on message count and character length.
    /// </summary>
    /// <remarks>The method evaluates the conversation against predefined thresholds for the number of
    /// messages and the total character count. Compression is recommended if either threshold is exceeded.</remarks>
    /// <param name="conversation">The conversation to evaluate for compression.</param>
    /// <returns><see langword="true"/> if the conversation should be compressed; otherwise, <see langword="false"/>.</returns>
    public bool ShouldCompress(Conversation conversation)
    {
        int messageCount = conversation.Messages.Count;
        int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));

        // Compress if either threshold is exceeded
        return messageCount > messageThreshold || totalChars > characterThreshold;
    }


    public ConversationCompressionOptions GetCompressionOptions(Conversation conversation)
    {
        // Adapt compression options based on conversation size
        ConversationCompressionOptions adaptedOptions = new ConversationCompressionOptions
        {
            ChunkSize = options.ChunkSize,
            PreserveSystemMessages = options.PreserveSystemMessages,
            SummaryModel = options.SummaryModel,
            SummaryPrompt = options.SummaryPrompt,
            MaxSummaryTokens = options.MaxSummaryTokens
        };

        int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));

        return adaptedOptions;
    }
}

/// <summary>
///     Compression strategy based on message count.
/// </summary>
public class MessageCountCompressionStrategy : IConversationCompressionStrategy
{
    private readonly int messageThreshold;
    private readonly ConversationCompressionOptions options;

    /// <summary>
    ///     Creates a new message count compression strategy.
    /// </summary>
    /// <param name="messageThreshold">Compress when message count exceeds this threshold (default: 20)</param>
    /// <param name="options">Optional custom compression options</param>
    public MessageCountCompressionStrategy(int messageThreshold = 20, ConversationCompressionOptions? options = null)
    {
        this.messageThreshold = messageThreshold;
        this.options = options ?? new ConversationCompressionOptions();
    }

    public bool ShouldCompress(Conversation conversation)
    {
        return conversation.Messages.Count > messageThreshold;
    }

    public ConversationCompressionOptions GetCompressionOptions(Conversation conversation)
    {
        return options;
    }
}

/// <summary>
///     Compression strategy based on character count.
/// </summary>
public class CharacterCountCompressionStrategy : IConversationCompressionStrategy
{
    private readonly int characterThreshold;
    private readonly ConversationCompressionOptions options;

    /// <summary>
    ///     Creates a new character count compression strategy.
    /// </summary>
    /// <param name="characterThreshold">Compress when total character count exceeds this threshold (default: 50000)</param>
    /// <param name="options">Optional custom compression options</param>
    public CharacterCountCompressionStrategy(int characterThreshold = 50000, ConversationCompressionOptions? options = null)
    {
        this.characterThreshold = characterThreshold;
        this.options = options ?? new ConversationCompressionOptions();
    }

    public bool ShouldCompress(Conversation conversation)
    {
        int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));
        return totalChars > characterThreshold;
    }

    public ConversationCompressionOptions GetCompressionOptions(Conversation conversation)
    {
        return options;
    }
}

/// <summary>
///     Compression strategy based on periodic intervals.
/// </summary>
public class PeriodicCompressionStrategy : IConversationCompressionStrategy
{
    private readonly int interval;
    private readonly ConversationCompressionOptions options;
    private int messagesSinceLastCompression;

    /// <summary>
    ///     Creates a new periodic compression strategy.
    /// </summary>
    /// <param name="interval">Compress every N messages (default: 10)</param>
    /// <param name="options">Optional custom compression options</param>
    public PeriodicCompressionStrategy(int interval = 10, ConversationCompressionOptions? options = null)
    {
        this.interval = interval;
        this.options = options ?? new ConversationCompressionOptions();
        messagesSinceLastCompression = 0;
    }

    public bool ShouldCompress(Conversation conversation)
    {
        messagesSinceLastCompression++;

        if (messagesSinceLastCompression >= interval)
        {
            messagesSinceLastCompression = 0;
            return true;
        }

        return false;
    }

    public ConversationCompressionOptions GetCompressionOptions(Conversation conversation)
    {
        return options;
    }
}

/// <summary>
///     Adaptive compression strategy that considers multiple factors.
/// </summary>
public class AdaptiveCompressionStrategy : IConversationCompressionStrategy
{
    private readonly int messageThreshold;
    private readonly int characterThreshold;
    private readonly ConversationCompressionOptions options;

    /// <summary>
    ///     Creates a new adaptive compression strategy.
    /// </summary>
    /// <param name="messageThreshold">Message count threshold (default: 20)</param>
    /// <param name="characterThreshold">Character count threshold (default: 50000)</param>
    /// <param name="options">Optional custom compression options</param>
    public AdaptiveCompressionStrategy(
        int messageThreshold = 20,
        int characterThreshold = 50000,
        ConversationCompressionOptions? options = null)
    {
        this.messageThreshold = messageThreshold;
        this.characterThreshold = characterThreshold;
        this.options = options ?? new ConversationCompressionOptions();
    }

    public bool ShouldCompress(Conversation conversation)
    {
        int messageCount = conversation.Messages.Count;
        int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));

        // Compress if either threshold is exceeded
        return messageCount > messageThreshold || totalChars > characterThreshold;
    }

    public ConversationCompressionOptions GetCompressionOptions(Conversation conversation)
    {
        // Adapt compression options based on conversation size
        ConversationCompressionOptions adaptedOptions = new ConversationCompressionOptions
        {
            ChunkSize = options.ChunkSize,
            PreserveSystemMessages = options.PreserveSystemMessages,
            SummaryModel = options.SummaryModel,
            SummaryPrompt = options.SummaryPrompt,
            MaxSummaryTokens = options.MaxSummaryTokens
        };

        int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));

        return adaptedOptions;
    }
}

/// <summary>
///     Default implementation of conversation summarizer using chunked parallel summarization.
/// </summary>
public class DefaultConversationSummarizer : IConversationSummarizer
{
    private readonly ChatEndpoint endpoint;
    private readonly ChatRequest requestParameters;

    /// <summary>
    ///     Creates a new default conversation summarizer.
    /// </summary>
    /// <param name="endpoint">Chat endpoint for API calls</param>
    /// <param name="requestParameters">Request parameters to use as template</param>
    public DefaultConversationSummarizer(ChatEndpoint endpoint, ChatRequest requestParameters)
    {
        this.endpoint = endpoint;
        this.requestParameters = requestParameters;
    }

    public async Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage> messages, ConversationCompressionOptions options, CancellationToken token = default)
    {
        // Group messages into chunks based on character count
        List<List<ChatMessage>> chunks = [];
        List<ChatMessage> currentChunk = [];
        int currentChunkLength = 0;

        foreach (ChatMessage msg in messages)
        {
            int msgLength = Conversation.GetMessageLength(msg);

            if (currentChunkLength + msgLength > options.ChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(currentChunk);
                currentChunk = [];
                currentChunkLength = 0;
            }

            currentChunk.Add(msg);
            currentChunkLength += msgLength;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(currentChunk);
        }

        // Process chunks in parallel to get summaries
        Task<string>[] summaryTasks = chunks.Select(chunk => SummarizeChunk(chunk, options, token)).ToArray();
        string[] summaries = await Task.WhenAll(summaryTasks);

        // Convert summaries to messages
        List<ChatMessage> summaryMessages = [];

        foreach (string summary in summaries)
        {
            if (!summary.IsNullOrWhiteSpace())
            {
                summaryMessages.Add(new ChatMessage(ChatMessageRoles.Assistant, $"[Previous conversation summary]: {summary}"));
            }
        }

        return summaryMessages;
    }

    private async Task<string> SummarizeChunk(List<ChatMessage> chunk, ConversationCompressionOptions options, CancellationToken token)
    {
        // Build the text representation of the chunk
        StringBuilder chunkText = new StringBuilder();

        foreach (ChatMessage msg in chunk)
        {
            string roleStr = msg.Role switch
            {
                ChatMessageRoles.System => "System",
                ChatMessageRoles.User => "User",
                ChatMessageRoles.Assistant => "Assistant",
                ChatMessageRoles.Tool => "Tool",
                _ => "Unknown"
            };

            chunkText.AppendLine($"{roleStr}: {Conversation.GetMessageContent(msg)}");
        }

        // Create a temporary chat request for summarization
        ChatRequest summarizeRequest = new ChatRequest(null, requestParameters)
        {
            Model = options.SummaryModel,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, options.SummaryPrompt),
                new ChatMessage(ChatMessageRoles.User, chunkText.ToString())
            ],
            MaxTokens = options.MaxSummaryTokens,
            Temperature = 0.3, // Lower temperature for more consistent summaries
            CancellationToken = token
        };

        try
        {
            ChatResult? result = await endpoint.CreateChatCompletion(summarizeRequest);
            return result?.Choices?[0]?.Message?.Content ?? string.Empty;
        }
        catch (Exception)
        {
            // If summarization fails, return a simple placeholder
            return $"[{chunk.Count} messages from this conversation]";
        }
    }
}