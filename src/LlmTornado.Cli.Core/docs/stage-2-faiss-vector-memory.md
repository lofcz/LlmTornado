# Stage 2: FAISS Vector Memory for Semantic Recall

## Overview

Add an **optional FAISS-based vector memory** layer that embeds every conversation message and retrieves semantically relevant past messages on each turn. This complements the structured summarization from Stage 1 — when messages are compressed away, the vector index retains their embeddings for similarity search, allowing recall of details that the summary may have omitted.

**Source of inspiration:** VisualErp's `FaissVectorMemory` class.

## Problem Solved

After summarization compresses older messages, specific details (file paths, code snippets, tangential facts) may be lost even from the structured summary. Vector memory provides a second chance: when the user's new message is semantically similar to an old compressed-away message, the FAISS index surfaces that old message as "Relevant Past Context" injected into the system prompt.

## Why FAISS

- `LlmTornado.VectorDatabases.Faiss` already exists in the workspace — no new external service
- File-based index (local disk) — no server process needed
- Works with OpenAI `text-embedding-3-small` (1536 dimensions) via the existing `LlmTornado` embedding API
- The `FaissVectorDatabase` class implements `IVectorDatabase` with full CRUD + `QueryByEmbeddingAsync`

## Why Opt-In

- Requires an embedding-capable API key (currently only OpenAI embeds supported)
- Adds latency per message (~100-200ms for embedding API call)
- Adds disk storage for the FAISS index
- Not all CLI/ACP users will want or need semantic recall

## Files Changed

| File | Action | Lines Affected |
|------|--------|----------------|
| `LlmTornado.Cli.Core.csproj` | **Modify** | Add 2 project references |
| `Memory/VectorMemory.cs` | **New file** | — |
| `Memory/ConversationMemoryManager.cs` | **Modify** | Constructor, `AddMessage`, `NewConversation`, new method |
| `AgentSettings.cs` | **Modify** | Add 2 new properties |
| `AgentBuilder.cs` | **Modify** | `BuildSystemPrompt` overload or helper |

## Detailed Changes

### 1. Modify: `LlmTornado.Cli.Core.csproj`

**Add two project references** inside the existing `<ItemGroup>` containing `ProjectReference` entries (after line 22):

```xml
<ProjectReference Include="..\LlmTornado.VectorDatabases\LlmTornado.VectorDatabases.csproj" />
<ProjectReference Include="..\LlmTornado.VectorDatabases.Faiss\LlmTornado.VectorDatabases.Faiss.csproj" />
```

**Result:**
```xml
<ItemGroup>
    <ProjectReference Include="..\LlmTornado\LlmTornado.csproj" />
    <ProjectReference Include="..\LlmTornado.Agents\LlmTornado.Agents.csproj" />
    <ProjectReference Include="..\LlmTornado.Mcp\LlmTornado.Mcp.csproj" />
    <ProjectReference Include="..\LlmTornado.VectorDatabases\LlmTornado.VectorDatabases.csproj" />
    <ProjectReference Include="..\LlmTornado.VectorDatabases.Faiss\LlmTornado.VectorDatabases.Faiss.csproj" />
</ItemGroup>
```

### 2. New File: `Memory/VectorMemory.cs`

