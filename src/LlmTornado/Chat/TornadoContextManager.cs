using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Data;
namespace LlmTornado.Chat;

public interface IContextManager
{
    public Task<Conversation> CheckRefreshAsync(Conversation conversation);
}

public class TornadoContextManager : IContextManager
{
    public IConversationCompressionStrategy CompressionStrategy { get; }
    public IConversationSummarizer Summarizer { get; }

    public string Goal { get; set; } = string.Empty;

    public TornadoContextManager(IConversationCompressionStrategy compressionStrategy, IConversationSummarizer summarizer)
    {
        if (compressionStrategy is null)
        {
            throw new ArgumentNullException(nameof(compressionStrategy));
        }

        CompressionStrategy = compressionStrategy;
        
        if(summarizer is null)
        {
            // Use custom summarizer if provided, otherwise use default
            Summarizer =  new TornadoConversationSummarizer();
        }
        else
        {
            Summarizer = summarizer;
        }
    }

    /// <summary>
    /// Set Context Manager Goal to help guide summarization and compression
    /// </summary>
    /// <param name="goal"></param>
    public void SetGoal(string goal)
    {
        Goal = goal;
    }   

    /// <summary>
    /// Refreshes the conversation by applying compression if needed.
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    public async Task<Conversation> CheckRefreshAsync(Conversation conversation)
    {
        // Implement your logic to get a response for the conversation
        if(CompressionStrategy.ShouldCompress(conversation))
        {
            ConversationCompressionOptions options = CompressionStrategy.GetCompressionOptions(conversation);
            await CompressConversation(conversation, options);
        }

        return conversation;
    }

    private Conversation CreateContext(Conversation conversation, List<ChatMessage> summaries, ConversationCompressionOptions options)
    {
        conversation.Clear();

        // Add system messages first
        conversation.AddMessage(ConversationContent.GetSystemMessages(conversation, options));

        // Add summaries
        conversation.AddMessage(summaries);

        return conversation;
    }

    /// <summary>
    ///     Compresses the conversation messages by summarizing older messages while preserving recent context.
    ///     Messages are grouped into chunks and summarized in parallel for efficiency.
    /// </summary>
    /// <param name="options">Compression options</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The number of messages compressed</returns>
    private async Task CompressConversation(Conversation conversation, ConversationCompressionOptions options, CancellationToken token = default)
    {
        if (conversation.Messages.Count <= 2) return; // Not enough messages to compress

        // Delegate to the summarizer
        List<ChatMessage> summaries = await Summarizer.SummarizeMessages(conversation, options, token);

        if (summaries.Count == 0) return; // No summaries generated keep messages as is

        CreateContext(conversation, summaries, options);
    }
}


