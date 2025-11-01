using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
///     Default implementation of messages summarizer using chunked parallel summarization.
/// </summary>
public class TornadoMessageSummarizer : IMessagesSummarizer
{
    public TornadoApi Client { get; set; }
    public TornadoMessageSummarizer(TornadoApi client)
    {
        Client = client;
    }
    public async Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage> messages, MessageCompressionOptions options, CancellationToken token = default)
    {
        // Separate system messages if preserving them
        ConversationContent content = ConversationContent.SortContent(messages, options);
        // Group messages into chunks based on character count
        List<List<ChatMessage>> chunks = [];
        List<ChatMessage> currentChunk = [];
        int currentChunkLength = 0;

        foreach (ChatMessage msg in content.MessagesToCompress)
        {
            int msgLength = msg.GetMessageTokens();

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

    private async Task<string> SummarizeChunk(TornadoApi client, List<ChatMessage> chunk, MessageCompressionOptions options, CancellationToken token)
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