```csharp
using LlmTornado.Chat;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Faiss.Integrations;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Optional FAISS-based vector memory for semantic recall of past messages.
/// Embeds each conversation message and supports similarity search.
/// </summary>
internal sealed class VectorMemory : IAsyncDisposable
{
    private readonly TornadoApi _api;
    private readonly FaissVectorDatabase _faissDb;
    private readonly string _collectionName;

    /// <summary>
    /// Creates a new vector memory instance.
    /// </summary>
    /// <param name="api">TornadoApi with embedding-capable provider configured.</param>
    /// <param name="indexDirectory">Directory for FAISS index storage.</param>
    /// <param name="collectionName">Name of the FAISS collection.</param>
    /// <param name="vectorDimension">Embedding dimension (1536 for text-embedding-3-small).</param>
    public VectorMemory(
        TornadoApi api,
        string indexDirectory,
        string collectionName = "conversation_memory",
        int vectorDimension = 1536)
    {
        _api = api;
        _collectionName = collectionName;

        if (!Directory.Exists(indexDirectory))
            Directory.CreateDirectory(indexDirectory);

        _faissDb = new FaissVectorDatabase(indexDirectory, vectorDimension);
    }

    /// <summary>
    /// Initialize the FAISS collection. Must be called once before use.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _faissDb.InitializeCollection(_collectionName);
    }

    /// <summary>
    /// Embed and store a conversation message. Silently skips empty messages.
    /// </summary>
    public async Task AddMessageAsync(ChatMessage message)
    {
        string content = message.Content ?? message.Parts?.FirstOrDefault()?.Text ?? "";
        if (string.IsNullOrWhiteSpace(content))
            return;

        try
        {
            var embeddingResult = await _api.Embeddings.CreateEmbedding(
                EmbeddingModel.OpenAi.Gen3.Small, content);

            if (embeddingResult?.Data is not { Count: > 0 })
                return;

            float[] embedding = embeddingResult.Data[0].Embedding
                .Select(e => (float)e).ToArray();

            VectorDocument doc = new(
                id: Guid.NewGuid().ToString(),
                content: content,
                metadata: new Dictionary<string, object>
                {
                    { "Role", message.Role?.ToString() ?? "unknown" },
                    { "Timestamp", DateTime.UtcNow.ToString("o") }
                },
                embedding: embedding);

            await _faissDb.AddDocumentsAsync([doc]);
        }
        catch
        {
            // Best effort — vector memory is supplementary, never blocks the conversation
        }
    }

    /// <summary>
    /// Search for messages semantically similar to the query.
    /// Returns formatted context strings: "[Role] content".
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <returns>Formatted relevant context strings, or empty list on failure.</returns>
    public async Task<List<string>> SearchRelevantContextAsync(string query, int topK = 3)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            var embeddingResult = await _api.Embeddings.CreateEmbedding(
                EmbeddingModel.OpenAi.Gen3.Small, query);

            if (embeddingResult?.Data is not { Count: > 0 })
                return [];

            float[] embedding = embeddingResult.Data[0].Embedding
                .Select(e => (float)e).ToArray();

            VectorDocument[] results = await _faissDb.QueryByEmbeddingAsync(
                embedding, where: null, topK: topK, includeScore: true);

            return results
                .Where(r => !string.IsNullOrWhiteSpace(r.Content))
                .Select(r =>
                {
                    string role = r.Metadata?.TryGetValue("Role", out object? roleObj) == true
                        ? roleObj.ToString() ?? "unknown"
                        : "unknown";
                    return $"[{role}] {r.Content}";
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Clear all stored vectors (for new conversation).
    /// Deletes and re-creates the collection.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            await _faissDb.DeleteCollectionAsync(_collectionName);
            await _faissDb.InitializeCollection(_collectionName);
        }
        catch
        {
            // Best effort
        }
    }

    public ValueTask DisposeAsync()
    {
        // FaissVectorDatabase doesn't implement IAsyncDisposable,
        // but we expose it for future cleanup needs
        return ValueTask.CompletedTask;
    }
}
```

**Key differences from VisualErp's `FaissVectorMemory`:**
- No session filtering — CLI Core manages one conversation at a time; `ClearAsync()` resets on new conversation
- No `SessionId` in metadata — simpler since there's no multi-session concern
- All operations wrapped in try-catch with silent failure — vector memory is supplementary
- `IAsyncDisposable` for clean lifecycle management

### 3. Modify: `AgentSettings.cs`

**Add two new properties** after the existing `ToolOptimizerEnabled` property (after line 61):

