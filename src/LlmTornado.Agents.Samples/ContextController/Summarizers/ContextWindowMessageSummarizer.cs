using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
/// Analysis of compression needs for a message set.
/// </summary>
public class CompressionAnalysis
{
    public List<ChatMessage> LargeMessages { get; set; } = new();
    public List<ChatMessage> UncompressedMessages { get; set; } = new();
    public List<ChatMessage> CompressedMessages { get; set; } = new();
    public List<ChatMessage> SystemMessages { get; set; } = new();
    public bool NeedsUncompressedCompression { get; set; }
    public bool NeedsReCompression { get; set; }
    public double TargetUtilization { get; set; }
    public double ReCompressionTarget { get; set; }
}

/// <summary>
/// Metrics for tracking summarization performance
/// </summary>
public class SummarizationMetrics
{
    private long _totalSummarizations;
    private long _largeMessageCompressions;
    private long _uncompressedCompressions;
    private long _recompressions;
    private long _totalMessagesBefore;
    private long _totalMessagesAfter;
    private long _totalTokensBefore;
    private long _totalTokensAfter;
    private long _totalDurationMs;
    private readonly object _lock = new();

    public long TotalSummarizations
    {
        get { lock (_lock) return _totalSummarizations; }
    }

    public long LargeMessageCompressions
    {
        get { lock (_lock) return _largeMessageCompressions; }
    }

    public long UncompressedCompressions
    {
        get { lock (_lock) return _uncompressedCompressions; }
    }

    public long Recompressions
    {
        get { lock (_lock) return _recompressions; }
    }

    public long TotalMessagesBefore
    {
        get { lock (_lock) return _totalMessagesBefore; }
    }

    public long TotalMessagesAfter
    {
        get { lock (_lock) return _totalMessagesAfter; }
    }

    public long TotalTokensBefore
    {
        get { lock (_lock) return _totalTokensBefore; }
    }

    public long TotalTokensAfter
    {
        get { lock (_lock) return _totalTokensAfter; }
    }

    public long TotalDurationMs
    {
        get { lock (_lock) return _totalDurationMs; }
    }

    public double AverageDurationMs
    {
        get
        {
            lock (_lock)
            {
                return _totalSummarizations > 0
                    ? (double)_totalDurationMs / _totalSummarizations
                    : 0;
            }
        }
    }

    public double AverageCompressionRatio
    {
        get
        {
            lock (_lock)
            {
                return _totalTokensBefore > 0
                    ? (double)_totalTokensAfter / _totalTokensBefore
                    : 0;
            }
        }
    }

    public long TotalTokensSaved
    {
        get { lock (_lock) return _totalTokensBefore - _totalTokensAfter; }
    }

    internal void RecordSummarization(
        int messagesBefore,
        int messagesAfter,
        int tokensBefore,
        int tokensAfter,
        long durationMs,
        string type)
    {
        lock (_lock)
        {
            _totalSummarizations++;
            _totalMessagesBefore += messagesBefore;
            _totalMessagesAfter += messagesAfter;
            _totalTokensBefore += tokensBefore;
            _totalTokensAfter += tokensAfter;
            _totalDurationMs += durationMs;

            switch (type)
            {
                case "large":
                    _largeMessageCompressions++;
                    break;
                case "uncompressed":
                    _uncompressedCompressions++;
                    break;
                case "recompressed":
                    _recompressions++;
                    break;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _totalSummarizations = 0;
            _largeMessageCompressions = 0;
            _uncompressedCompressions = 0;
            _recompressions = 0;
            _totalMessagesBefore = 0;
            _totalMessagesAfter = 0;
            _totalTokensBefore = 0;
            _totalTokensAfter = 0;
            _totalDurationMs = 0;
        }
    }

    public override string ToString()
    {
        return $@"Summarization Metrics:
- Total Summarizations: {TotalSummarizations:N0}
  - Large Messages: {LargeMessageCompressions:N0}
  - Uncompressed: {UncompressedCompressions:N0}
  - Re-compressions: {Recompressions:N0}
- Messages: {TotalMessagesBefore:N0} ? {TotalMessagesAfter:N0}
- Tokens: {TotalTokensBefore:N0} ? {TotalTokensAfter:N0} (saved {TotalTokensSaved:N0})
- Compression Ratio: {AverageCompressionRatio:P1}
- Avg Duration: {AverageDurationMs:F2}ms
- Total Duration: {TotalDurationMs:N0}ms";
    }
}

/// <summary>
/// Advanced message summarizer that implements selective compression
/// based on context window utilization targets.
/// </summary>
public class ContextWindowMessageSummarizer : IMessagesSummarizer
{
    private readonly TornadoApi _client;
    private readonly ChatModel _model;
    private readonly MessageMetadataStore _metadataStore;
    private readonly SummarizationMetrics _metrics;
    private readonly bool _enableLogging;
    private readonly Action<string>? _logAction;

