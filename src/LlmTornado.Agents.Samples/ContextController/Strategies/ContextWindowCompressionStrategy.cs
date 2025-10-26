using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
/// Analysis result of context window utilization.
/// </summary>
public class ContextWindowAnalysis
{
    public int ContextWindowSize { get; set; }
    public int SystemTokens { get; set; }
    public int CompressedTokens { get; set; }
    public int UncompressedTokens { get; set; }
    public int TotalTokens { get; set; }
    public double TotalUtilization { get; set; }
    public double CompressedAndSystemUtilization { get; set; }
    public bool HasLargeMessages { get; set; }
    public List<ChatMessage> SystemMessages { get; set; } = new();
    public List<ChatMessage> CompressedMessages { get; set; } = new();
    public List<ChatMessage> UncompressedMessages { get; set; } = new();

    /// <summary>
    /// Gets the compression recommendation based on analysis
    /// </summary>
    public string GetRecommendation()
    {
        if (HasLargeMessages)
            return "Compression recommended: Large messages detected (>10k tokens)";
        
        if (TotalUtilization >= 0.80)
            return $"Compression urgently needed: {TotalUtilization:P1} utilization (critical level)";
        
        if (TotalUtilization >= 0.60)
            return $"Compression recommended: {TotalUtilization:P1} utilization";
        
        if (CompressedAndSystemUtilization >= 0.80)
            return $"Re-compression recommended: {CompressedAndSystemUtilization:P1} compressed+system utilization";
        
        return $"No compression needed: {TotalUtilization:P1} utilization (healthy)";
    }

    /// <summary>
    /// Gets detailed statistics as a dictionary
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            ["ContextWindowSize"] = ContextWindowSize,
            ["TotalTokens"] = TotalTokens,
            ["TotalUtilization"] = TotalUtilization,
            ["SystemTokens"] = SystemTokens,
            ["CompressedTokens"] = CompressedTokens,
            ["UncompressedTokens"] = UncompressedTokens,
            ["CompressedAndSystemUtilization"] = CompressedAndSystemUtilization,
            ["HasLargeMessages"] = HasLargeMessages,
            ["SystemMessageCount"] = SystemMessages.Count,
            ["CompressedMessageCount"] = CompressedMessages.Count,
            ["UncompressedMessageCount"] = UncompressedMessages.Count,
            ["Recommendation"] = GetRecommendation()
        };
    }

    public override string ToString()
    {
        return $@"Context Window Analysis:
- Total Window: {ContextWindowSize} tokens
- Total Used: {TotalTokens} tokens ({TotalUtilization:P1})
- System: {SystemTokens} tokens
- Compressed: {CompressedTokens} tokens
- Uncompressed: {UncompressedTokens} tokens
- Compressed+System: {CompressedAndSystemUtilization:P1}
- Has Large Messages: {HasLargeMessages}
- Recommendation: {GetRecommendation()}";
    }
}

/// <summary>
/// Compression metrics for tracking performance and behavior
/// </summary>
public class CompressionMetrics
{
    private long _totalAnalysisCalls;
    private long _totalCompressionChecks;
    private long _totalCompressionsTriggered;
    private long _totalAnalysisDurationMs;
    private readonly object _lock = new();

    /// <summary>
    /// Total number of analysis operations performed
    /// </summary>
    public long TotalAnalysisCalls
    {
        get { lock (_lock) return _totalAnalysisCalls; }
    }

    /// <summary>
    /// Total number of compression checks performed
    /// </summary>
    public long TotalCompressionChecks
    {
        get { lock (_lock) return _totalCompressionChecks; }
    }

    /// <summary>
    /// Total number of times compression was triggered
    /// </summary>
    public long TotalCompressionsTriggered
    {
        get { lock (_lock) return _totalCompressionsTriggered; }
    }

    /// <summary>
    /// Total time spent in analysis operations (milliseconds)
    /// </summary>
    public long TotalAnalysisDurationMs
    {
        get { lock (_lock) return _totalAnalysisDurationMs; }
    }

    /// <summary>
    /// Average analysis duration in milliseconds
    /// </summary>
    public double AverageAnalysisDurationMs
    {
        get
        {
            lock (_lock)
            {
                return _totalAnalysisCalls > 0
                    ? (double)_totalAnalysisDurationMs / _totalAnalysisCalls
                    : 0;
            }
        }
    }

