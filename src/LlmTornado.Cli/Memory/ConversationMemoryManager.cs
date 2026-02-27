using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

namespace LlmTornado.Cli.Memory;

/// <summary>
/// Manages the active conversation with automatic summarization and persistence.
/// </summary>
internal sealed class ConversationMemoryManager
{
    private readonly CompressionStrategy _compressionStrategy;
    private readonly MessageSummarizer _summarizer;
    private readonly MessageMetadataTracker _metadataTracker;
    private List<ChatMessage> _messages = [];

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public ConversationMemoryManager(
        TornadoApi api,
        ChatModel model,
        int? contextWindowTokens)
    {
        _compressionStrategy = new CompressionStrategy(contextWindowTokens ?? 128_000);
        _summarizer = new MessageSummarizer(api, model);
        _metadataTracker = new MessageMetadataTracker();

        // Try to resume existing conversation
        if (File.Exists(CliStorage.CurrentConversationPath))
        {
            try
            {
                PersistentConversation pc = new(CliStorage.CurrentConversationPath);
                _messages = pc.GetMessages();
                foreach (ChatMessage msg in _messages)
                    _metadataTracker.Track(msg);
            }
            catch
            {
                _messages = [];
            }
        }
    }

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        _metadataTracker.Track(message);

        // Append to JSONL for crash resilience
        PersistentConversation pc = new(CliStorage.CurrentConversationPath, continuousSave: true);
        pc.AppendMessage(message);
    }

    /// <summary>
    /// Check if summarization is needed and run it if so.
    /// </summary>
    public async Task<bool> MaybeSummarize(CancellationToken cancellationToken = default)
    {
        CompressionAnalysis analysis = _compressionStrategy.Analyze(_messages, _metadataTracker);

        if (!analysis.ShouldCompress)
            return false;

        _messages = await _summarizer.Summarize(_messages, analysis, _metadataTracker, cancellationToken);
        RebuildPersistence();
        return true;
    }

    public List<ChatMessage> GetMessagesForAgent() => [.. _messages];

    public void NewConversation()
    {
        _messages.Clear();
        _metadataTracker.Clear();

        try { File.WriteAllText(CliStorage.CurrentConversationPath, ""); }
        catch { /* best effort */ }
    }

    public void LoadConversation(List<ChatMessage> messages)
    {
        _messages = [.. messages];
        _metadataTracker.Clear();
        foreach (ChatMessage msg in _messages)
            _metadataTracker.Track(msg);
        RebuildPersistence();
    }

    public void UpdateModel(ChatModel model, int? contextWindowTokens)
    {
        _summarizer.UpdateModel(model);
        _compressionStrategy.UpdateContextWindow(contextWindowTokens ?? 128_000);
    }

    private void RebuildPersistence()
    {
        try
        {
            File.WriteAllText(CliStorage.CurrentConversationPath, "");
            PersistentConversation pc = new(CliStorage.CurrentConversationPath, continuousSave: true);
            foreach (ChatMessage msg in _messages)
                pc.AppendMessage(msg);
        }
        catch
        {
            // best effort
        }
    }
}
