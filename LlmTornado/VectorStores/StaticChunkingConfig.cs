using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Configuration options for static chunking
/// </summary>
public class StaticChunkingConfig
{
    /// <summary>
    /// The maximum number of tokens in each chunk.
    /// Default: 800
    /// Minimum: 100
    /// Maximum: 4096
    /// </summary>
    [JsonProperty("max_chunk_size_tokens")]
    public int MaxChunkSizeTokens { get; set; } = 800;

    /// <summary>
    /// The number of tokens that overlap between chunks.
    /// Default: 400
    /// Must not exceed half of max_chunk_size_tokens.
    /// </summary>
    [JsonProperty("chunk_overlap_tokens")]
    public int ChunkOverlapTokens { get; set; } = 400;
}