    /// <summary>
    /// Creates a new context window message summarizer
    /// </summary>
    /// <param name="client">Tornado API client</param>
    /// <param name="model">Chat model to use</param>
    /// <param name="metadataStore">Message metadata store</param>
    /// <param name="enableLogging">Enable detailed logging</param>
    /// <param name="logAction">Custom log action (null = Console)</param>
    public ContextWindowMessageSummarizer(
        TornadoApi client,
        ChatModel model,
        MessageMetadataStore metadataStore,
        bool enableLogging = false,
        Action<string>? logAction = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
        _metrics = new SummarizationMetrics();
        _enableLogging = enableLogging;
        _logAction = logAction;
    }

    /// <summary>
    /// Gets the current summarization metrics
    /// </summary>
    public SummarizationMetrics Metrics => _metrics;

    public async Task<List<ChatMessage>> SummarizeMessages(
        List<ChatMessage> messages,
        MessageCompressionOptions options,
        CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        int tokensBefore = messages.Sum(m => TokenEstimator.EstimateTokens(m));

        Log($"[ContextWindowMessageSummarizer] Starting summarization for {messages.Count} messages ({tokensBefore:N0} tokens)");

        var analysis = AnalyzeCompressionNeeds(messages, options);

        List<ChatMessage> result = new();

        // Step 1: Handle large messages (>10k tokens)
        if (analysis.LargeMessages.Any())
        {
            Log($"[ContextWindowMessageSummarizer] Compressing {analysis.LargeMessages.Count} large messages");
            var largeSummaries = await CompressLargeMessages(
                analysis.LargeMessages, options, token);
            result.AddRange(largeSummaries);
        }

        // Step 2: Handle uncompressed compression (60% threshold)
        if (analysis.NeedsUncompressedCompression)
        {
            Log($"[ContextWindowMessageSummarizer] Compressing {analysis.UncompressedMessages.Count} uncompressed messages to target {analysis.TargetUtilization:P1}");
            var uncompressedSummaries = await CompressToTarget(
                analysis.UncompressedMessages,
                analysis.TargetUtilization,
                options,
                token);
            result.AddRange(uncompressedSummaries);
        }

        // Step 3: Handle re-compression (80% threshold)
        if (analysis.NeedsReCompression)
        {
            Log($"[ContextWindowMessageSummarizer] Re-compressing {analysis.CompressedMessages.Count} compressed messages to target {analysis.ReCompressionTarget:P1}");
            var recompressedSummaries = await ReCompressToTarget(
                analysis.CompressedMessages,
                analysis.ReCompressionTarget,
                options,
                token);
            result.AddRange(recompressedSummaries);
        }

        sw.Stop();
        int tokensAfter = result.Sum(m => TokenEstimator.EstimateTokens(m));

        // Record metrics
        string type = analysis.LargeMessages.Any() ? "large" :
                     analysis.NeedsUncompressedCompression ? "uncompressed" : "recompressed";
        _metrics.RecordSummarization(
            messages.Count,
            result.Count,
            tokensBefore,
            tokensAfter,
            sw.ElapsedMilliseconds,
            type);

        Log($"[ContextWindowMessageSummarizer] Summarization complete: {messages.Count} ? {result.Count} messages, {tokensBefore:N0} ? {tokensAfter:N0} tokens ({sw.ElapsedMilliseconds}ms)");

        return result;
    }

    private async Task<List<ChatMessage>> CompressLargeMessages(
        List<ChatMessage> largeMessages,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        List<ChatMessage> summaries = new();

        foreach (var message in largeMessages)
        {
            Log($"[ContextWindowMessageSummarizer] Compressing large message {message.Id} ({TokenEstimator.EstimateTokens(message):N0} tokens)");
            var summary = await SummarizeSingleMessage(message, options, token);
            summaries.Add(summary);

            // Update metadata
            _metadataStore.UpdateState(message.Id, CompressionState.Compressed);
        }

        return summaries;
    }

    private async Task<List<ChatMessage>> CompressToTarget(
        List<ChatMessage> uncompressedMessages,
        double targetUtilization,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        int contextWindow = TokenEstimator.GetContextWindowSize(_model);
        int targetTokens = (int)(contextWindow * targetUtilization);

        // Sort by oldest first
        var sortedMessages = _metadataStore.GetOldestByState(
            uncompressedMessages, CompressionState.Uncompressed);

        List<ChatMessage> toCompress = new();
        int currentTokens = uncompressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));

