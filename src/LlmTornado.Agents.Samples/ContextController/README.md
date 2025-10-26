# Advanced Context Window Management Strategy

A sophisticated message compression system for managing chat context windows in LLM applications. This system automatically compresses conversation history to keep token usage within optimal bounds while preserving important context.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [How It Works](#how-it-works)
- [Performance](#performance)
- [Migration Guide](#migration-guide)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

The Advanced Context Window Management Strategy provides intelligent, threshold-based message compression for long-running conversations. It automatically monitors context window utilization and applies selective compression to keep conversations within optimal token budgets.

### Key Benefits

- ✅ **Automatic compression** - No manual intervention required
- ✅ **Intelligent targeting** - Compresses oldest messages first, preserving recent context
- ✅ **Multi-level compression** - Initial compression and re-compression for extreme cases
- ✅ **System message protection** - Never compresses critical system messages
- ✅ **Large message handling** - Immediately compresses messages >10k tokens
- ✅ **Configurable thresholds** - Customize compression behavior for your use case

---

## ✨ Features

### Threshold-Based Compression

The system uses three configurable thresholds to trigger compression:

1. **60% Utilization** - Compresses uncompressed messages to ~40% target
2. **80% Compressed+System** - Re-compresses already compressed messages to ~20% target
3. **10k+ Token Messages** - Immediately compresses large individual messages

### Intelligent Message Categorization

Messages are automatically categorized into:
- **System Messages** - Never compressed (instructions, prompts)
- **Uncompressed Messages** - Original conversation messages
- **Compressed Messages** - First-level summarized content
- **Re-Compressed Messages** - Second-level highly compressed summaries

### Metadata Tracking

Each message is tracked with:
- Compression state (Uncompressed/Compressed/ReCompressed)
- Compression generation count (0, 1, 2, ...)
- Original timestamp (for age-based sorting)
- Estimated token count
- System message flag

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│              MessageCompressionService                  │
│  (Orchestrates compression based on strategy)           │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┴─────────────┐
        │                          │
┌───────▼────────────┐  ┌──────────▼──────────────┐
│  Compression       │  │  Message                │
│  Strategy          │  │  Summarizer             │
│  (When/What)       │  │  (How)                  │
└────────┬───────────┘  └──────────┬──────────────┘
         │                         │
    ┌────▼──────┐          ┌──────▼──────┐
    │  Message  │          │   Token     │
    │  Metadata │          │   Estimator │
    │  Store    │          │             │
    └───────────┘          └─────────────┘
```

### Core Components

#### 1. **ContextWindowCompressionStrategy**
Decision engine that determines when and what to compress based on:
- Context window utilization analysis
- Message categorization (system/compressed/uncompressed)
- Threshold-based triggers
- Compression target calculations

#### 2. **ContextWindowMessageSummarizer**
Execution engine that performs compression:
- Large message compression (>10k tokens)
- Progressive uncompressed message compression
- Re-compression of already compressed content
- LLM-based summarization with configurable prompts

#### 3. **MessageMetadataStore**
Tracking system that maintains:
- Compression state for all messages
- Generation counters
- Timestamp information
- Token estimates
- Thread-safe operations

#### 4. **TokenEstimator**
Utility for token calculations:
- Character-based estimation (4 chars ≈ 1 token)
- Context window size retrieval
- Utilization percentage calculation

---

## 🚀 Getting Started

### Basic Setup

```csharp
using LlmTornado.Agents.Samples.ContextController;

// 1. Create the metadata store
var metadataStore = new MessageMetadataStore();

// 2. Create the compression strategy
var strategy = new ContextWindowCompressionStrategy(
    model: ChatModel.OpenAi.Gpt4o,
    metadataStore: metadataStore
);

// 3. Create the message summarizer
var summarizer = new ContextWindowMessageSummarizer(
    client: tornadoApi,
    model: ChatModel.OpenAi.Gpt4o,
    metadataStore: metadataStore
);

// 4. Create the compression service
var compressionService = new MessageCompressionService(
    api: tornadoApi,
    strategy: strategy,
    summarizer: summarizer
);
```

### Track Messages

```csharp
// Track new messages as they're added to conversation
foreach (var message in conversation.Messages)
{
    metadataStore.Track(message);
}
```

### Check and Compress

```csharp
// Check if compression is needed
if (strategy.ShouldCompress(conversation.Messages))
{
    // Get compression options based on current state
    var options = strategy.GetCompressionOptions(conversation.Messages);
    
    // Perform compression
    var summaries = await summarizer.SummarizeMessages(
        conversation.Messages,
        options,
        cancellationToken
    );
    
    // Replace compressed messages with summaries
    // (implementation depends on your conversation management)
}
```

---

## ⚙️ Configuration

### ContextWindowCompressionOptions

```csharp
var options = new ContextWindowCompressionOptions
{
    // Target utilization after initial compression (default: 0.40 = 40%)
    TargetUtilization = 0.40,
    
    // Threshold to trigger uncompressed message compression (default: 0.60 = 60%)
    UncompressedCompressionThreshold = 0.60,
    
    // Threshold to trigger re-compression (default: 0.80 = 80%)
    CompressedReCompressionThreshold = 0.80,
    
    // Target utilization after re-compression (default: 0.20 = 20%)
    ReCompressionTarget = 0.20,
    
    // Token threshold for "large" messages (default: 10000)
    LargeMessageThreshold = 10000,
    
    // Characters per chunk for compression (default: 10000)
    ChunkSize = 10000,
    
    // Whether to compress tool call messages (default: true)
    CompressToolCallmessages = true,
    
    // Model to use for summarization (default: same as conversation model)
    SummaryModel = ChatModel.OpenAi.Gpt4oMini,
    
    // Prompt for initial compression
    InitialCompressionPrompt = "Summarize these messages concisely while preserving all key information, decisions, and context:",
    
    // Prompt for re-compression
    ReCompressionPrompt = "Create an even more concise summary of these already-summarized messages, focusing only on critical information:",
    
    // Maximum tokens for each summary (default: 1000)
    MaxSummaryTokens = 1000
};

var strategy = new ContextWindowCompressionStrategy(
    model: ChatModel.OpenAi.Gpt4o,
    metadataStore: metadataStore,
    options: options
);
```

### Recommended Configurations

#### Conservative (Preserve More Context)
```csharp
new ContextWindowCompressionOptions
{
    TargetUtilization = 0.50,                    // Less aggressive compression
    UncompressedCompressionThreshold = 0.70,     // Wait longer before compressing
    CompressedReCompressionThreshold = 0.85,     // Rarely re-compress
    LargeMessageThreshold = 15000,               // Higher threshold for "large"
    MaxSummaryTokens = 1500                      // Longer summaries
}
```

#### Aggressive (Maximize Space)
```csharp
new ContextWindowCompressionOptions
{
    TargetUtilization = 0.30,                    // More aggressive compression
    UncompressedCompressionThreshold = 0.50,     // Compress earlier
    CompressedReCompressionThreshold = 0.70,     // Re-compress more often
    LargeMessageThreshold = 5000,                // Lower threshold for "large"
    MaxSummaryTokens = 500                       // Shorter summaries
}
```

#### Budget-Conscious (Minimize API Calls)
```csharp
new ContextWindowCompressionOptions
{
    TargetUtilization = 0.35,
    UncompressedCompressionThreshold = 0.65,
    CompressedReCompressionThreshold = 0.85,
    SummaryModel = ChatModel.OpenAi.Gpt4oMini,  // Use cheaper model for summaries
    MaxSummaryTokens = 800
}
```

---

## 📖 Usage Examples

### Example 1: Basic Integration

```csharp
public class ChatService
{
    private readonly TornadoApi _client;
    private readonly MessageMetadataStore _metadataStore;
    private readonly ContextWindowCompressionStrategy _strategy;
    private readonly ContextWindowMessageSummarizer _summarizer;
    
    public ChatService(TornadoApi client)
    {
        _client = client;
        _metadataStore = new MessageMetadataStore();
        
        _strategy = new ContextWindowCompressionStrategy(
            ChatModel.OpenAi.Gpt4o,
            _metadataStore
        );
        
        _summarizer = new ContextWindowMessageSummarizer(
            _client,
            ChatModel.OpenAi.Gpt4o,
            _metadataStore
        );
    }
    
    public async Task<string> SendMessage(
        Conversation conversation,
        string userMessage)
    {
        // Add user message
        var message = new ChatMessage(ChatMessageRoles.User, userMessage);
        conversation.AppendMessage(message);
        _metadataStore.Track(message);
        
        // Check if compression is needed
        if (_strategy.ShouldCompress(conversation.Messages))
        {
            await CompressConversation(conversation);
        }
        
        // Get response
        var response = await conversation.GetResponseFromChatbotAsync();
        _metadataStore.Track(response);
        
        return response.Content;
    }
    
    private async Task CompressConversation(Conversation conversation)
    {
        var options = _strategy.GetCompressionOptions(conversation.Messages);
        var summaries = await _summarizer.SummarizeMessages(
            conversation.Messages,
            options
        );
        
        // Replace compressed messages with summaries
        // (implementation depends on your conversation structure)
    }
}
```

### Example 2: Monitoring Context Window Usage

```csharp
public class ContextMonitor
{
    private readonly ContextWindowCompressionStrategy _strategy;
    
    public ContextWindowAnalysis GetAnalysis(List<ChatMessage> messages)
    {
        return _strategy.AnalyzeMessages(messages);
    }
    
    public void LogAnalysis(ContextWindowAnalysis analysis)
    {
        Console.WriteLine($"Context Window: {analysis.ContextWindowSize} tokens");
        Console.WriteLine($"Total Used: {analysis.TotalTokens} ({analysis.TotalUtilization:P1})");
        Console.WriteLine($"  - System: {analysis.SystemTokens} tokens");
        Console.WriteLine($"  - Compressed: {analysis.CompressedTokens} tokens");
        Console.WriteLine($"  - Uncompressed: {analysis.UncompressedTokens} tokens");
        Console.WriteLine($"Compressed+System: {analysis.CompressedAndSystemUtilization:P1}");
        Console.WriteLine($"Has Large Messages: {analysis.HasLargeMessages}");
    }
}
```

### Example 3: Custom Compression Logic

```csharp
public class CustomCompressionStrategy : IMessagesCompressionStrategy
{
    private readonly ContextWindowCompressionStrategy _baseStrategy;
    
    public bool ShouldCompress(List<ChatMessage> messages)
    {
        // Add custom logic before default strategy
        if (IsWeekend())
        {
            // More aggressive on weekends (lower API usage)
            return messages.Count > 20;
        }
        
        return _baseStrategy.ShouldCompress(messages);
    }
    
    public MessageCompressionOptions GetCompressionOptions(List<ChatMessage> messages)
    {
        var options = _baseStrategy.GetCompressionOptions(messages);
        
        // Customize options based on time of day
        if (IsPeakHours())
        {
            options.SummaryModel = ChatModel.OpenAi.Gpt4oMini; // Use cheaper model
            options.MaxSummaryTokens = 500; // Shorter summaries
        }
        
        return options;
    }
}
```

### Example 4: Periodic Metadata Cleanup

```csharp
public class ConversationManager
{
    private readonly MessageMetadataStore _metadataStore;
    
    // Clean up metadata for messages no longer in conversation
    public void CleanupMetadata(Conversation conversation)
    {
        var currentIds = conversation.Messages
            .Select(m => m.Id)
            .ToHashSet();
        
        int removed = _metadataStore.RemoveWhere(id => !currentIds.Contains(id));
        
        Console.WriteLine($"Removed metadata for {removed} old messages");
        Console.WriteLine($"Memory usage: {_metadataStore.GetEstimatedMemoryUsage()} bytes");
    }
}
```

---

## 🔍 How It Works

### Compression Decision Flow

```
1. Analyze Messages
   ├─ Categorize: System, Compressed, Uncompressed
   ├─ Calculate: Total tokens, Utilization %
   └─ Identify: Large messages (>10k tokens)

2. Check Compression Rules (in order)
   ├─ Rule 1: Has large messages? → YES → Compress
   ├─ Rule 2: Total utilization ≥60%? → YES → Compress
   └─ Rule 3: Compressed+System ≥80%? → YES → Re-compress

3. Determine Compression Type
   ├─ Large Messages: Individual compression
   ├─ High Total: Compress oldest uncompressed → target 40%
   └─ High Compressed: Re-compress oldest compressed → target 20%

4. Execute Compression
   ├─ Group messages into chunks
   ├─ Summarize using LLM
   ├─ Update metadata state
   └─ Return summary messages
```

### Compression Strategies

#### Initial Compression (60% Threshold)
- **Target:** Reduce to 40% utilization
- **Scope:** Oldest uncompressed messages
- **Method:** Standard summarization with context preservation
- **Prompt:** "Summarize these messages concisely while preserving all key information..."

#### Re-Compression (80% Threshold)
- **Target:** Reduce compressed+system to 20% utilization
- **Scope:** Oldest compressed/re-compressed messages
- **Method:** Aggressive summarization focusing on essentials
- **Prompt:** "Create an even more concise summary focusing only on critical information..."
- **Tokens:** Half of MaxSummaryTokens

#### Large Message Compression (Immediate)
- **Target:** Compress individual message
- **Scope:** Any message >10k tokens
- **Method:** Single-message compression
- **Prompt:** "Compress this large message while preserving all key information..."

---

## ⚡ Performance

### Token Estimation

The system uses character-based estimation (4 characters ≈ 1 token) which is:
- **Fast:** O(n) where n = message length
- **Accurate:** Within 10-20% of actual token count for most content
- **Memory Efficient:** No external dependencies or caching

### Memory Usage

- **Metadata Store:** ~200 bytes per tracked message
- **10,000 messages:** ~2MB RAM
- **100,000 messages:** ~20MB RAM

### Recommendations

#### For Long-Running Applications
```csharp
// Periodic cleanup (e.g., every 1000 messages)
if (conversation.Messages.Count % 1000 == 0)
{
    CleanupOldMetadata(conversation, metadataStore);
}

void CleanupOldMetadata(Conversation conv, MessageMetadataStore store)
{
    var currentIds = conv.Messages.Select(m => m.Id).ToHashSet();
    store.RemoveWhere(id => !currentIds.Contains(id));
}
```

#### For High-Volume Scenarios
```csharp
// Use cheaper model for summaries
var options = new ContextWindowCompressionOptions
{
    SummaryModel = ChatModel.OpenAi.Gpt4oMini, // 60x cheaper than GPT-4
    MaxSummaryTokens = 500 // Reduce tokens per summary
};
```

#### Performance Metrics
```csharp
public class CompressionMetrics
{
    public int CompressionCount { get; set; }
    public int RecompressionCount { get; set; }
    public int LargeMessageCount { get; set; }
    public int TotalTokensSaved { get; set; }
    public TimeSpan TotalCompressionTime { get; set; }
    
    public void RecordCompression(
        int tokensBefore,
        int tokensAfter,
        TimeSpan duration)
    {
        CompressionCount++;
        TotalTokensSaved += tokensBefore - tokensAfter;
        TotalCompressionTime += duration;
    }
}
```

---

## 🔄 Migration Guide

### From TornadoCompressionStrategy

The new system is designed as a drop-in replacement with enhanced capabilities.

#### Before
```csharp
var strategy = new TornadoCompressionStrategy(model, metadataStore);
var summarizer = new TornadoMessageSummarizer(client, model);
```

#### After
```csharp
var strategy = new ContextWindowCompressionStrategy(model, metadataStore);
var summarizer = new ContextWindowMessageSummarizer(client, model, metadataStore);
```

### Key Differences

1. **Configuration:** More granular control with `ContextWindowCompressionOptions`
2. **Summarizer Dependency:** Now requires `MessageMetadataStore` for state tracking
3. **Analysis:** Enhanced `ContextWindowAnalysis` with detailed breakdowns
4. **Re-compression:** Automatic re-compression of already compressed content

### Migration Checklist

- [ ] Update strategy and summarizer creation
- [ ] Pass metadataStore to summarizer constructor
- [ ] Review and adjust compression thresholds
- [ ] Update any custom compression logic
- [ ] Test with your typical conversation patterns
- [ ] Monitor memory usage with long conversations

---

## 🐛 Troubleshooting

### Issue: Messages not being compressed

**Symptoms:** Context window grows beyond expectations

**Possible Causes:**
1. Thresholds too high - most messages are system messages
2. Messages not being tracked in metadata store
3. Custom strategy logic interfering

**Solutions:**
```csharp
// 1. Check analysis output
var analysis = strategy.AnalyzeMessages(messages);
Console.WriteLine(analysis.ToString());

// 2. Verify message tracking
foreach (var message in messages)
{
    var meta = metadataStore.Get(message.Id);
    if (meta == null)
    {
        Console.WriteLine($"Message {message.Id} not tracked!");
        metadataStore.Track(message);
    }
}

// 3. Lower thresholds
var options = new ContextWindowCompressionOptions
{
    UncompressedCompressionThreshold = 0.50, // More aggressive
};
```

### Issue: Too many compressions (high API costs)

**Symptoms:** Frequent compression calls, high API usage

**Solutions:**
```csharp
// 1. Increase thresholds
var options = new ContextWindowCompressionOptions
{
    UncompressedCompressionThreshold = 0.70,
    CompressedReCompressionThreshold = 0.90
};

// 2. Use cheaper model for summaries
var options = new ContextWindowCompressionOptions
{
    SummaryModel = ChatModel.OpenAi.Gpt4oMini
};

// 3. Increase chunk size (fewer API calls)
var options = new ContextWindowCompressionOptions
{
    ChunkSize = 20000 // Double the default
};
```

### Issue: Memory usage growing over time

**Symptoms:** RAM increases with conversation length

**Solutions:**
```csharp
// 1. Implement periodic cleanup
if (conversation.Messages.Count % 500 == 0)
{
    var currentIds = conversation.Messages.Select(m => m.Id).ToHashSet();
    int removed = metadataStore.RemoveWhere(id => !currentIds.Contains(id));
    Console.WriteLine($"Cleaned up {removed} metadata entries");
}

// 2. Check memory usage
long memoryBytes = metadataStore.GetEstimatedMemoryUsage();
Console.WriteLine($"Metadata store: {memoryBytes / 1024 / 1024:F2} MB");

// 3. Clear if needed (loses tracking history)
if (memoryBytes > 100_000_000) // 100MB limit
{
    metadataStore.Clear();
    // Re-track current conversation
    foreach (var msg in conversation.Messages)
    {
        metadataStore.Track(msg);
    }
}
```

### Issue: System messages being compressed

**Symptoms:** Important instructions being lost

**Cause:** System messages should never be compressed by design

**Verification:**
```csharp
var systemMessages = messages.Where(m => m.Role == ChatMessageRoles.System);
foreach (var msg in systemMessages)
{
    var meta = metadataStore.Get(msg.Id);
    Console.WriteLine($"System message {msg.Id}: IsSystemMessage={meta?.IsSystemMessage}");
}
```

---

## 📊 Monitoring and Diagnostics

### Built-in Analysis

```csharp
// Get detailed analysis
var analysis = strategy.AnalyzeMessages(conversation.Messages);

Console.WriteLine($@"
Context Window Analysis:
  Total Window: {analysis.ContextWindowSize:N0} tokens
  Total Used: {analysis.TotalTokens:N0} tokens ({analysis.TotalUtilization:P1})
  
  Breakdown:
    System: {analysis.SystemTokens:N0} tokens
    Compressed: {analysis.CompressedTokens:N0} tokens  
    Uncompressed: {analysis.UncompressedTokens:N0} tokens
    
  Metrics:
    Compressed+System: {analysis.CompressedAndSystemUtilization:P1}
    Has Large Messages: {analysis.HasLargeMessages}
    
  Message Counts:
    System: {analysis.SystemMessages.Count}
    Compressed: {analysis.CompressedMessages.Count}
    Uncompressed: {analysis.UncompressedMessages.Count}
");
```

### Custom Logging

```csharp
public class LoggingMessageSummarizer : ContextWindowMessageSummarizer
{
    public LoggingMessageSummarizer(
        TornadoApi client,
        ChatModel model,
        MessageMetadataStore metadataStore)
        : base(client, model, metadataStore)
    {
    }
    
    public override async Task<List<ChatMessage>> SummarizeMessages(
        List<ChatMessage> messages,
        MessageCompressionOptions options,
        CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        int tokensBefore = messages.Sum(m => TokenEstimator.EstimateTokens(m));
        
        var result = await base.SummarizeMessages(messages, options, token);
        
        sw.Stop();
        int tokensAfter = result.Sum(m => TokenEstimator.EstimateTokens(m));
        
        Console.WriteLine($@"
Compression Complete:
  Messages: {messages.Count} → {result.Count}
  Tokens: {tokensBefore:N0} → {tokensAfter:N0} (saved {tokensBefore - tokensAfter:N0})
  Time: {sw.ElapsedMilliseconds}ms
  Compression Ratio: {(double)tokensAfter / tokensBefore:P1}
");
        
        return result;
    }
}
```

---

## 📚 Additional Resources

- **Tests:** See `LlmTornado.Tests/ContextController/` for comprehensive test examples
- **Source Code:** `LlmTornado.Agents.Samples/ContextController/`
- **API Documentation:** XML comments in source files

## 📝 License

[Your license information here]

## 🤝 Contributing

[Your contributing guidelines here]

---

**Last Updated:** 2025-01-26  
**Version:** 1.0.0  
**Status:** Production Ready ✅