    /// <summary>
    /// Compression trigger rate (compressions / checks)
    /// </summary>
    public double CompressionTriggerRate
    {
        get
        {
            lock (_lock)
            {
                return _totalCompressionChecks > 0
                    ? (double)_totalCompressionsTriggered / _totalCompressionChecks
                    : 0;
            }
        }
    }

    internal void RecordAnalysis(long durationMs)
    {
        lock (_lock)
        {
            _totalAnalysisCalls++;
            _totalAnalysisDurationMs += durationMs;
        }
    }

    internal void RecordCompressionCheck(bool triggered)
    {
        lock (_lock)
        {
            _totalCompressionChecks++;
            if (triggered)
                _totalCompressionsTriggered++;
        }
    }

    /// <summary>
    /// Resets all metrics to zero
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _totalAnalysisCalls = 0;
            _totalCompressionChecks = 0;
            _totalCompressionsTriggered = 0;
            _totalAnalysisDurationMs = 0;
        }
    }

    public override string ToString()
    {
        return $@"Compression Metrics:
- Analysis Calls: {TotalAnalysisCalls:N0}
- Compression Checks: {TotalCompressionChecks:N0}
- Compressions Triggered: {TotalCompressionsTriggered:N0}
- Trigger Rate: {CompressionTriggerRate:P1}
- Avg Analysis Time: {AverageAnalysisDurationMs:F2}ms
- Total Analysis Time: {TotalAnalysisDurationMs:N0}ms";
    }
}

/// <summary>
/// Advanced compression strategy that manages context window utilization
/// by compressing messages based on configurable thresholds.
/// </summary>
public class ContextWindowCompressionStrategy : IMessagesCompressionStrategy
{
    private readonly ChatModel _model;
    private readonly MessageMetadataStore _metadataStore;
    private readonly ContextWindowCompressionOptions _options;
    private readonly CompressionMetrics _metrics;

    /// <summary>
    /// Creates a new context window compression strategy.
    /// </summary>
    /// <param name="model">The chat model being used</param>
    /// <param name="metadataStore">Store for tracking message metadata</param>
    /// <param name="options">Optional compression options</param>
    public ContextWindowCompressionStrategy(
        ChatModel model,
        MessageMetadataStore metadataStore,
        ContextWindowCompressionOptions? options = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
        _options = options ?? new ContextWindowCompressionOptions();
        _metrics = new CompressionMetrics();
    }

    /// <summary>
    /// Gets the current compression metrics
    /// </summary>
    public CompressionMetrics Metrics => _metrics;

    /// <summary>
    /// Determines if compression should occur based on the strategy rules.
    /// </summary>
    /// <param name="messages">The messages to evaluate</param>
    /// <returns>True if compression should occur</returns>
    public bool ShouldCompress(List<ChatMessage> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            _metrics.RecordCompressionCheck(false);
            return false;
        }

        var analysis = AnalyzeMessages(messages);
        bool shouldCompress = false;
        string reason = string.Empty;

        // Rule 1: Check for large messages (>10k tokens)
        if (analysis.HasLargeMessages)
        {
            shouldCompress = true;
            reason = "Large messages detected";
        }
        // Rule 2: Check if total usage exceeds 60%
        else if (analysis.TotalUtilization >= _options.UncompressedCompressionThreshold)
        {
            shouldCompress = true;
            reason = $"Total utilization {analysis.TotalUtilization:P1} exceeds threshold {_options.UncompressedCompressionThreshold:P1}";
        }
        // Rule 3: Check if compressed + system exceeds 80%
        else if (analysis.CompressedAndSystemUtilization >= _options.CompressedReCompressionThreshold)
        {
            shouldCompress = true;
            reason = $"Compressed+System utilization {analysis.CompressedAndSystemUtilization:P1} exceeds threshold {_options.CompressedReCompressionThreshold:P1}";
        }

        _metrics.RecordCompressionCheck(shouldCompress);

        if (_options.EnableLogging)
        {
            var logMessage = shouldCompress
                ? $"[ContextWindowCompressionStrategy] Compression triggered: {reason}"
                : $"[ContextWindowCompressionStrategy] No compression needed: {analysis.TotalUtilization:P1} utilization";
            
            Log(logMessage);
        }

