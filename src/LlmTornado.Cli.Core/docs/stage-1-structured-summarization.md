# Stage 1: Structured Summarization

## Overview

Replace the current flat-text conversation summarization with a **structured JSON summary model** using `outputSchema`-constrained LLM output. This preserves entity precision (IDs, quantities, dates) across compression rounds and enables rolling/incremental summarization instead of re-processing all messages.

**Source of inspiration:** VisualErp's `SummarizationRunnable` + `StructuredSummary` model.

## Problem with Current Approach

The current `MessageSummarizer.GenerateSummary()` produces free-form bullet-point text:
- Entity precision degrades across multiple compression rounds (IDs approximate, quantities drift)
- No structured tracking of completed vs pending work
- Re-summarizing a previous summary loses semantic structure
- No ability for callers to inspect summary contents programmatically

## Files Changed

| File | Action | Lines Affected |
|------|--------|----------------|
| `Memory/StructuredSummary.cs` | **New file** | — |
| `Memory/MessageSummarizer.cs` | **Modify** | Lines 13-113 (most of the file) |
| `Memory/ConversationMemoryManager.cs` | **Modify** | Lines 11-17 (fields), 67-76 (MaybeSummarize) |

**No changes** to `Memory/CompressionStrategy.cs` — the token-budget trigger logic is kept as-is.

## Detailed Changes

### 1. New File: `Memory/StructuredSummary.cs`

Create a new model class in the `LlmTornado.Cli.Core.Memory` namespace.

```csharp
namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Structured rolling summary of a conversation. Used with outputSchema
/// to get constrained JSON output from the LLM summarizer.
/// </summary>
internal sealed class StructuredSummary
{
    /// <summary>
    /// The overarching goal or intent of the conversation.
    /// </summary>
    public string CoreIntent { get; set; } = "";

    /// <summary>
    /// Named entities mentioned in the conversation (parts, vendors, IDs, quantities, dates).
    /// </summary>
    public ExtractedEntity[] Entities { get; set; } = [];

    /// <summary>
    /// Steps/tasks that have been completed during the conversation.
    /// </summary>
    public string[] CompletedSteps { get; set; } = [];

    /// <summary>
    /// Steps/tasks that are still pending or in progress.
    /// </summary>
    public string[] PendingSteps { get; set; } = [];

    /// <summary>
    /// Key decisions made during the conversation.
    /// </summary>
    public string[] Decisions { get; set; } = [];

    /// <summary>
    /// Important facts or context that should be preserved.
    /// </summary>
    public string[] KeyFacts { get; set; } = [];

    /// <summary>
    /// The currently active skill slug, if any.
    /// </summary>
    public string? ActiveSkill { get; set; }
}

/// <summary>
/// A typed entity extracted from conversation context.
/// </summary>
internal sealed class ExtractedEntity
{
    /// <summary>
    /// Entity type (e.g. "Part", "Vendor", "OrderId", "Quantity", "Date", "FilePath").
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// The entity value (e.g. "PART-12345", "Acme Corp", "2026-03-15").
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Optional context about where/how this entity was mentioned.
    /// </summary>
    public string? Context { get; set; }
}
```

**Rationale:** Adapted from VisualErp's `StructuredSummary` but marked `internal sealed` to match CLI Core conventions. The `ExtractedEntity` preserves typed references across compression rounds — if the user mentioned "part ABC-123" early in the conversation and it gets compressed, the entity survives in the structured summary with its exact ID.

### 2. Modify: `Memory/MessageSummarizer.cs`

#### 2a. Add new fields and properties

**After line 16** (after `private ChatModel _model;`), add:

```csharp
private StructuredSummary? _lastSummary;

/// <summary>
/// The most recent structured summary produced by rolling summarization.
/// Null if no summarization has occurred yet.
/// </summary>
public StructuredSummary? LastSummary => _lastSummary;
```

#### 2b. Replace `GenerateSummary()` with rolling structured summarization

**Replace the private `GenerateSummary` method (lines 87-113)** with a new `GenerateStructuredSummary` method:

