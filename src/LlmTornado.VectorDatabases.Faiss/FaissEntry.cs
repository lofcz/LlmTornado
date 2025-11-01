namespace LlmTornado.VectorDatabases.Faiss;

/// <summary>
/// Represents a single entry in the FAISS index with its metadata.
/// </summary>
public class FaissEntry
{
    /// <summary>
    /// Unique identifier for the entry.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Document content.
    /// </summary>
    public string? Document { get; set; }
    
    /// <summary>
    /// Metadata associated with the document.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
    
    /// <summary>
    /// Vector embedding of the document.
    /// </summary>
    public float[]? Embedding { get; set; }
    
    /// <summary>
    /// Distance/similarity score (used in query results).
    /// </summary>
    public float? Distance { get; set; }

    public FaissEntry(string id, string? document = null, Dictionary<string, object>? metadata = null, float[]? embedding = null, float? distance = null)
    {
        Id = id;
        Document = document;
        Metadata = metadata;
        Embedding = embedding;
        Distance = distance;
    }
}
