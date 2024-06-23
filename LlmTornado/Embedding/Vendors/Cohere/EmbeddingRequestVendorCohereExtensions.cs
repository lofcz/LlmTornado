namespace LlmTornado.Embedding.Vendors.Cohere;

/// <summary>
/// Embedding features supported only by Cohere.
/// </summary>
public class EmbeddingRequestVendorCohereExtensions
{
    /// <summary>
    /// Input type / results usage intent.
    /// </summary>
    public EmbeddingVendorCohereExtensionInputTypes InputType { get; set; } = EmbeddingVendorCohereExtensionInputTypes.SearchDocument;
    
    /// <summary>
    /// Ways to truncate the input when length exceeds supported length.
    /// </summary>
    public EmbeddingVendorCohereExtensionTruncation? Truncate { get; set; }
    
    // [todo] embedding_types
}

/// <summary>
/// Cohere Embedding Gen 3 and newer require the input type / results usage intent to be specified.
/// </summary>
public enum EmbeddingVendorCohereExtensionInputTypes
{
    /// <summary>
    /// Used for embeddings stored in a vector database for search use-cases.
    /// </summary>
    SearchDocument,
    /// <summary>
    /// Used for embeddings of search queries run against a vector DB to find relevant documents.
    /// </summary>
    SearchQuery,
    /// <summary>
    /// Used for embeddings passed through a text classifier.
    /// </summary>
    Classification,
    /// <summary>
    /// Used for the embeddings run through a clustering algorithm.
    /// </summary>
    Clustering
}

/// <summary>
/// Ways to truncate the input when length exceeds supported length.
/// </summary>
public enum EmbeddingVendorCohereExtensionTruncation
{
    /// <summary>
    /// When the input exceeds the maximum input token length, an error will be returned.
    /// </summary>
    None,
    /// <summary>
    /// Input is trimmed from the beginning to fit the context size.
    /// </summary>
    Start,
    /// <summary>
    /// Input is trimmed from the end to fit the context size.
    /// </summary>
    End
}