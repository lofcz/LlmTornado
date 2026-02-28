# Stage 7: Conversation Memory

## Goal

Provide persistent conversation history with intelligent LLM-based summarization to manage context window usage. Also provide a conversation store for saving, loading, listing, and deleting named conversations.

---

## Files to Create

### `src/LlmTornado.Cli/Memory/ConversationMemoryManager.cs`
### `src/LlmTornado.Cli/Memory/ConversationStore.cs`
### `src/LlmTornado.Cli/Memory/MessageSummarizer.cs`
### `src/LlmTornado.Cli/Memory/CompressionStrategy.cs`

---

## Architecture Overview

```
ConversationMemoryManager
├── PersistentConversation (from LlmTornado.Agents)
│   └── current.jsonl (crash-resilient append-mode)
├── CompressionStrategy
│   └── Decides when to compress (60%/80% thresholds)
├── MessageSummarizer
│   └── Calls LLM to produce summaries
└── ConversationStore
    └── Save/Load/List/Delete named conversations
```

---

## ConversationMemoryManager

The central class managing the active conversation with automatic summarization.

```csharp
namespace LlmTornado.Cli.Memory;

internal sealed class ConversationMemoryManager
{
    private readonly PersistentConversation _persistence;
    private readonly CompressionStrategy _compressionStrategy;
    private readonly MessageSummarizer _summarizer;
    private readonly MessageMetadataTracker _metadataTracker;
    private List<ChatMessage> _messages = [];

    /// <summary>
    /// The current messages (including compressed summaries).
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>
    /// The context window size of the current model (in tokens).
    /// Updated when the model changes.
    /// </summary>
    public int ContextWindowTokens { get; set; }

    public ConversationMemoryManager(
        TornadoApi api,
        ChatModel model,
        int contextWindowTokens)
    {
        _persistence = new PersistentConversation(
            CliStorage.CurrentConversationPath,
            continuousSave: true);
        
        _compressionStrategy = new CompressionStrategy(contextWindowTokens);
        _summarizer = new MessageSummarizer(api, model);
        _metadataTracker = new MessageMetadataTracker();

        // Load existing conversation if resuming
        _messages = _persistence.GetMessages();
    }

    /// <summary>
    /// Add a message and persist it. Does NOT trigger summarization.
    /// </summary>
    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        _persistence.AppendMessage(message);
        _metadataTracker.Track(message);
    }

    /// <summary>
    /// Check if summarization is needed and run it if so.
    /// Call this after each assistant response completes.
    /// Returns true if summarization was performed.
    /// </summary>
    public async Task<bool> MaybeSummarize(CancellationToken cancellationToken = default)
    {
        var analysis = _compressionStrategy.Analyze(_messages, _metadataTracker);
        
        if (!analysis.ShouldCompress)
            return false;

        _messages = await _summarizer.Summarize(
            _messages, analysis, _metadataTracker, cancellationToken);

        // Rebuild persistence with summarized messages
        RebuildPersistence();
        return true;
    }

    /// <summary>
    /// Get messages suitable for passing to Agent.Run().
    /// </summary>
    public List<ChatMessage> GetMessagesForAgent()
    {
        return [.._messages];
    }

    /// <summary>
    /// Start a new conversation. Optionally save the current one first.
    /// </summary>
    public void NewConversation()
    {
        _messages.Clear();
        _persistence.Clear();
        _metadataTracker.Clear();
        
        // Truncate the current.jsonl file
        File.WriteAllText(CliStorage.CurrentConversationPath, "");
    }

    /// <summary>
    /// Load a previously saved conversation.
    /// </summary>
    public void LoadConversation(List<ChatMessage> messages)
    {
        _messages = [..messages];
        _metadataTracker.Clear();
        foreach (var msg in _messages)
            _metadataTracker.Track(msg);
        RebuildPersistence();
    }

    /// <summary>
    /// Update the model used for summarization (e.g., after /model set).
    /// </summary>
    public void UpdateModel(ChatModel model, int contextWindowTokens)
    {
        _summarizer.UpdateModel(model);
        _compressionStrategy.UpdateContextWindow(contextWindowTokens);
        ContextWindowTokens = contextWindowTokens;
    }

    private void RebuildPersistence()
    {
        File.WriteAllText(CliStorage.CurrentConversationPath, "");
        _persistence.Clear();
        foreach (var msg in _messages)
            _persistence.AppendMessage(msg);
    }
}
```

