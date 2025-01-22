namespace LlmTornado.VectorStores;

/// <summary>
/// Represents the auto chunking strategy with default settings
/// (max_chunk_size_tokens: 800, chunk_overlap_tokens: 400)
/// </summary>
public class AutoChunkingStrategy : ChunkingStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AutoChunkingStrategy"/> class
    /// </summary>
    public AutoChunkingStrategy()
    {
        Type = "auto";
    }
}