        // Select oldest messages until we reach target
        foreach (var message in sortedMessages)
        {
            if (currentTokens <= targetTokens)
                break;

            toCompress.Add(message);
            currentTokens -= TokenEstimator.EstimateTokens(message);
        }

        if (!toCompress.Any())
        {
            Log($"[ContextWindowMessageSummarizer] No messages to compress (already at target)");
            return new List<ChatMessage>();
        }

        Log($"[ContextWindowMessageSummarizer] Compressing {toCompress.Count} oldest messages");

        // Group into chunks and compress
        var chunks = CreateChunks(toCompress, options.ChunkSize);
        var summaryTasks = chunks.Select(chunk =>
            SummarizeChunk(_client, chunk, options, token)).ToArray();
        var summaries = await Task.WhenAll(summaryTasks);

        List<ChatMessage> result = new();
        foreach (var summary in summaries)
        {
            if (!string.IsNullOrWhiteSpace(summary))
            {
                var summaryMessage = new ChatMessage(
                    ChatMessageRoles.Assistant,
                    $"[Compressed summary]: {summary}");
                result.Add(summaryMessage);

                // Track as compressed
                _metadataStore.Track(summaryMessage, CompressionState.Compressed);
            }
        }

        // Update metadata for compressed messages
        foreach (var message in toCompress)
        {
            _metadataStore.UpdateState(message.Id, CompressionState.Compressed);
        }

