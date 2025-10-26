using LlmTornado.Chat.Models;

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

    /// <summary>
    /// Enable detailed logging (default: false)
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Custom logger action (receives log messages)
    /// </summary>
    public Action<string>? LogAction { get; set; }
}
