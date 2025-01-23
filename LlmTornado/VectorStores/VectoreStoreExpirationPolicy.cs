using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents the expiration policy for a vector store
/// </summary>
public class VectoreStoreExpirationPolicy
{
    /// <summary>
    /// Anchor timestamp after which the expiration policy applies.
    /// Supported anchors: `last_active_at`.
    /// </summary>
    [JsonProperty("anchor")]
    public string Anchor { get; set; } = null!;

    /// <summary>
    /// The number of days after the anchor time that the vector store will expire.
    /// </summary>
    [JsonProperty("days")]
    public int Days { get; set; }
}