        return result;
    }

    private async Task<List<ChatMessage>> ReCompressToTarget(
        List<ChatMessage> compressedMessages,
        double targetUtilization,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        int contextWindow = TokenEstimator.GetContextWindowSize(_model);
        int targetTokens = (int)(contextWindow * targetUtilization);

        // Sort by oldest first (including both Compressed and ReCompressed)
        var sortedMessages = _metadataStore.GetOldestByState(
            compressedMessages, CompressionState.Compressed)
            .Concat(_metadataStore.GetOldestByState(
                compressedMessages, CompressionState.ReCompressed))
            .OrderBy(m =>
            {
                var meta = _metadataStore.Get(m.Id);
                return meta?.OriginalTimestamp ?? DateTime.MaxValue;
            })
            .ToList();

        List<ChatMessage> toReCompress = new();
        int currentTokens = compressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));

        // Select oldest compressed messages until we reach target
        foreach (var message in sortedMessages)
        {
            if (currentTokens <= targetTokens)
                break;

            toReCompress.Add(message);
            currentTokens -= TokenEstimator.EstimateTokens(message);
        }

        if (!toReCompress.Any())
        {
            Log($"[ContextWindowMessageSummarizer] No messages to re-compress (already at target)");
            return new List<ChatMessage>();
        }

        Log($"[ContextWindowMessageSummarizer] Re-compressing {toReCompress.Count} oldest compressed messages");

        // Use more aggressive compression for re-compression
        var reCompressionOptions = new MessageCompressionOptions
        {
            ChunkSize = options.ChunkSize * 2, // Larger chunks for re-compression
            PreserveSystemmessages = true,
            CompressToolCallmessages = options.CompressToolCallmessages,
            SummaryModel = options.SummaryModel,
            SummaryPrompt = "Create an ultra-concise summary focusing only on absolutely critical information:",
            MaxSummaryTokens = options.MaxSummaryTokens / 2 // Half the tokens
        };

        var chunks = CreateChunks(toReCompress, reCompressionOptions.ChunkSize);
        var summaryTasks = chunks.Select(chunk =>
            SummarizeChunk(_client, chunk, reCompressionOptions, token)).ToArray();
        var summaries = await Task.WhenAll(summaryTasks);

        List<ChatMessage> result = new();
        foreach (var summary in summaries)
        {
            if (!string.IsNullOrWhiteSpace(summary))
            {
                var summaryMessage = new ChatMessage(
                    ChatMessageRoles.Assistant,
                    $"[Re-compressed summary]: {summary}");
                result.Add(summaryMessage);

                // Track as re-compressed
                _metadataStore.Track(summaryMessage, CompressionState.ReCompressed);
            }
        }

        // Update metadata for re-compressed messages
        foreach (var message in toReCompress)
        {
            _metadataStore.UpdateState(message.Id, CompressionState.ReCompressed);
        }

        return result;
    }

    private async Task<ChatMessage> SummarizeSingleMessage(
        ChatMessage message,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        var conversation = _client.Chat.CreateConversation(options.SummaryModel);
        conversation.RequestParameters.Messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRoles.System,
                "Compress this large message while preserving all key information:"),
            message
        };
        conversation.RequestParameters.MaxTokens = options.MaxSummaryTokens;
        conversation.RequestParameters.Temperature = 0.3;
        conversation.RequestParameters.CancellationToken = token;

        var result = (await conversation.GetResponseRichSafe())?.Data?.Result;
        var content = result?.Choices?[0]?.Message?.Content ?? "[Compression failed]";

        return new ChatMessage(ChatMessageRoles.Assistant,
            $"[Large message compressed]: {content}");
    }

    private List<List<ChatMessage>> CreateChunks(
        List<ChatMessage> messages,
        int chunkSize)
    {
        List<List<ChatMessage>> chunks = new();
        List<ChatMessage> currentChunk = new();
        int currentLength = 0;

        foreach (var message in messages)
        {
            int messageLength = message.GetMessageLength();

            if (currentLength + messageLength > chunkSize && currentChunk.Any())
            {
                chunks.Add(currentChunk);
                currentChunk = new();
                currentLength = 0;
            }

            currentChunk.Add(message);
            currentLength += messageLength;
        }

        if (currentChunk.Any())
        {
            chunks.Add(currentChunk);
        }

        Log($"[ContextWindowMessageSummarizer] Created {chunks.Count} chunks from {messages.Count} messages");

        return chunks;
    }

    private async Task<string> SummarizeChunk(
        TornadoApi client,
        List<ChatMessage> chunk,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        StringBuilder chunkText = new StringBuilder();

        foreach (var msg in chunk)
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
            Log($"[ContextWindowMessageSummarizer] Summarizing chunk of {chunk.Count} messages ({chunkText.Length} characters)");

            Conversation conversation = client.Chat.CreateConversation(options.SummaryModel);
            conversation.AddSystemMessage(options.SummaryPrompt);
            conversation.AddUserMessage(chunkText.ToString());
            conversation.RequestParameters.MaxTokens = options.MaxSummaryTokens;
            conversation.RequestParameters.Temperature = 0.3;
            conversation.RequestParameters.CancellationToken = token;
            RestDataOrException<ChatRichResponse> response = await conversation.GetResponseRichSafe();
            if(response.Exception != null)
            {
                Log($"[ContextWindowMessageSummarizer] Error during summarization: {response.Exception.Message}");
                return $"[{chunk.Count} messages from this conversation]";
            }
            else
            {
                if (response.Data == null)
                {
                    Log($"[ContextWindowMessageSummarizer] No data in response");
                    return $"[{chunk.Count} messages from this conversation]";
                }
            }
            ChatResult? result = response.Data.Result;
            var summary = result?.Choices?[0]?.Message?.Content ?? string.Empty;
            
            Log($"[ContextWindowMessageSummarizer] Chunk summarized: {chunkText.Length} chars ? {summary.Length} chars");
            
            return summary;
        }
        catch (Exception ex)
        {
            Log($"[ContextWindowMessageSummarizer] Exception during summarization: {ex.Message}");
            return $"[{chunk.Count} messages from this conversation]";
        }
    }

    private CompressionAnalysis AnalyzeCompressionNeeds(
        List<ChatMessage> messages,
        MessageCompressionOptions options)
    {
        int contextWindow = TokenEstimator.GetContextWindowSize(_model);

        var systemMessages = messages.Where(m => m.Role == ChatMessageRoles.System).ToList();
        var compressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Compressed)
            .Concat(_metadataStore.GetOldestByState(messages, CompressionState.ReCompressed))
            .ToList();
        var uncompressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Uncompressed).ToList();

        int systemTokens = systemMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int compressedTokens = compressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int uncompressedTokens = uncompressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int totalTokens = systemTokens + compressedTokens + uncompressedTokens;

        double totalUtilization = TokenEstimator.CalculateUtilization(totalTokens, contextWindow);
        double compressedAndSystemUtilization = TokenEstimator.CalculateUtilization(
            systemTokens + compressedTokens, contextWindow);

        var largeMessages = uncompressedMessages
            .Where(m => TokenEstimator.EstimateTokens(m) > 10000)
            .ToList();

        return new CompressionAnalysis
        {
            LargeMessages = largeMessages,
            UncompressedMessages = uncompressedMessages.Except(largeMessages).ToList(),
            CompressedMessages = compressedMessages,
            SystemMessages = systemMessages,
            NeedsUncompressedCompression = totalUtilization >= 0.60,
            NeedsReCompression = compressedAndSystemUtilization >= 0.80,
            TargetUtilization = 0.40,
            ReCompressionTarget = 0.20
        };
    }

    private void Log(string message)
    {
        if (_enableLogging)
        {
            if (_logAction != null)
            {
                _logAction(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}
