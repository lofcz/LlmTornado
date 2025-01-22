using System.Text.Json.Serialization;

namespace LlmTornado.VectorStores;

/// <summary>
/// Base class for chunking strategies
/// </summary>
public abstract class ChunkingStrategy
{
    /// <summary>
    /// The type of chunking strategy
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
}