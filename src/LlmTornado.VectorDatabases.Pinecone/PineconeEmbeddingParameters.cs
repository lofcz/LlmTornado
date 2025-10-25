namespace LlmTornado.VectorDatabases.Pinecone;

/// <summary>
/// Type of input text for embedding generation.
/// </summary>
public enum PineconeInputType
{
    /// <summary>
    /// Use for document/passage text that will be stored and searched.
    /// </summary>
    Passage,
    
    /// <summary>
    /// Use for query text that will search against passages.
    /// </summary>
    Query
}

/// <summary>
/// Truncation strategy when input exceeds model's maximum length.
/// </summary>
public enum PineconeTruncate
{
    /// <summary>
    /// Do not truncate - will error if input too long.
    /// </summary>
    None,
    
    /// <summary>
    /// Truncate from the start of the text.
    /// </summary>
    Start,
    
    /// <summary>
    /// Truncate from the end of the text (recommended default).
    /// </summary>
    End
}

/// <summary>
/// Parameters for Pinecone embedding generation via inference API.
/// </summary>
public class PineconeEmbeddingParameters
{
    /// <summary>
    /// Type of input text. Use Passage for documents, Query for search queries.
    /// Default: Passage
    /// </summary>
    public PineconeInputType InputType { get; set; } = PineconeInputType.Passage;
    
    /// <summary>
    /// Truncation strategy when input exceeds model's max length.
    /// Default: End
    /// </summary>
    public PineconeTruncate Truncate { get; set; } = PineconeTruncate.End;
    
    /// <summary>
    /// Converts to dictionary for API request.
    /// Always includes all parameters as they may be required by the model.
    /// </summary>
    internal Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>
        {
            ["input_type"] = InputType.ToString().ToLowerInvariant(),
            ["truncate"] = Truncate.ToString().ToUpperInvariant()
        };
    }
}

