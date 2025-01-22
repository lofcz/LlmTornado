using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Base class for chunking strategies
/// </summary>
public abstract class ChunkingStrategy
{
    /// <summary>
    /// The type of chunking strategy
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = null!;
}