```csharp
private async Task<StructuredSummary> GenerateStructuredSummary(
    List<ChatMessage> messages,
    CancellationToken cancellationToken)
{
    string prompt = BuildRollingPrompt(_lastSummary, messages);

    try
    {
        TornadoAgent summarizer = new(
            client: _api,
            model: _model,
            name: "StructuredSummarizer",
            instructions: SummarizerInstructions,
            outputSchema: typeof(StructuredSummary),
            streaming: false);

        var result = await summarizer.Run(input: prompt);
        string responseText = result.Messages.LastOrDefault()?.Content ?? "{}";

        StructuredSummary? summary = System.Text.Json.JsonSerializer.Deserialize<StructuredSummary>(
            responseText,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return summary ?? _lastSummary ?? new StructuredSummary();
    }
    catch
    {
        return _lastSummary ?? new StructuredSummary();
    }
}
```

#### 2c. Add the prompt construction method and instructions constant

```csharp
private const string SummarizerInstructions = """
    You are a precision context-preservation engine. Your task is to analyze
    conversation messages and produce a STRUCTURED SUMMARY that preserves all
    important details for continued conversation.
    
    RULES:
    1. NEVER drop entities from the previous summary unless explicitly superseded.
    2. Move completed items from pendingSteps to completedSteps.
    3. Update entity context if new information is learned.
    4. Keep completedSteps in chronological order.
    5. If a skill was activated, set activeSkill to the skill slug.
    6. Be precise with identifiers — never approximate IDs, quantities, or dates.
    7. CoreIntent should describe the user's overarching goal.
    8. KeyFacts should capture important constraints, preferences, or context.
    """;

internal static string BuildRollingPrompt(
    StructuredSummary? previousSummary,
    List<ChatMessage> newMessages)
{
    StringBuilder sb = new();

    if (previousSummary is not null)
    {
        sb.AppendLine("=== PREVIOUS SUMMARY ===");
        sb.AppendLine(System.Text.Json.JsonSerializer.Serialize(previousSummary,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }));
        sb.AppendLine();
    }

    sb.AppendLine("=== NEW MESSAGES ===");
    foreach (ChatMessage msg in newMessages)
    {
        string role = msg.Role?.ToString() ?? "unknown";
        string content = msg.Content ?? msg.Parts?.FirstOrDefault()?.Text ?? "";
        sb.AppendLine($"[{role}]: {content}");
    }
    sb.AppendLine();
    sb.AppendLine("Produce the updated structured summary JSON.");

    return sb.ToString();
}
```

**Key difference from VisualErp:** We use CLI Core's existing `_model` (whatever the user has selected) rather than hardcoding GPT-4.1 Mini. The `CompressionStrategy` still controls *when* to summarize — we only change *what* the summary looks like.

#### 2d. Add `FormatSummaryAsString()` static method

This renders the structured summary into a human-readable text block for injection into the `[Conversation Summary]` system message:

```csharp
/// <summary>
/// Format a structured summary as a readable string for system prompt injection.
/// </summary>
internal static string FormatSummaryAsString(StructuredSummary summary)
{
    StringBuilder sb = new();

    if (!string.IsNullOrEmpty(summary.CoreIntent))
        sb.AppendLine($"**Goal:** {summary.CoreIntent}");

    if (summary.Entities.Length > 0)
    {
        sb.AppendLine("**Key Entities:**");
        foreach (ExtractedEntity entity in summary.Entities)
        {
            string ctx = entity.Context is not null ? $" ({entity.Context})" : "";
            sb.AppendLine($"  - {entity.Type}: {entity.Value}{ctx}");
        }
    }

    if (summary.CompletedSteps.Length > 0)
    {
        sb.AppendLine("**Completed:**");
        foreach (string step in summary.CompletedSteps)
            sb.AppendLine($"  - {step}");
    }

    if (summary.PendingSteps.Length > 0)
    {
        sb.AppendLine("**Pending:**");
        foreach (string step in summary.PendingSteps)
            sb.AppendLine($"  - {step}");
    }

    if (summary.Decisions.Length > 0)
    {
        sb.AppendLine("**Decisions:**");
        foreach (string decision in summary.Decisions)
            sb.AppendLine($"  - {decision}");
    }

    if (summary.KeyFacts.Length > 0)
    {
        sb.AppendLine("**Facts:**");
        foreach (string fact in summary.KeyFacts)
            sb.AppendLine($"  - {fact}");
    }

    if (summary.ActiveSkill is not null)
        sb.AppendLine($"**Active Skill:** {summary.ActiveSkill}");

    return sb.ToString();
}
```

