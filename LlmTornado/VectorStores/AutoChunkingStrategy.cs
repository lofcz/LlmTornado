namespace LlmTornado.VectorStores;

/// <summary>
/// Represents the auto chunking strategy with default settings
/// (max_chunk_size_tokens: 800, chunk_overlap_tokens: 400)
/// </summary>
public class AutoChunkingStrategy : ChunkingStrategy
{
    public AutoChunkingStrategy()
    {
        Type = "auto";
    }
}