---

## CompressionStrategy

Adapted from the `ContextWindowCompressionStrategy` in the ContextController sample.

```csharp
namespace LlmTornado.Cli.Memory;

internal sealed class CompressionStrategy
{
    private int _contextWindowTokens;

    // --- Tunable Thresholds ---

    /// <summary>
    /// Trigger initial compression when uncompressed messages exceed this fraction
    /// of the context window. Default: 0.60 (60%).
    /// </summary>
    public double UncompressedThreshold { get; set; } = 0.60;

    /// <summary>
    /// Trigger re-compression when compressed + system messages exceed this fraction.
    /// Default: 0.80 (80%).
    /// </summary>
    public double ReCompressionThreshold { get; set; } = 0.80;

    /// <summary>
    /// Target utilization after initial compression. Default: 0.40 (40%).
    /// </summary>
    public double TargetUtilization { get; set; } = 0.40;

    /// <summary>
    /// Target utilization after re-compression. Default: 0.20 (20%).
    /// </summary>
    public double ReCompressionTarget { get; set; } = 0.20;

    /// <summary>
    /// Single messages exceeding this token count are compressed immediately.
    /// Default: 10,000 tokens.
    /// </summary>
    public int LargeMessageThreshold { get; set; } = 10_000;

    public CompressionStrategy(int contextWindowTokens)
    {
        _contextWindowTokens = contextWindowTokens;
    }

    public void UpdateContextWindow(int tokens) => _contextWindowTokens = tokens;

    /// <summary>
    /// Analyze the current message list and decide if compression is needed.
    /// </summary>
    public CompressionAnalysis Analyze(
        List<ChatMessage> messages,
        MessageMetadataTracker metadata)
    {
        int systemTokens = 0;
        int uncompressedTokens = 0;
        int compressedTokens = 0;
        var largeMessages = new List<int>();        // indices
        var uncompressedIndices = new List<int>();
        var compressedIndices = new List<int>();

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            int tokens = EstimateTokens(msg);
            var state = metadata.GetState(msg.Id);

            if (msg.Role == ChatMessageRoles.System)
            {
                systemTokens += tokens;
                continue;   // never compress system messages
            }

            if (state == MessageCompressionState.Compressed ||
                state == MessageCompressionState.ReCompressed)
            {
                compressedTokens += tokens;
                compressedIndices.Add(i);
            }
            else
            {
                uncompressedTokens += tokens;
                uncompressedIndices.Add(i);
                if (tokens > LargeMessageThreshold)
                    largeMessages.Add(i);
            }
        }

        int totalTokens = systemTokens + uncompressedTokens + compressedTokens;
        double utilization = (double)totalTokens / _contextWindowTokens;
        double uncompressedUtil = (double)uncompressedTokens / _contextWindowTokens;
        double compressedUtil = (double)(compressedTokens + systemTokens) / _contextWindowTokens;

        bool shouldCompress = largeMessages.Count > 0
            || uncompressedUtil >= UncompressedThreshold
            || compressedUtil >= ReCompressionThreshold;

        bool isReCompression = largeMessages.Count == 0
            && uncompressedUtil < UncompressedThreshold
            && compressedUtil >= ReCompressionThreshold;

        return new CompressionAnalysis
        {
            ShouldCompress = shouldCompress,
            IsReCompression = isReCompression,
            TotalTokens = totalTokens,
            Utilization = utilization,
            LargeMessageIndices = largeMessages,
            UncompressedIndices = uncompressedIndices,
            CompressedIndices = compressedIndices,
            TargetTokens = isReCompression
                ? (int)(_contextWindowTokens * ReCompressionTarget)
                : (int)(_contextWindowTokens * TargetUtilization)
        };
    }

    /// <summary>
    /// Estimate tokens for a message using the 4-chars-per-token heuristic.
    /// Uses the pre-computed Tokens property if available.
    /// </summary>
    internal static int EstimateTokens(ChatMessage message)
    {
        if (message.Tokens is > 0)
            return message.Tokens.Value;

        int charCount = (message.Content?.Length ?? 0)
            + (message.Parts?.Sum(p => p.Text?.Length ?? 0) ?? 0);
        
        return Math.Max(1, charCount / 4);
    }
}

internal sealed class CompressionAnalysis
{
    public required bool ShouldCompress { get; init; }
    public required bool IsReCompression { get; init; }
    public required int TotalTokens { get; init; }
    public required double Utilization { get; init; }
    public required List<int> LargeMessageIndices { get; init; }
    public required List<int> UncompressedIndices { get; init; }
    public required List<int> CompressedIndices { get; init; }
    public required int TargetTokens { get; init; }
}
```

