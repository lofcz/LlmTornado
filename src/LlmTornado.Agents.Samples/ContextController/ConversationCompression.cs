using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
///     Options for messages compression.
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
    public bool PreserveSystemmessages { get; set; } = true;

    /// <summary>
    ///     Whether to Compress Tool call messages (default: true).
    /// </summary>
    public bool CompressToolCallmessages { get; set; } = true;

    /// <summary>
    ///     The model to use for summarization.
    /// </summary>
    public ChatModel? SummaryModel { get; set; }

    /// <summary>
    ///     Custom prompt for summarization.
    /// </summary>
    public string SummaryPrompt { get; set; } = "Summarize this chat messages concisely, preserving key information:";

    /// <summary>
    ///     Maximum tokens for each summary (default: 1000).
    /// </summary>
    public int MaxSummaryTokens { get; set; } = 1000;
}

/// <summary>
///     Strategy for determining when messages compression should occur.
/// </summary>
public interface IConversationCompressionStrategy
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
    ConversationCompressionOptions GetCompressionOptions(List<ChatMessage>messages);
}

/// <summary>
///     Handles the summarization of messages messages.
/// </summary>
public interface IConversationSummarizer
{
    /// <summary>
    ///     Summarizes a list of messages into one or more summary messages.
    /// </summary>
    /// <param name="messages">messages to summarize</param>
    /// <param name="options">Compression options</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of summary messages</returns>
    Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage>messages, ConversationCompressionOptions options, CancellationToken token = default);
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
    /// Determines whether the specified messages should be compressed based on message count and character length.
    /// </summary>
    /// <remarks>The method evaluates the messages against predefined thresholds for the number of
    /// messages and the total character count. Compression is recommended if either threshold is exceeded.</remarks>
    /// <param name="messages">The messages to evaluate for compression.</param>
    /// <returns><see langword="true"/> if the messages should be compressed; otherwise, <see langword="false"/>.</returns>
    public bool ShouldCompress(List<ChatMessage> messages)
    {
        int messageCount = messages.Count;
        int totalChars = messages.Sum(m => m.GetMessageLength());

        // Compress if either threshold is exceeded
        return messageCount > messageThreshold || totalChars > characterThreshold;
    }


    public ConversationCompressionOptions GetCompressionOptions(List<ChatMessage>messages)
    {
        // Adapt compression options based on messages size
        ConversationCompressionOptions adaptedOptions = new ConversationCompressionOptions
        {
            ChunkSize = options.ChunkSize,
            PreserveSystemmessages = options.PreserveSystemmessages,
            SummaryModel = options.SummaryModel,
            SummaryPrompt = options.SummaryPrompt,
            MaxSummaryTokens = options.MaxSummaryTokens
        };

        int totalChars = messages.Sum(m => m.GetMessageLength());

        return adaptedOptions;
    }
}


/// <summary>
///     Default implementation of messages summarizer using chunked parallel summarization.
/// </summary>
public class TornadoMessageSummarizer : IConversationSummarizer
{
    public TornadoApi Client { get; set; }
    public TornadoMessageSummarizer(TornadoApi client)
    {
        Client = client;
    }
    public async Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage> messages, ConversationCompressionOptions options, CancellationToken token = default)
    {
        // Separate system messages if preserving them
        ConversationContent content = ConversationContent.SortContent(messages, options);
        // Group messages into chunks based on character count
        List<List<ChatMessage>> chunks = [];
        List<ChatMessage> currentChunk = [];
        int currentChunkLength = 0;

        foreach (ChatMessage msg in content.MessagesToCompress)
        {
            int msgLength = msg.GetMessageLength();

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
        Task<string>[] summaryTasks = chunks.Select(chunk => SummarizeChunk(Client, chunk, options, token)).ToArray();
        string[] summaries = await Task.WhenAll(summaryTasks);

        // Convert summaries to messages
        List<ChatMessage> summarymessages = [];

        foreach (string summary in summaries)
        {
            if (!summary.IsNullOrWhiteSpace())
            {
                summarymessages.Add(new ChatMessage(ChatMessageRoles.Assistant, $"[Previous messages summary]: {summary}"));
            }
        }

        return summarymessages;
    }

    private async Task<string> SummarizeChunk(TornadoApi client, List<ChatMessage> chunk, ConversationCompressionOptions options, CancellationToken token)
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

            chunkText.AppendLine($"{roleStr}: {msg.GetMessageContent()}");
        }

        try
        {
            Conversation conversation = client.Chat.CreateConversation(options.SummaryModel ?? ChatModel.OpenAi.Gpt35.Turbo);
            conversation.RequestParameters.Messages =  [
                new ChatMessage(ChatMessageRoles.System, options.SummaryPrompt),
                new ChatMessage(ChatMessageRoles.User, chunkText.ToString())
            ];
            conversation.RequestParameters.Model = options.SummaryModel;
            conversation.RequestParameters.MaxTokens = options.MaxSummaryTokens;
            conversation.RequestParameters.Temperature = 0.3;
            conversation.RequestParameters.CancellationToken = token;
            ChatResult? result = (await conversation.GetResponseRichSafe())?.Data?.Result;
            return result?.Choices?[0]?.Message?.Content ?? string.Empty;
        }
        catch (Exception)
        {
            // If summarization fails, return a simple placeholder
            return $"[{chunk.Count} messages from this messages]";
        }
    }
}

public class ConversationContent
{
    public List<ChatMessage> SystemMessages { get; set; } = new List<ChatMessage>();
    public List<ChatMessage> MessagesToCompress { get; set; } = new List<ChatMessage>();

    /// <summary>
    ///  Get the content of the messages based on the compression options
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ConversationContent SortContent(List<ChatMessage>messages, ConversationCompressionOptions options)
    {
        ConversationContent content = new ConversationContent();
        ChatMessage msg;
        // Collect messages into categories
        for (int i = 0; i < messages.Count; i++)
        {
            msg = messages[i];

            if (options.PreserveSystemmessages && msg.Role == ChatMessageRoles.System)
            {
                content.SystemMessages.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.User)
            {
                content.MessagesToCompress.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.Tool && options.CompressToolCallmessages) //Keep tool messages only if compressing them
            {
                content.MessagesToCompress.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.Assistant)
            {
                if (msg.FunctionCall != null)
                {
                    if (options.CompressToolCallmessages)
                    {
                        content.MessagesToCompress.Add(msg);
                    }
                }
                else
                {
                    content.MessagesToCompress.Add(msg);
                }
            }
        }

        return content;
    }

    /// <summary>
    ///  Get the content of the messages based on the compression options
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static List<ChatMessage> GetSystemMessages(List<ChatMessage>messages, ConversationCompressionOptions options)
    {
        ConversationContent content = new ConversationContent();
        ChatMessage msg;
        // Collect messages into categories
        for (int i = 0; i < messages.Count; i++)
        {
            msg = messages[i];

            if (options.PreserveSystemmessages && msg.Role == ChatMessageRoles.System)
            {
                content.SystemMessages.Add(msg);
            }
        }

        return content.SystemMessages;
    }
}