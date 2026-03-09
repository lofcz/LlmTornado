using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Storage;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Manages the active conversation with automatic summarization and persistence.
/// Supports both legacy file-based and new SQLite-based storage.
/// </summary>
public sealed class ConversationMemoryManager
{
    private readonly CompressionStrategy _compressionStrategy;
    private readonly MessageSummarizer _summarizer;
    private readonly MessageMetadataTracker _metadataTracker;

    // Legacy file-based path (kept for backward compatibility)
    private readonly string? _conversationPath;

    // New SQLite-backed store
    private readonly SqliteConversationStore? _store;
    private string? _conversationId;

    private List<ChatMessage> _messages = [];

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public string? ConversationId => _conversationId;

    /// <summary>
    /// Legacy constructor — file-based persistence.
    /// </summary>
    [Obsolete("Use the SqliteConversationStore overload instead.")]
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

    /// <summary>
    /// SQLite-backed constructor.
    /// </summary>
    public ConversationMemoryManager(
        TornadoApi api,
        ChatModel model,
        int? contextWindowTokens,
        SqliteConversationStore store,
        string? conversationId = null)
    {
        _store = store;
        _conversationId = conversationId;
        _compressionStrategy = new CompressionStrategy(contextWindowTokens ?? 128_000);
        _summarizer = new MessageSummarizer(api, model);
        _metadataTracker = new MessageMetadataTracker();

        // Try to resume existing conversation from SQLite
        if (_conversationId is not null)
        {
            List<ChatMessage>? loaded = _store.Load(_conversationId);
            if (loaded is not null)
            {
                _messages = loaded;
                foreach (ChatMessage msg in _messages)
                    _metadataTracker.Track(msg);
            }
        }
    }

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        _metadataTracker.Track(message);

        if (_store is not null && _conversationId is not null)
        {
            _store.AppendMessage(_conversationId, message, _messages.Count - 1);
        }
        else if (_conversationPath is not null)
        {
            // Legacy file persistence
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

        int messageCountBefore = _messages.Count;
        _messages = await _summarizer.Summarize(_messages, analysis, _metadataTracker, cancellationToken);

        if (_store is not null && _conversationId is not null)
        {
            // Persist summary to DB
            string summaryText = _messages
                .Where(m => m.Role == LlmTornado.Code.ChatMessageRoles.System)
                .Select(m => m.Content)
                .LastOrDefault() ?? "";

            int tokenEstimate = summaryText.Length / 4;
            long summaryId = _store.SaveSummary(_conversationId, summaryText, messageCountBefore - 1, tokenEstimate);

            // Mark old messages as compressed
            _store.MarkMessagesCompressed(_conversationId, messageCountBefore - 1);

            // Auto-create snapshot
            _store.CreateSnapshot(_conversationId, $"auto-summary-{DateTime.UtcNow:yyyyMMdd_HHmmss}");

            // Re-save the compressed message set
            _store.Save(_messages, null, null, existingId: _conversationId);
        }
        else
        {
            RebuildPersistence();
        }

        return true;
    }

    public List<ChatMessage> GetMessagesForAgent() => [.. _messages];

    public void NewConversation(string? newConversationId = null)
    {
        _messages.Clear();
        _metadataTracker.Clear();

        if (_store is not null)
        {
            _conversationId = newConversationId ?? $"{DateTime.Now:yyyyMMdd_HHmmss}";
            _store.EnsureConversation(_conversationId);
        }
        else if (_conversationPath is not null)
        {
            try { File.WriteAllText(_conversationPath, ""); }
            catch { /* best effort */ }
        }
    }

    public void LoadConversation(string conversationId)
    {
        if (_store is not null)
        {
            _conversationId = conversationId;
            List<ChatMessage>? loaded = _store.Load(conversationId);
            _messages = loaded ?? [];
        }
        else
        {
            _messages = [];
        }

        _metadataTracker.Clear();
        foreach (ChatMessage msg in _messages)
            _metadataTracker.Track(msg);
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
