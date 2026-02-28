using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Manages the active conversation with automatic summarization and persistence.
/// </summary>
internal sealed class ConversationMemoryManager
{
    private readonly CompressionStrategy _compressionStrategy;
    private readonly MessageSummarizer _summarizer;
    private readonly MessageMetadataTracker _metadataTracker;
    private readonly string? _conversationPath;
    private List<ChatMessage> _messages = [];

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public ConversationMemoryManager(
        TornadoApi api,
        ChatModel model,
        int? contextWindowTokens,
        string? conversationPath = null)
    {
        _conversationPath = conversationPath;
        _compressionStrategy = new CompressionStrategy(contextWindowTokens ?? 128_000);
        _summarizer = new MessageSummarizer(api, model);
        _metadataTracker = new MessageMetadataTracker();

        // Try to resume existing conversation
        if (_conversationPath is not null && File.Exists(_conversationPath))
        {
            try
            {
                PersistentConversation pc = new(_conversationPath);
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
        if (_conversationPath is not null)
        {
            PersistentConversation pc = new(_conversationPath, continuousSave: true);
            pc.AppendMessage(message);
        }
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

        if (_conversationPath is not null)
        {
            try { File.WriteAllText(_conversationPath, ""); }
            catch { /* best effort */ }
        }
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
        if (_conversationPath is null)
            return;

        try
        {
            File.WriteAllText(_conversationPath, "");
            PersistentConversation pc = new(_conversationPath, continuousSave: true);
            foreach (ChatMessage msg in _messages)
                pc.AppendMessage(msg);
        }
        catch
        {
            // best effort
        }
    }
}
