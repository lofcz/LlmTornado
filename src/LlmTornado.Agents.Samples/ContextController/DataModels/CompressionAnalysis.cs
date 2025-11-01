using LlmTornado.Chat;

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
