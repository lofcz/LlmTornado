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
    public string SummaryPrompt { get; set; } = "Summarize this chat conversation concisely, preserving key information, Keep the Summary brief 4000 characters MAX.";

    /// <summary>
    ///     The prompt to use for the final summary message.
    /// </summary>
    public string OverviewPrompt { get; set; } = "Based on the previous conversation summary, continue the conversation naturally.";

    /// <summary>
    /// Define the role to set the summary messages as.
    /// </summary>
    public ChatMessageRoles Role { get; set; } = ChatMessageRoles.Assistant;

    /// <summary>
    /// Chat temperature for summarization.
    /// </summary>
    public double Temperature { get; set; } = 0.3;
}

/// <summary>
///     Strategy for determining when conversation compression should occur.
/// </summary>
public interface IConversationCompressor
{
    /// <summary>
    ///     Determines if compression should occur based on the current conversation state.
    /// </summary>
    /// <param name="conversation">The conversation to check</param>
    /// <returns>True if compression should occur, false otherwise</returns>
    bool ShouldCompress(List<ChatMessage> messages);

    /// <summary>
    ///     Summarizes a Conversation into one or more summary messages.
    /// </summary>
    /// <param name="conversation">conversation to summarize</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of summary messages</returns>
    Task<List<ChatMessage>> Compress(List<ChatMessage> messages, string? context = "", CancellationToken token = default);
}


public class ConversationCompressor : IConversationCompressor
{
    private readonly int tokenThreshold;
    private readonly ConversationCompressionOptions options;
    private readonly TornadoApi api;

    public ConversationCompressor(TornadoApi api, int tokenThreshold = 50000, ConversationCompressionOptions? options = null)
    {
        this.tokenThreshold = tokenThreshold;
        this.options = options ?? new ConversationCompressionOptions();
        this.api = api;
    }

    public bool ShouldCompress(List<ChatMessage> messages)
    {
        return messages.Sum(m => m.GetMessageTokens()) > tokenThreshold;
    }

    /// <summary>
    /// Compress the conversation by summarizing older messages while preserving recent context.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<List<ChatMessage>> Compress(List<ChatMessage> messages, string? context = "", CancellationToken token = default)
    {
        ConversationContent content = ConversationContent.SortContent(messages, options);

        // Group messages into chunks based on character count
        List<List<ChatMessage>> chunks = CreateChunks(content, options.ChunkSize);
        
        // Process chunks in parallel to get summaries
        Task<string>[] summaryTasks = chunks.Select(chunk => SummarizeChunk(api, chunk, options, context, token)).ToArray();
        string[] summaries = await Task.WhenAll(summaryTasks);

        List<ChatMessage> conversation = new List<ChatMessage>();

        foreach (string summary in summaries)
        {
            if (!summary.IsNullOrWhiteSpace())
            {
                conversation.Add(new ChatMessage(options.Role, $"[Previous conversation summary]: {summary}"));
            }
        }

        var followUpMessage = await CreateOverviewMessage(api, string.Join("\n\n",summaries), token);

        conversation.Add(new ChatMessage(options.Role, followUpMessage));

        return conversation;
    }

    private async Task<string> CreateOverviewMessage(TornadoApi api, string  messages, CancellationToken token = default)
    {
        try
        {
            Conversation conversation = api.Chat.CreateConversation(options.SummaryModel, options.Temperature);
            conversation.AddSystemMessage(options.OverviewPrompt);
            conversation.AddUserMessage(messages);

            var response = await conversation.GetResponseRichSafe(token);

            return response.Data.Text;
        }
        catch (Exception)
        {
            // If summarization fails, return a simple placeholder
            return $"";
        }
    }

    private List<List<ChatMessage>> CreateChunks(ConversationContent content, int chunkSize)
    {
        List<List<ChatMessage>> chunks = [];
        List<ChatMessage> currentChunk = [];
        int currentChunkLength = 0;
        foreach (ChatMessage msg in content.MessagesToCompress)
        {
            int msgLength = msg.GetMessageTokens();
            if (currentChunkLength + msgLength > chunkSize && currentChunk.Count > 0)
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
        return chunks;
    }

    private async Task<string> SummarizeChunk(TornadoApi api, List<ChatMessage> chunk, ConversationCompressionOptions options, string? context = "",  CancellationToken token = default)
    {
        // Build the text representation of the chunk
        StringBuilder chunkText = new StringBuilder();

        foreach (ChatMessage msg in chunk)
        {
            chunkText.AppendLine(msg.GetMessageContent());
        }

        try
        {
            Conversation conversation = api.Chat.CreateConversation(options.SummaryModel, options.Temperature);
            conversation.AddSystemMessage(options.SummaryPrompt);
            conversation.AddUserMessage(chunkText.ToString());

            var ser = conversation.Serialize();
            var response = await conversation.GetResponseRichSafe(token);

            return response.Data.Text;
        }
        catch (Exception)
        {
            // If summarization fails, return a simple placeholder
            return $"[{chunk.Count} messages from this conversation]";
        }
    }
}

public class ConversationContent
{
    public List<ChatMessage> SystemMessages { get; set; } = new List<ChatMessage>();
    public List<ChatMessage> MessagesToCompress { get; set; } = new List<ChatMessage>();

    /// <summary>
    ///  Get the content of the conversation based on the compression options
    /// </summary>
    /// <param name="conversation"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ConversationContent SortContent(List<ChatMessage> messages, ConversationCompressionOptions options)
    {
        ConversationContent content = new ConversationContent();
        ChatMessage msg;
        // Collect messages into categories
        for (int i = 0; i < messages.Count; i++)
        {
            msg = messages[i];

            if (options.PreserveSystemMessages && msg.Role == ChatMessageRoles.System)
            {
                content.SystemMessages.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.User)
            {
                content.MessagesToCompress.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.Tool && options.CompressToolCallMessages) //Keep tool messages only if compressing them
            {
                content.MessagesToCompress.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.Assistant)
            {
                if (msg.FunctionCall != null)
                {
                    if (options.CompressToolCallMessages)
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
}