using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Implements a fixed-size text chunking strategy with configurable overlap between chunks.
/// This strategy splits text into chunks of predetermined size while maintaining overlap
/// between adjacent chunks to preserve context across chunk boundaries.
/// </summary>
sealed class StaticChunkingStrategy : ChunkingStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaticChunkingStrategy"/> class
    /// </summary>
    public StaticChunkingStrategy()
    {
        Type = "static";
    }

    /// <summary>
    /// Configuration for static chunking
    /// </summary>
    [JsonProperty("static")]
    public StaticChunkingConfig Static { get; set; } = null!;
}