        return shouldCompress;
    }

    /// <summary>
    /// Gets the compression options to use based on current message state.
    /// </summary>
    /// <param name="messages">The messages being compressed</param>
    /// <returns>Compression options</returns>
    public MessageCompressionOptions GetCompressionOptions(List<ChatMessage> messages)
    {
        var analysis = AnalyzeMessages(messages);

        var options = new MessageCompressionOptions
        {
            ChunkSize = _options.ChunkSize,
            PreserveSystemmessages = true, // Always preserve system messages
            CompressToolCallmessages = _options.CompressToolCallmessages,
            SummaryModel = _options.SummaryModel,
            SummaryPrompt = DeterminePrompt(analysis),
            MaxSummaryTokens = DetermineMaxTokens(analysis)
        };

        if (_options.EnableLogging)
        {
            Log($"[ContextWindowCompressionStrategy] Compression options: Prompt={options.SummaryPrompt.Substring(0, Math.Min(50, options.SummaryPrompt.Length))}..., MaxTokens={options.MaxSummaryTokens}");
        }

        return options;
    }

    /// <summary>
    /// Analyzes the current message set to determine compression needs.
    /// </summary>
    /// <param name="messages">The messages to analyze</param>
    /// <returns>Analysis results</returns>
    public ContextWindowAnalysis AnalyzeMessages(List<ChatMessage> messages)
    {
        var sw = Stopwatch.StartNew();

        int contextWindow = TokenEstimator.GetContextWindowSize(_model);

        // Categorize messages - system messages are never compressed
        var systemMessages = messages.Where(m => m.Role == ChatMessageRoles.System).ToList();
        
        var compressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Compressed)
            .Concat(_metadataStore.GetOldestByState(messages, CompressionState.ReCompressed))
            .ToList();
        
        // Get uncompressed messages, excluding system messages
        var uncompressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Uncompressed)
            .Where(m => m.Role != ChatMessageRoles.System)
            .ToList();

        // Calculate token counts
        int systemTokens = systemMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int compressedTokens = compressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int uncompressedTokens = uncompressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int totalTokens = systemTokens + compressedTokens + uncompressedTokens;

        // Check for large messages
        bool hasLargeMessages = _metadataStore.GetLargeMessages(
            uncompressedMessages, _options.LargeMessageThreshold).Any();

        var analysis = new ContextWindowAnalysis
        {
            ContextWindowSize = contextWindow,
            SystemTokens = systemTokens,
            CompressedTokens = compressedTokens,
            UncompressedTokens = uncompressedTokens,
            TotalTokens = totalTokens,
            TotalUtilization = TokenEstimator.CalculateUtilization(totalTokens, contextWindow),
            CompressedAndSystemUtilization = TokenEstimator.CalculateUtilization(
                systemTokens + compressedTokens, contextWindow),
            HasLargeMessages = hasLargeMessages,
            SystemMessages = systemMessages,
            CompressedMessages = compressedMessages,
            UncompressedMessages = uncompressedMessages
        };

        sw.Stop();
        _metrics.RecordAnalysis(sw.ElapsedMilliseconds);

        if (_options.EnableLogging)
        {
            Log($"[ContextWindowCompressionStrategy] Analysis complete in {sw.ElapsedMilliseconds}ms:\n{analysis}");
        }

        return analysis;
    }

    /// <summary>
    /// Determines the appropriate prompt based on compression type needed.
    /// </summary>
    private string DeterminePrompt(ContextWindowAnalysis analysis)
    {
        if (analysis.CompressedAndSystemUtilization >= _options.CompressedReCompressionThreshold)
        {
            return _options.ReCompressionPrompt;
        }
        return _options.InitialCompressionPrompt;
    }

    /// <summary>
    /// Determines the maximum tokens for summaries based on compression type.
    /// </summary>
    private int DetermineMaxTokens(ContextWindowAnalysis analysis)
    {
        if (analysis.CompressedAndSystemUtilization >= _options.CompressedReCompressionThreshold)
        {
            // More aggressive compression for re-compression
            return _options.MaxSummaryTokens / 2;
        }
        return _options.MaxSummaryTokens;
    }

    /// <summary>
    /// Logs a message using the configured logger or Console
    /// </summary>
    private void Log(string message)
    {
        if (_options.LogAction != null)
        {
            _options.LogAction(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Gets the current compression options.
    /// </summary>
    public ContextWindowCompressionOptions Options => _options;
}
