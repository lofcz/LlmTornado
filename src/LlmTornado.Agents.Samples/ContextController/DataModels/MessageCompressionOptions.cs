using LlmTornado.Chat.Models;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
///     Options for messages compression.
/// </summary>
public class MessageCompressionOptions
{
    /// <summary>
    ///     The approximate character count per chunk (default: 10000).
    /// </summary>
    public int ChunkSize { get; set; } = 10000;

    /// <summary>
    ///     Whether to preserve system messages (default: true).
    /// </summary>
    public bool PreserveSystemmessages { get; set; } = true;

    /// <summary>
    ///     Whether to Compress Tool call messages (default: true).
    /// </summary>
    public bool CompressToolCallmessages { get; set; } = true;

    /// <summary>
    ///     The model to use for summarization.
    /// </summary>
    public ChatModel? SummaryModel { get; set; }

    /// <summary>
    ///     Custom prompt for summarization.
    /// </summary>
    public string SummaryPrompt { get; set; } = "Summarize this chat messages concisely, preserving key information:";

    /// <summary>
    ///     Maximum tokens for each summary (default: 1000).
    /// </summary>
    public int MaxSummaryTokens { get; set; } = 1000;
}