---

## MessageSummarizer

Calls the LLM to produce concise summaries of message groups.

```csharp
namespace LlmTornado.Cli.Memory;

internal sealed class MessageSummarizer
{
    private readonly TornadoApi _api;
    private ChatModel _model;

    // Configuration
    public int MaxSummaryTokens { get; set; } = 1000;
    public int ChunkSizeChars { get; set; } = 10_000;

    public MessageSummarizer(TornadoApi api, ChatModel model)
    {
        _api = api;
        _model = model;
    }

    public void UpdateModel(ChatModel model) => _model = model;

    /// <summary>
    /// Summarize messages according to the compression analysis.
    /// Returns a new message list with compressed spans replaced by summaries.
    /// </summary>
    public async Task<List<ChatMessage>> Summarize(
        List<ChatMessage> messages,
        CompressionAnalysis analysis,
        MessageMetadataTracker metadata,
        CancellationToken cancellationToken)
    {
        // 1. Determine which messages to compress
        List<int> indicesToCompress = analysis.IsReCompression
            ? analysis.CompressedIndices
            : SelectToTarget(messages, analysis.UncompressedIndices, analysis.TargetTokens, metadata);

        // Also add large messages
        foreach (int idx in analysis.LargeMessageIndices)
        {
            if (!indicesToCompress.Contains(idx))
                indicesToCompress.Add(idx);
        }

        indicesToCompress.Sort();

        if (indicesToCompress.Count == 0)
            return messages;

        // 2. Build contiguous groups of messages to summarize together
        var groups = BuildGroups(indicesToCompress, ChunkSizeChars, messages);

        // 3. Summarize each group via LLM
        var summaries = new Dictionary<(int Start, int End), string>();
        foreach (var group in groups)
        {
            string summary = await SummarizeChunk(
                messages.GetRange(group.Start, group.End - group.Start + 1),
                analysis.IsReCompression,
                cancellationToken);
            summaries[(group.Start, group.End)] = summary;
        }

        // 4. Rebuild message list, replacing groups with summary messages
        var result = new List<ChatMessage>();
        int i = 0;
        foreach (var group in groups.OrderBy(g => g.Start))
        {
            // Add messages before this group
            while (i < group.Start)
                result.Add(messages[i++]);

            // Add summary message
            string prefix = analysis.IsReCompression
                ? "[Re-compressed summary]"
                : "[Compressed summary]";
            var summaryMsg = new ChatMessage(ChatMessageRoles.Assistant,
                $"{prefix}: {summaries[(group.Start, group.End)]}");
            result.Add(summaryMsg);

            // Track the summary message as compressed
            var newState = analysis.IsReCompression
                ? MessageCompressionState.ReCompressed
                : MessageCompressionState.Compressed;
            metadata.SetState(summaryMsg.Id, newState);

            i = group.End + 1;
        }

        // Add remaining messages after the last group
        while (i < messages.Count)
            result.Add(messages[i++]);

        return result;
    }

    /// <summary>
    /// Select oldest uncompressed messages until we reach the target token count.
    /// </summary>
    private List<int> SelectToTarget(
        List<ChatMessage> messages,
        List<int> candidateIndices,
        int targetTokens,
        MessageMetadataTracker metadata)
    {
        // Calculate how many tokens we need to remove
        int currentTotal = messages.Sum(m => CompressionStrategy.EstimateTokens(m));
        int tokensToRemove = currentTotal - targetTokens;
        
        if (tokensToRemove <= 0)
            return [];

        // Select oldest first (lowest indices)
        var selected = new List<int>();
        int removedTokens = 0;

        foreach (int idx in candidateIndices.OrderBy(i => i))
        {
            if (removedTokens >= tokensToRemove)
                break;
            selected.Add(idx);
            removedTokens += CompressionStrategy.EstimateTokens(messages[idx]);
        }

        return selected;
    }

    private static List<(int Start, int End)> BuildGroups(
        List<int> indices, int chunkSizeChars, List<ChatMessage> messages)
    {
        // Build contiguous spans, splitting at gaps or when chunk exceeds size limit
        var groups = new List<(int Start, int End)>();
        if (indices.Count == 0) return groups;

        int start = indices[0];
        int end = indices[0];
        int currentChars = messages[indices[0]].Content?.Length ?? 0;

        for (int i = 1; i < indices.Count; i++)
        {
            bool contiguous = indices[i] == end + 1;
            int msgChars = messages[indices[i]].Content?.Length ?? 0;

            if (contiguous && currentChars + msgChars <= chunkSizeChars)
            {
                end = indices[i];
                currentChars += msgChars;
            }
            else
            {
                groups.Add((start, end));
                start = indices[i];
                end = indices[i];
                currentChars = msgChars;
            }
        }
        groups.Add((start, end));

        return groups;
    }

    /// <summary>
    /// Call the LLM to summarize a chunk of messages.
    /// </summary>
    private async Task<string> SummarizeChunk(
        List<ChatMessage> chunk,
        bool isReCompression,
        CancellationToken cancellationToken)
    {
        // Format messages as text
        var sb = new System.Text.StringBuilder();
        foreach (var msg in chunk)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }

        string prompt = isReCompression
            ? "Provide an extremely concise summary of this previous conversation summary. " +
              "Preserve only the most critical facts, decisions, and action items. Be very brief."
            : "Summarize this conversation segment concisely. Preserve key facts, decisions, " +
              "code snippets mentioned, file paths, and action items. Omit pleasantries and filler.";

        var conversation = _api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model,
            MaxTokens = isReCompression ? MaxSummaryTokens / 2 : MaxSummaryTokens
        });
        conversation.AppendSystemMessage(prompt);
        conversation.AppendUserInput(sb.ToString());

        string? result = await conversation.GetResponseSafe(cancellationToken: cancellationToken);
        return result ?? "[Summary generation failed]";
    }
}
```

