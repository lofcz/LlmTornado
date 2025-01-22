using System.Text.Json.Serialization;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents the expiration policy for a vector store
/// </summary>
public class ExpirationPolicy
{
    /// <summary>
    /// Anchor timestamp after which the expiration policy applies.
    /// Supported anchors: `last_active_at`.
    /// </summary>
    [JsonPropertyName("anchor")]
    public string Anchor { get; set; } = null!;

    /// <summary>
    /// The number of days after the anchor time that the vector store will expire.
    /// </summary>
    [JsonPropertyName("days")]
    public int Days { get; set; }
}