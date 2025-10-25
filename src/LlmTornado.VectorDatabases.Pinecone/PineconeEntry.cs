namespace LlmTornado.VectorDatabases.Pinecone;

/// <summary>
/// Represents a vector entry in Pinecone with its ID, embedding, metadata, document content, and optional distance score.
/// </summary>
public class PineconeEntry
{
    /// <summary>
    /// The unique identifier for this vector.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The vector embedding.
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Metadata associated with this vector.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// The document content/text associated with this vector.
    /// Stored in metadata with key "_document".
    /// </summary>
    public string? Document { get; set; }

    /// <summary>
    /// The distance/similarity score from a query (if this entry is a query result).
    /// </summary>
    public float? Distance { get; set; }

    /// <summary>
    /// Creates a new PineconeEntry instance.
    /// </summary>
    /// <param name="id">The vector ID</param>
    /// <param name="document">The document content</param>
    /// <param name="metadata">The metadata dictionary</param>
    /// <param name="embedding">The vector embedding</param>
    /// <param name="distance">The distance score</param>
    public PineconeEntry(
        string id,
        string? document = null,
        Dictionary<string, object>? metadata = null,
        float[]? embedding = null,
        float? distance = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Document = document;
        Metadata = metadata;
        Embedding = embedding;
        Distance = distance;
    }
}

