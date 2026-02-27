using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Memory;

/// <summary>
/// Uses an LLM to produce conversation summaries for context compression.
/// </summary>
internal sealed class MessageSummarizer
{
    private readonly TornadoApi _api;
    private ChatModel _model;

    public MessageSummarizer(TornadoApi api, ChatModel model)
    {
        _api = api;
        _model = model;
    }

    public void UpdateModel(ChatModel model) => _model = model;

    /// <summary>
    /// Compress messages by summarizing older turns while keeping recent ones.
    /// </summary>
    public async Task<List<ChatMessage>> Summarize(
        List<ChatMessage> messages,
        CompressionAnalysis analysis,
        MessageMetadataTracker metadata,
        CancellationToken cancellationToken)
    {
        // Keep recent messages (last ~20% of non-system messages)
        int keepCount = Math.Max(2, analysis.UncompressedIndices.Count / 5);
        int summarizeUpTo = analysis.UncompressedIndices.Count - keepCount;

        if (summarizeUpTo <= 0)
            return messages;

        // Collect messages to summarize
        List<ChatMessage> toSummarize = [];
        List<int> indicesToRemove = [];

        for (int i = 0; i < summarizeUpTo && i < analysis.UncompressedIndices.Count; i++)
        {
            int idx = analysis.UncompressedIndices[i];
            toSummarize.Add(messages[idx]);
            indicesToRemove.Add(idx);
        }

        if (toSummarize.Count == 0)
            return messages;

        // Build summary prompt
        string summaryText = await GenerateSummary(toSummarize, cancellationToken);

        // Create a summary message
        ChatMessage summaryMessage = new(ChatMessageRoles.System,
            $"[Conversation Summary]\n{summaryText}");
        metadata.MarkCompressed(summaryMessage.Id);

        // Build new message list: system messages + summary + kept messages
        List<ChatMessage> result = [];

        // Preserve system messages
        foreach (ChatMessage msg in messages)
        {
            if (msg.Role == ChatMessageRoles.System && !indicesToRemove.Contains(messages.IndexOf(msg)))
                result.Add(msg);
        }

        result.Add(summaryMessage);

        // Add kept (recent) messages
        for (int i = summarizeUpTo; i < analysis.UncompressedIndices.Count; i++)
        {
            int idx = analysis.UncompressedIndices[i];
            result.Add(messages[idx]);
        }

        return result;
    }

    private async Task<string> GenerateSummary(List<ChatMessage> messages, CancellationToken cancellationToken)
    {
        StringBuilder conversationText = new();
        foreach (ChatMessage msg in messages)
        {
            string role = msg.Role?.ToString() ?? "unknown";
            string content = msg.Content ?? msg.Parts?.FirstOrDefault()?.Text ?? "";
            conversationText.AppendLine($"{role}: {content}");
        }

        try
        {
            Conversation conv = _api.Chat.CreateConversation(new ChatRequest
            {
                Model = _model,
            });

            conv.AppendSystemMessage(
                "You are a conversation summarizer. Produce a concise summary of the following conversation " +
                "that captures all key decisions, information shared, tasks completed, and pending items. " +
                "Be brief but comprehensive. Use bullet points.");

            conv.AppendUserInput(conversationText.ToString());

            var result = await conv.GetResponseSafe();
            return result?.Data?.ToString() ?? "[Summary generation failed]";
        }
        catch (Exception ex)
        {
            return $"[Summary generation error: {ex.Message}]";
        }
    }
}