#### 2e. Modify `Summarize()` to use rolling structured summarization

**Replace the body of `Summarize()` (lines 29-83)** — the method signature stays the same for backward compatibility, but internally it now uses `GenerateStructuredSummary` and carries forward `_lastSummary`:

```csharp
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

    // Rolling structured summarization
    _lastSummary = await GenerateStructuredSummary(toSummarize, cancellationToken);
    string summaryText = FormatSummaryAsString(_lastSummary);

    // Create a summary message
    ChatMessage summaryMessage = new(ChatMessageRoles.System,
        $"[Conversation Summary]\n{summaryText}");
    metadata.MarkCompressed(summaryMessage.Id);

    // Build new message list: system messages + summary + kept messages
    List<ChatMessage> result = [];

    // Preserve system messages (except ones being removed)
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
```

#### 2f. Add required using statements

At the top of the file, add:
```csharp
using LlmTornado.Agents;  // for TornadoAgent
```

### 3. Modify: `Memory/ConversationMemoryManager.cs`

#### 3a. Expose the last structured summary

**After line 19** (after `public IReadOnlyList<ChatMessage> Messages => _messages;`), add:

```csharp
/// <summary>
/// The most recent structured summary from the last compression pass.
/// Null if no compression has been triggered yet.
/// </summary>
public StructuredSummary? LastSummary => _summarizer.LastSummary;
```

No other changes needed — `MaybeSummarize()` already delegates to `_summarizer.Summarize()` which now produces structured summaries internally. The `CompressionStrategy` trigger logic is untouched.

## Data Flow (Before vs After)

### Before
```
Messages exceed 60% context window
  → CompressionStrategy.Analyze() triggers compression
  → MessageSummarizer.Summarize() picks messages to compress
  → GenerateSummary() → free-form Conversation → bullet-point text
  → "[Conversation Summary]\n• point 1\n• point 2..."
```

### After  
```
Messages exceed 60% context window
  → CompressionStrategy.Analyze() triggers compression (unchanged)
  → MessageSummarizer.Summarize() picks messages to compress (same logic)
  → GenerateStructuredSummary() → TornadoAgent with outputSchema: typeof(StructuredSummary)
  → Rolling: carries forward _lastSummary + only new messages
  → FormatSummaryAsString() → readable structured text
  → "[Conversation Summary]\n**Goal:** ...\n**Key Entities:**\n  - Part: ABC-123..."
```

### Rolling Benefit

On the **second compression pass**, the summarizer receives:
1. The previous `StructuredSummary` as JSON (entities already extracted)
2. Only the messages added since the last summary

This means entities from the first pass are carried forward without re-processing the entire history. The LLM's job becomes "update this summary with these new messages" rather than "summarize everything from scratch."

## Verification

1. **Build:** Solution should compile with no errors
2. **Functional test:** Start a multi-turn conversation, fill context to ~60% (triggering compression), then check the `[Conversation Summary]` system message contains structured fields (`**Goal:**`, `**Key Entities:**`, `**Completed:**`, etc.)
3. **Entity precision:** Mention a specific ID (e.g., "look up part ABC-123") early in conversation. After compression, the ID should appear verbatim in the `Entities` section.
4. **Rolling test:** Continue the conversation past a second compression trigger. The second summary should include entities from both the first and second pass.
5. **Graceful degradation:** If the LLM returns malformed JSON, the summarizer falls back to the previous summary (or an empty one), never crashes.

## Dependencies

- `LlmTornado.Agents` — for `TornadoAgent` (already referenced in `.csproj`)
- No new NuGet packages required
- No changes to `.csproj`