---

## MessageMetadataTracker

Tracks the compression state of each message.

```csharp
namespace LlmTornado.Cli.Memory;

internal enum MessageCompressionState
{
    Uncompressed,
    Compressed,
    ReCompressed,
    Archived
}

internal sealed class MessageMetadataTracker
{
    private readonly Dictionary<Guid, MessageCompressionState> _states = new();

    public void Track(ChatMessage message)
    {
        _states.TryAdd(message.Id, MessageCompressionState.Uncompressed);
    }

    public MessageCompressionState GetState(Guid messageId)
    {
        return _states.GetValueOrDefault(messageId, MessageCompressionState.Uncompressed);
    }

    public void SetState(Guid messageId, MessageCompressionState state)
    {
        _states[messageId] = state;
    }

    public void Clear() => _states.Clear();
}
```

---

## ConversationStore

Manages saved conversations on disk.

```csharp
namespace LlmTornado.Cli.Memory;

internal sealed class ConversationStore
{
    /// <summary>
    /// Save the current conversation to a named file.
    /// </summary>
    public ConversationMetadata Save(
        List<ChatMessage> messages,
        string? label,
        string? model,
        List<string>? activeSkills)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string safeName = string.IsNullOrEmpty(label) ? timestamp : $"{timestamp}_{SanitizeLabel(label)}";
        
        string jsonlPath = Path.Combine(CliStorage.ConversationsDirectory, $"{safeName}.jsonl");
        string metaPath = Path.Combine(CliStorage.ConversationsDirectory, $"{safeName}.meta.json");

        // Save messages as JSONL
        messages.SaveConversation(jsonlPath);

        // Save metadata
        var meta = new ConversationMetadata
        {
            Id = safeName,
            Label = label,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Model = model,
            MessageCount = messages.Count,
            FirstMessagePreview = messages
                .FirstOrDefault(m => m.Role == ChatMessageRoles.User)
                ?.Content?[..Math.Min(100, messages[0].Content?.Length ?? 0)],
            ActiveSkills = activeSkills ?? []
        };
        CliStorage.SaveJson(metaPath, meta);

        return meta;
    }

    /// <summary>
    /// List all saved conversations, newest first.
    /// </summary>
    public List<ConversationMetadata> List()
    {
        var results = new List<ConversationMetadata>();
        string dir = CliStorage.ConversationsDirectory;

        if (!Directory.Exists(dir))
            return results;

        foreach (string metaFile in Directory.GetFiles(dir, "*.meta.json"))
        {
            var meta = CliStorage.LoadJson<ConversationMetadata>(metaFile);
            if (meta is not null)
                results.Add(meta);
        }

        return results.OrderByDescending(m => m.CreatedAt).ToList();
    }

    /// <summary>
    /// Load a saved conversation by ID (filename stem).
    /// </summary>
    public List<ChatMessage>? Load(string id)
    {
        string jsonlPath = Path.Combine(CliStorage.ConversationsDirectory, $"{id}.jsonl");
        if (!File.Exists(jsonlPath))
            return null;

        var messages = new List<ChatMessage>();
        messages.LoadMessages(jsonlPath);
        return messages;
    }

    /// <summary>
    /// Delete a saved conversation by ID.
    /// </summary>
    public bool Delete(string id)
    {
        string jsonlPath = Path.Combine(CliStorage.ConversationsDirectory, $"{id}.jsonl");
        string metaPath = Path.Combine(CliStorage.ConversationsDirectory, $"{id}.meta.json");

        bool deleted = false;
        if (File.Exists(jsonlPath)) { File.Delete(jsonlPath); deleted = true; }
        if (File.Exists(metaPath)) { File.Delete(metaPath); deleted = true; }
        return deleted;
    }

    private static string SanitizeLabel(string label)
    {
        // Replace non-alphanumeric chars with hyphens, collapse multiple hyphens
        return Regex.Replace(
            Regex.Replace(label.ToLower(), @"[^a-z0-9-]", "-"),
            @"-+", "-").Trim('-');
    }
}
```

