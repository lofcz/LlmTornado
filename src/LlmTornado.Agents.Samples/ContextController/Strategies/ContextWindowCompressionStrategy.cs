using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
/// Configuration options for context window-based compression strategy.
/// </summary>
public class ContextWindowCompressionOptions
{
    /// <summary>
    /// Target utilization percentage (default: 0.40 = 40%)
    /// </summary>
    public double TargetUtilization { get; set; } = 0.40;

    /// <summary>
    /// Threshold to trigger uncompressed message compression (default: 0.60 = 60%)
    /// </summary>
    public double UncompressedCompressionThreshold { get; set; } = 0.60;

    /// <summary>
    /// Threshold to trigger re-compression of compressed messages (default: 0.80 = 80%)
    /// </summary>
    public double CompressedReCompressionThreshold { get; set; } = 0.80;

    /// <summary>
    /// Target for compressed messages + system messages after re-compression (default: 0.20 = 20%)
    /// </summary>
    public double ReCompressionTarget { get; set; } = 0.20;

    /// <summary>
    /// Token threshold for large messages (default: 10000)
    /// </summary>
    public int LargeMessageThreshold { get; set; } = 10000;

    /// <summary>
    /// Chunk size for compression (default: 10000 characters)
    /// </summary>
    public int ChunkSize { get; set; } = 10000;

    /// <summary>
    /// Whether to compress tool call messages (default: true)
    /// </summary>
    public bool CompressToolCallmessages { get; set; } = true;

    /// <summary>
    /// Model to use for summarization
    /// </summary>
    public ChatModel? SummaryModel { get; set; }

    /// <summary>
    /// Prompt for initial compression
    /// </summary>
    public string InitialCompressionPrompt { get; set; } =
        "Summarize these messages concisely while preserving all key information, decisions, and context:";

    /// <summary>
    /// Prompt for re-compression of already compressed messages
    /// </summary>
    public string ReCompressionPrompt { get; set; } =
        "Create an even more concise summary of these already-summarized messages, focusing only on critical information:";

    /// <summary>
    /// Maximum tokens for each summary (default: 1000)
    /// </summary>
    public int MaxSummaryTokens { get; set; } = 1000;
}

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

    public override string ToString()
    {
        return $@"Context Window Analysis:
- Total Window: {ContextWindowSize} tokens
- Total Used: {TotalTokens} tokens ({TotalUtilization:P1})
- System: {SystemTokens} tokens
- Compressed: {CompressedTokens} tokens
- Uncompressed: {UncompressedTokens} tokens
- Compressed+System: {CompressedAndSystemUtilization:P1}
- Has Large Messages: {HasLargeMessages}";
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
    }

    /// <summary>
    /// Determines if compression should occur based on the strategy rules.
    /// </summary>
    /// <param name="messages">The messages to evaluate</param>
    /// <returns>True if compression should occur</returns>
    public bool ShouldCompress(List<ChatMessage> messages)
    {
        if (messages == null || messages.Count == 0)
            return false;

        var analysis = AnalyzeMessages(messages);

        // Rule 1: Check for large messages (>10k tokens)
        if (analysis.HasLargeMessages)
            return true;

        // Rule 2: Check if total usage exceeds 60%
        if (analysis.TotalUtilization >= _options.UncompressedCompressionThreshold)
            return true;

        // Rule 3: Check if compressed + system exceeds 80%
        if (analysis.CompressedAndSystemUtilization >= _options.CompressedReCompressionThreshold)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the compression options to use based on current message state.
    /// </summary>
    /// <param name="messages">The messages being compressed</param>
    /// <returns>Compression options</returns>
    public MessageCompressionOptions GetCompressionOptions(List<ChatMessage> messages)
    {
        var analysis = AnalyzeMessages(messages);

        return new MessageCompressionOptions
        {
            ChunkSize = _options.ChunkSize,
            PreserveSystemmessages = true, // Always preserve system messages
            CompressToolCallmessages = _options.CompressToolCallmessages,
            SummaryModel = _options.SummaryModel,
            SummaryPrompt = DeterminePrompt(analysis),
            MaxSummaryTokens = DetermineMaxTokens(analysis)
        };
    }

    /// <summary>
    /// Analyzes the current message set to determine compression needs.
    /// </summary>
    /// <param name="messages">The messages to analyze</param>
    /// <returns>Analysis results</returns>
    public ContextWindowAnalysis AnalyzeMessages(List<ChatMessage> messages)
    {
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

        return new ContextWindowAnalysis
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
    /// Gets the current compression options.
    /// </summary>
    public ContextWindowCompressionOptions Options => _options;
}
