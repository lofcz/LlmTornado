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
    private int _modelContextWindowTokens;
    private int? _compressionContextTokenCap;

    // Real token accounting: the provider-reported usage of the turn's final request replaces the
    // chars/4 guess as the context-size baseline. Reported mid-turn, sealed at the SyncFrom point
    // (when _messages matches exactly what that request + its completion contained), and dropped
    // whenever history is rewritten.
    private int? _pendingActualTokens;
    private int? _actualTokensAtSync;
    private int _messageCountAtSync;

    /// <summary>
    /// Hard ceiling for the request payload as a fraction of the model context window.
    /// Applied after summarization as a deterministic safety net.
    /// </summary>
    private const double HardBudgetFraction = 0.90;

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public string? ConversationId => _conversationId;
    public int ModelContextWindowTokens => _modelContextWindowTokens;
    public int EffectiveCompressionContextTokens => _compressionStrategy.ContextWindowTokens;
    public int? CompressionContextTokenCap => _compressionContextTokenCap;

    /// <summary>
    /// Raised when the hard budget guard drops messages from the request payload.
    /// Argument is the number of messages dropped. Surfaced so trimming is never silent.
    /// </summary>
    public event Action<int>? ContextTrimmed;

    /// <summary>
    /// Legacy constructor — file-based persistence.
    /// </summary>
    [Obsolete("Use the SqliteConversationStore overload instead.")]
    public ConversationMemoryManager(
        TornadoApi api,
        ChatModel model,
        int? contextWindowTokens,
        string? conversationPath = null,
        int? compressionContextTokenCap = null)
    {
        _conversationPath = conversationPath;
        _modelContextWindowTokens = contextWindowTokens ?? 128_000;
        _compressionContextTokenCap = compressionContextTokenCap is > 0 ? compressionContextTokenCap : null;
        _compressionStrategy = new CompressionStrategy(GetEffectiveContextWindowTokens());
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
        string? conversationId = null,
        int? compressionContextTokenCap = null)
    {
        _store = store;
        _conversationId = conversationId;
        _modelContextWindowTokens = contextWindowTokens ?? 128_000;
        _compressionContextTokenCap = compressionContextTokenCap is > 0 ? compressionContextTokenCap : null;
        _compressionStrategy = new CompressionStrategy(GetEffectiveContextWindowTokens());
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

    /// <summary>
    /// Record real usage from a model response (fires per request in the tool loop; the last one
    /// wins). Prompt + completion together describe the history after that response is appended.
    /// </summary>
    public void ReportActualUsage(int promptTokens, int completionTokens)
    {
        if (promptTokens > 0)
            _pendingActualTokens = promptTokens + completionTokens;
    }

    /// <summary>
    /// Best available token count for the current history: the real provider figure (sealed at the
    /// last sync) plus estimates for any messages appended since; falls back to the chars/4
    /// estimate when no real usage has been observed.
    /// </summary>
    public int EstimateCurrentTokens()
    {
        if (_actualTokensAtSync is int real && _messages.Count >= _messageCountAtSync)
        {
            int tail = 0;
            for (int i = _messageCountAtSync; i < _messages.Count; i++)
                tail += CompressionStrategy.EstimateTokens(_messages[i]);
            return real + tail;
        }

        return _messages.Sum(CompressionStrategy.EstimateTokens);
    }

    /// <summary>True when <see cref="EstimateCurrentTokens"/> is backed by provider-reported usage.</summary>
    public bool HasActualTokenCount => _actualTokensAtSync is not null;

    private void InvalidateActualTokens()
    {
        _pendingActualTokens = null;
        _actualTokensAtSync = null;
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
    /// Run summarization if the context is over threshold, then enforce a hard token budget.
    /// Returns true if the message set changed (so the caller can sync the runtime conversation).
    /// </summary>
    public async Task<bool> MaybeSummarize(CancellationToken cancellationToken = default)
    {
        bool changed = false;

        CompressionAnalysis analysis = _compressionStrategy.Analyze(
            _messages, _metadataTracker, HasActualTokenCount ? EstimateCurrentTokens() : null);
        if (analysis.ShouldCompress)
        {
            int messageCountBefore = _messages.Count;
            List<ChatMessage> before = _messages;
            List<ChatMessage> summarized = await _summarizer.Summarize(_messages, analysis, _metadataTracker, cancellationToken);

            // Summarize returns the SAME list reference when it is a no-op (too few messages to compress);
            // only treat it as a real change when a new list comes back.
            bool didSummarize = !ReferenceEquals(summarized, before);
            _messages = summarized;
            changed |= didSummarize;

            if (didSummarize && _store is not null && _conversationId is not null)
            {
                // Persist summary bookkeeping to DB. The newest compressed-state message is the summary
                // just produced (it is a User-role message, so we locate it by metadata state, not role).
                string summaryText = _messages
                    .Where(m => _metadataTracker.GetState(m.Id) != MessageCompressionState.Uncompressed)
                    .Select(m => m.Content)
                    .LastOrDefault() ?? "";

                int tokenEstimate = summaryText.Length / 4;
                _store.SaveSummary(_conversationId, summaryText, messageCountBefore - 1, tokenEstimate);
                _store.MarkMessagesCompressed(_conversationId, messageCountBefore - 1);
                _store.CreateSnapshot(_conversationId, $"auto-summary-{DateTime.UtcNow:yyyyMMdd_HHmmss}");
            }
        }

        // Hard budget guard — deterministic safety net, runs regardless of ShouldCompress.
        if (EnforceHardBudget())
            changed = true;

        if (changed)
        {
            // History was rewritten: the sealed provider figure no longer describes the payload.
            InvalidateActualTokens();
            PersistFull();
        }

        return changed;
    }

    public List<ChatMessage> GetMessagesForAgent() => [.. _messages];

    /// <summary>
    /// Replace the tracked message set with the authoritative runtime conversation (which includes
    /// tool-call/tool-result messages), then persist it. This is the single sync point that keeps
    /// memory and the runtime conversation from diverging.
    /// </summary>
    public void SyncFrom(IReadOnlyList<ChatMessage> fullMessages)
    {
        _messages = [.. fullMessages];

        // Track NEW messages only (TryAdd) — do NOT clear, so summaries keep their Compressed mark
        // across turns and the compression strategy does not re-summarize them.
        foreach (ChatMessage msg in _messages)
            _metadataTracker.Track(msg);

        // Seal the turn's real usage against this exact message set: the last usage event covered
        // the final request plus its completion, which is precisely what was just synced in.
        if (_pendingActualTokens is int pending)
        {
            _actualTokensAtSync = pending;
            _messageCountAtSync = _messages.Count;
            _pendingActualTokens = null;
        }

        PersistFull();
    }

    /// <summary>
    /// Ensure there is an active conversation id so per-turn persistence is live from the first turn.
    /// No-op if an id is already bound (e.g. after a rebuild or load).
    /// </summary>
    public void EnsureActiveConversation()
    {
        if (_store is null || _conversationId is not null)
            return;

        _conversationId = $"{DateTime.Now:yyyyMMdd_HHmmss}";
        _store.EnsureConversation(_conversationId);
    }

    /// <summary>
    /// Persist the full current message set under the active conversation id (upsert), or to the
    /// legacy file path when no store/id is configured.
    /// </summary>
    private void PersistFull()
    {
        if (_store is not null && _conversationId is not null)
            _store.Save(_messages, null, null, existingId: _conversationId);
        else if (_conversationPath is not null)
            RebuildPersistence();
    }

    /// <summary>
    /// Deterministic last-resort trim so the request payload never exceeds the model budget. Fires only
    /// when even the post-summarization set is over the hard ceiling. Keep priority: the most recent
    /// message (current turn) is always kept; then summaries (which encode dropped context); then the
    /// remaining messages newest-first. Anything that does not fit is dropped, preserving original order.
    /// </summary>
    private bool EnforceHardBudget()
    {
        if (_messages.Count == 0)
            return false;

        int budget = Math.Max(2048, (int)(_compressionStrategy.ContextWindowTokens * HardBudgetFraction));

        // Trigger on the best available total (real when we have it); the keep-set below still
        // fills by per-message estimates, which is the only granularity available.
        int total = EstimateCurrentTokens();
        if (total <= budget)
            return false;

        HashSet<Guid> keepIds = [];
        int used = 0;

        void TryKeep(ChatMessage m)
        {
            if (keepIds.Contains(m.Id))
                return;
            if (keepIds.Count > 0 && used + CompressionStrategy.EstimateTokens(m) > budget)
                return;
            keepIds.Add(m.Id);
            used += CompressionStrategy.EstimateTokens(m);
        }

        bool IsCompressed(ChatMessage m) =>
            _metadataTracker.GetState(m.Id) != MessageCompressionState.Uncompressed;

        // 1. Always keep the most recent message (the current user turn).
        TryKeep(_messages[^1]);

        // 2. Keep summaries newest-first (they encode already-dropped context).
        for (int i = _messages.Count - 1; i >= 0; i--)
            if (IsCompressed(_messages[i]))
                TryKeep(_messages[i]);

        // 3. Fill remaining budget with the rest, newest-first.
        for (int i = _messages.Count - 1; i >= 0; i--)
            TryKeep(_messages[i]);

        int droppedCount = _messages.Count - keepIds.Count;
        if (droppedCount <= 0)
            return false;

        _messages = _messages.Where(m => keepIds.Contains(m.Id)).ToList();
        ContextTrimmed?.Invoke(droppedCount);
        return true;
    }

    public void NewConversation(string? newConversationId = null)
    {
        _messages.Clear();
        _metadataTracker.Clear();
        InvalidateActualTokens();

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
        InvalidateActualTokens();

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
        InvalidateActualTokens();
        _messages = [.. messages];
        _metadataTracker.Clear();
        foreach (ChatMessage msg in _messages)
            _metadataTracker.Track(msg);
        RebuildPersistence();
    }

    public void UpdateModel(ChatModel model, int? contextWindowTokens)
    {
        _summarizer.UpdateModel(model);
        _modelContextWindowTokens = contextWindowTokens ?? 128_000;
        ApplyEffectiveContextWindow();
    }

    /// <summary>
    /// Override the compression trigger/target utilizations (values outside (0, 1] are ignored).
    /// </summary>
    public void ConfigureCompressionThresholds(double? triggerUtilization, double? targetUtilization)
    {
        if (triggerUtilization is > 0 and <= 1)
            _compressionStrategy.UncompressedThreshold = triggerUtilization.Value;
        if (targetUtilization is > 0 and <= 1)
            _compressionStrategy.TargetUtilization = targetUtilization.Value;
    }

    /// <summary>
    /// Set or clear an absolute cap for the context window used by compression/budget enforcement.
    /// Null or non-positive values clear the cap.
    /// </summary>
    public void SetCompressionContextTokenCap(int? capTokens)
    {
        _compressionContextTokenCap = capTokens is > 0 ? capTokens : null;
        ApplyEffectiveContextWindow();
    }

    private void ApplyEffectiveContextWindow()
        => _compressionStrategy.UpdateContextWindow(GetEffectiveContextWindowTokens());

    private int GetEffectiveContextWindowTokens()
    {
        if (_compressionContextTokenCap is > 0)
            return Math.Min(_modelContextWindowTokens, _compressionContextTokenCap.Value);

        return _modelContextWindowTokens;
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
