using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
///     A request model to modify a vector store
/// </summary>
public class VectorStoreModifyRequest
{
    /// <summary>
    /// The name of the vector store.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The expiration policy for a vector store.
    /// </summary>
    [JsonProperty("expires_after")]
    public VectoreStoreExpirationPolicy? ExpiresAfter { get; set; }

    /// <summary>
    /// Set of key-value pairs that can be attached to an object. 
    /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// Maximum of 16 key-value pairs.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}