---

## Summarization Flow — Visual

```
Message 1: User asks question                  ← Uncompressed (500 tokens)
Message 2: Assistant answers with code          ← Uncompressed (2000 tokens)
Message 3: User asks follow-up                  ← Uncompressed (300 tokens)
Message 4: Assistant explains                   ← Uncompressed (1500 tokens)
Message 5: User provides large file content     ← LARGE (12000 tokens!)
Message 6: Assistant analyzes file              ← Uncompressed (3000 tokens)
Message 7: User asks about specific function    ← Uncompressed (200 tokens)
                                                 ────────────────────
                                                 Total: 19500 tokens
                                                 Context: 32000 tokens
                                                 Utilization: 61% ← TRIGGERS COMPRESSION

After compression (target: 40% = 12800 tokens):
─────────────────────────────────────────────────
[Compressed summary]: User asked about X.       ← Compressed (200 tokens)
  Assistant provided code solution and
  explained approach Y using file Z.
Message 5: [Compressed]: User shared large      ← Compressed (300 tokens)
  file containing auth module with JWT handling.
Message 6: Assistant analyzes file              ← Uncompressed (3000 tokens)
Message 7: User asks about specific function    ← Uncompressed (200 tokens)
                                                 ────────────────────
                                                 Total: 3700 tokens
                                                 Utilization: 12%
```

---

## Integration Points

| Consumer | What it uses |
|----------|-------------|
| `CliAgentBuilder` (Stage 8) | `GetMessagesForAgent()` for building agent context |
| `Program.cs` (Stage 10) | `AddMessage()` after each turn, `MaybeSummarize()` after response |
| `/conversation save` (Stage 9) | `ConversationStore.Save()` with current messages |
| `/conversation load` (Stage 9) | `ConversationStore.Load()` + `LoadConversation()` |
| `/conversation list` (Stage 9) | `ConversationStore.List()` for display |
| `/conversation new` (Stage 9) | `NewConversation()` to reset state |
| `/model set` (Stage 9) | `UpdateModel()` to adjust summarization model + context window |

---

## Types Used from LlmTornado

| Type | Purpose |
|------|---------|
| `PersistentConversation` | JSONL persistence with crash-resilient append |
| `ChatMessage` | Message model |
| `ChatMessageRoles` | Role enum (System/User/Assistant/Tool) |
| `TornadoApi.Chat.CreateConversation()` | Used by summarizer to call LLM |
| `Conversation.GetResponseSafe()` | Get LLM response for summarization |
| `ConversationIOUtility.SaveConversation()` | Save full conversation to file |
| `ConversationIOUtility.LoadMessages()` | Load conversation from file |
