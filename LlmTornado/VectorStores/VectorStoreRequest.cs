using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LlmTornado.VectorStores;

public class VectorStoreRequest
{
    /// <summary>
    /// A list of File IDs that the vector store should use. Useful for tools like `file_search` that can access files.
    /// </summary>
    [JsonPropertyName("file_ids")]
    public List<string>? FileIds { get; set; }

    /// <summary>
    /// The name of the vector store.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The expiration policy for a vector store.
    /// </summary>
    [JsonPropertyName("expires_after")]
    public ExpirationPolicy? ExpiresAfter { get; set; }

    /// <summary>
    /// The chunking strategy used to chunk the file(s). If not set, will use the `auto` strategy.
    /// Only applicable if `file_ids` is non-empty.
    /// </summary>
    [JsonPropertyName("chunking_strategy")]
    public ChunkingStrategy? ChunkingStrategy { get; set; }

    /// <summary>
    /// Set of key-value pairs that can be attached to an object. 
    /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// Maximum of 16 key-value pairs.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}