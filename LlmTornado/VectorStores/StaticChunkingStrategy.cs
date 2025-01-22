using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents the static chunking strategy with configurable settings
/// </summary>
public class StaticChunkingStrategy : ChunkingStrategy
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