```csharp
/// <summary>
/// Whether FAISS vector memory is enabled for semantic recall of past messages.
/// Requires an embedding-capable provider (e.g., OpenAI).
/// Default: false (opt-in).
/// </summary>
[JsonPropertyName("vector_memory_enabled")]
public bool VectorMemoryEnabled { get; set; }

/// <summary>
/// Directory for FAISS vector index storage.
/// Null = use default (%APPDATA%/llmtornado/vector_index/ in CLI, temp dir in ACP).
/// </summary>
[JsonPropertyName("vector_memory_directory")]
public string? VectorMemoryDirectory { get; set; }
```

### 4. Modify: `Memory/ConversationMemoryManager.cs`

#### 4a. Add optional vector memory field

**Add a new field** after the existing `_conversationPath` field (line 16):

```csharp
private readonly VectorMemory? _vectorMemory;
```

#### 4b. Add constructor parameter

**Modify the constructor signature** to accept an optional `VectorMemory?`:

```csharp
public ConversationMemoryManager(
    TornadoApi api,
    ChatModel model,
    int? contextWindowTokens,
    string? conversationPath = null,
    VectorMemory? vectorMemory = null)
```

**Add assignment** inside the constructor body (after `_metadataTracker = new MessageMetadataTracker();`):

```csharp
_vectorMemory = vectorMemory;
```

#### 4c. Embed messages on add

**In `AddMessage()` method**, after the existing JSONL persistence block (after line 58), add:

```csharp
// Embed for vector memory (best-effort, fire-and-forget)
if (_vectorMemory is not null)
{
    _ = Task.Run(async () =>
    {
        try { await _vectorMemory.AddMessageAsync(message); }
        catch { /* best effort */ }
    });
}
```

**Note:** Fire-and-forget via `Task.Run` is intentional — we don't want embedding latency to block the conversation loop. Failures are silently swallowed.

#### 4d. Add semantic search method

**Add a new public method** after `GetMessagesForAgent()`:

```csharp
/// <summary>
/// Search vector memory for messages semantically relevant to the query.
/// Returns empty list if vector memory is not configured.
/// </summary>
public async Task<List<string>> GetRelevantContextAsync(string query, int topK = 3)
{
    if (_vectorMemory is null)
        return [];

    return await _vectorMemory.SearchRelevantContextAsync(query, topK);
}
```

#### 4e. Clear vector memory on new conversation

**In `NewConversation()` method**, after the existing file clearing block, add:

```csharp
if (_vectorMemory is not null)
{
    _ = Task.Run(async () =>
    {
        try { await _vectorMemory.ClearAsync(); }
        catch { /* best effort */ }
    });
}
```

### 5. Modify: `AgentBuilder.cs`

#### 5a. Add method to build system prompt with vector context

The caller (CLI REPL or ACP runtime) queries vector memory before each turn and passes context into the system prompt. Rather than making `AgentBuilder` dependent on `ConversationMemoryManager`, we add a clean injection point.

**Add a new public method** after the existing `BuildSystemPrompt()`:

```csharp
/// <summary>
/// Build a system prompt enriched with vector memory context.
/// Called by CLI/ACP wrappers that manage vector memory externally.
/// </summary>
public string BuildSystemPromptWithContext(List<string>? vectorContext)
{
    StringBuilder sb = new();
    sb.Append(BuildSystemPrompt());

    if (vectorContext is { Count: > 0 })
    {
        sb.AppendLine();
        sb.AppendLine("--- Relevant Past Context ---");
        foreach (string ctx in vectorContext)
        {
            sb.AppendLine(ctx);
        }
    }

    return sb.ToString();
}
```

However, since `BuildSystemPrompt()` is private and is called inside `Build()`, the practical integration point is for the caller to:
1. Query `memoryManager.GetRelevantContextAsync(userMessage)` before calling `runtime.InvokeAsync()`
2. Inject the relevant context as an additional system message prepended to the conversation

**Alternative (preferred, simpler):** Don't modify `AgentBuilder.BuildSystemPrompt()` at all. Instead, the CLI/ACP caller adds the vector context as a system message at the beginning of the messages list passed to the runtime. This keeps `AgentBuilder` clean and the vector memory truly optional.

