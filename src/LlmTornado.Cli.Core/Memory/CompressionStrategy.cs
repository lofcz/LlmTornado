using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Estimated token costs for various media types.
/// Based on OpenAI's vision token formula and practical estimates.
/// </summary>
public static class MediaTokenCosts
{
    /// <summary> Low-detail image: ~85 tokens. </summary>
    public const int ImageLow = 85;
    /// <summary> High-detail image: ~765 tokens (average). </summary>
    public const int ImageHigh = 765;
    /// <summary> Auto-detail image: use high estimate to be conservative. </summary>
    public const int ImageDefault = 765;
    /// <summary> PDF document: ~1000 tokens per page estimate. </summary>
    public const int Document = 1000;
    /// <summary> Audio: ~500 tokens per audio attachment. </summary>
    public const int Audio = 500;
}

/// <summary>
/// Tracks compression state per message.
/// </summary>
public enum MessageCompressionState
{
    Uncompressed,
    Compressed,
    ReCompressed,
}

/// <summary>
/// Determines when context compression is needed based on token utilization thresholds.
/// </summary>
public sealed class CompressionStrategy
{
    private int _contextWindowTokens;

    public double UncompressedThreshold { get; set; } = 0.60;
    public double ReCompressionThreshold { get; set; } = 0.80;
    public double TargetUtilization { get; set; } = 0.40;
    public double ReCompressionTarget { get; set; } = 0.20;
    public int LargeMessageThreshold { get; set; } = 10_000;

    public CompressionStrategy(int contextWindowTokens)
    {
        _contextWindowTokens = Math.Max(contextWindowTokens, 4096);
    }

    public void UpdateContextWindow(int tokens) => _contextWindowTokens = Math.Max(tokens, 4096);

    public CompressionAnalysis Analyze(
        List<ChatMessage> messages,
        MessageMetadataTracker metadata)
    {
        int systemTokens = 0;
        int uncompressedTokens = 0;
        int compressedTokens = 0;
        List<int> largeMessages = [];
        List<int> uncompressedIndices = [];
        List<int> compressedIndices = [];

        for (int i = 0; i < messages.Count; i++)
        {
            ChatMessage msg = messages[i];
            int tokens = EstimateTokens(msg);
            MessageCompressionState state = metadata.GetState(msg.Id);

            if (msg.Role == ChatMessageRoles.System)
            {
                systemTokens += tokens;
                continue;
            }

            if (state is MessageCompressionState.Compressed or MessageCompressionState.ReCompressed)
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
                : (int)(_contextWindowTokens * TargetUtilization),
        };
    }

    internal static int EstimateTokens(ChatMessage message)
    {
        if (message.Tokens is > 0)
            return message.Tokens.Value;

        int charCount = (message.Content?.Length ?? 0)
            + (message.Parts?.Sum(p => p.Text?.Length ?? 0) ?? 0);

        int textTokens = Math.Max(1, charCount / 4);
        
        // Add estimated token costs for media parts
        int mediaTokens = 0;
        if (message.Parts is not null)
        {
            foreach (ChatMessagePart part in message.Parts)
            {
                mediaTokens += part.Type switch
                {
                    ChatMessageTypes.Image => MediaTokenCosts.ImageDefault,
                    ChatMessageTypes.Document => MediaTokenCosts.Document,
                    ChatMessageTypes.Audio => MediaTokenCosts.Audio,
                    _ => 0
                };
            }
        }

        return textTokens + mediaTokens;
    }
}

public sealed class CompressionAnalysis
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

/// <summary>
/// Tracks compression state for messages by their Id.
/// </summary>
public sealed class MessageMetadataTracker
{
    private readonly Dictionary<Guid, MessageCompressionState> _states = new();

    public void Track(ChatMessage message) =>
        _states.TryAdd(message.Id, MessageCompressionState.Uncompressed);

    public void MarkCompressed(Guid id) =>
        _states[id] = MessageCompressionState.Compressed;

    public void MarkReCompressed(Guid id) =>
        _states[id] = MessageCompressionState.ReCompressed;

    public MessageCompressionState GetState(Guid id) =>
        _states.GetValueOrDefault(id, MessageCompressionState.Uncompressed);

    public void Clear() => _states.Clear();
}