```csharp
// In the CLI REPL loop or ACP runtime, before invoking the agent:
List<string> relevantContext = await memoryManager.GetRelevantContextAsync(userMessage);
if (relevantContext.Count > 0)
{
    string contextBlock = "--- Relevant Past Context ---\n" 
        + string.Join("\n", relevantContext);
    // Prepend as a system message to the conversation history
    messages.Insert(0, new ChatMessage(ChatMessageRoles.System, contextBlock));
}
```

**Decision:** Do **not** modify `AgentBuilder.BuildSystemPrompt()`. Instead, update the comment in `ConversationMemoryManager.GetRelevantContextAsync()` to document how callers should inject the context. This keeps Stage 2 changes isolated to the memory layer and settings.

## Data Flow

```
User types a message
  ↓
CLI REPL / ACP Runtime
  ├─ memoryManager.AddMessage(userMessage)
  │    ├─ Appends to _messages list
  │    ├─ Appends to JSONL file  
  │    └─ (async) _vectorMemory.AddMessageAsync(userMessage)
  │         └─ Embeds via OpenAI text-embedding-3-small
  │         └─ Stores in FAISS index
  │
  ├─ relevantContext = await memoryManager.GetRelevantContextAsync(userMessage)  
  │    └─ Embeds query, searches FAISS for top-3 similar past messages
  │    └─ Returns: ["[user] look up part ABC-123", "[assistant] Found part ABC-123..."]
  │
  ├─ (if relevantContext.Count > 0) inject as system message
  │
  └─ runtime.InvokeAsync(userMessage)
       └─ Agent sees: system prompt + [Relevant Past Context] + conversation history + user message
```

## Integration Points for CLI/ACP Callers

### CLI (`CliAgentBuilder` or `Program.cs` REPL loop)

```csharp
// At startup, if settings.VectorMemoryEnabled:
string vectorDir = settings.VectorMemoryDirectory 
    ?? Path.Combine(cliStorage.AppDataPath, "vector_index");
var vectorMemory = new VectorMemory(api, vectorDir);
await vectorMemory.InitializeAsync();

// Pass to ConversationMemoryManager:
var memoryManager = new ConversationMemoryManager(
    api, model, contextWindowTokens, conversationPath, vectorMemory);

// In the REPL loop, before each turn:
List<string> vectorCtx = await memoryManager.GetRelevantContextAsync(userInput);
// Inject vectorCtx into messages if non-empty
```

### ACP Server (`TornadoAcpRuntime`)

```csharp
// In NewSessionAsync, if configured:
var vectorMemory = new VectorMemory(api, Path.GetTempPath() + "tornado_acp_vectors");
await vectorMemory.InitializeAsync();
// Store in session context metadata for per-session vector memory
```

**Note:** The actual CLI/ACP integration code is NOT part of this stage — we're only building the core infrastructure. CLI/ACP wiring happens when those projects adopt the new `ConversationMemoryManager` constructor overload.

## Verification

1. **Build:** Solution compiles with the new FAISS project references
2. **Initialization:** `VectorMemory.InitializeAsync()` creates `faiss_index/` directory and collection file
3. **Embedding:** `AddMessageAsync()` calls the OpenAI embedding API and stores the vector
4. **Search:** `SearchRelevantContextAsync("part ABC")` returns messages that mentioned parts, even if those messages have been summarized away
5. **Clear:** `ClearAsync()` removes all vectors; subsequent searches return empty
6. **Graceful degradation:** If embedding API is unavailable, all operations return silently (no crashes)
7. **Opt-in:** When `VectorMemoryEnabled = false` (default), no vector memory object is created, zero overhead

## Dependencies

- `LlmTornado.VectorDatabases` — for `VectorDocument`, `IVectorDatabase`
- `LlmTornado.VectorDatabases.Faiss` — for `FaissVectorDatabase`
- `FaissNet` NuGet package (already a dependency of the FAISS project)
- OpenAI API key with embedding access (for `text-embedding-3-small`)
