namespace LlmTornado.Embedding.Vendors.Google;

/// <summary>
/// Embedding features supported only by Google.
/// </summary>
public class EmbeddingRequestVendorGoogleExtensions
{
    /// <summary>
    /// Type of task for which the embedding will be used.
    /// </summary>
    public EmbeddingRequestVendorGoogleExtensionsTaskTypes? TaskType { get; set; }
    
    /// <summary>
    /// Optional. Optional reduced dimension for the output embedding. If set, excessive values in the output embedding are truncated from the end.
    /// </summary>
    public int? OutputDimensionality { get; set; }
    
    /// <summary>
    /// Optional. An optional title for the text. Only applicable when TaskType is RETRIEVAL_DOCUMENT.
    /// Note: Specifying a title for RETRIEVAL_DOCUMENT provides better quality embeddings for retrieval.
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Type of task for which the embedding will be used.
/// </summary>
public enum EmbeddingRequestVendorGoogleExtensionsTaskTypes
{
    /// <summary>
    /// 	Unset value, which will default to one of the other enum values.
    /// </summary>
    Unspecified,
    
    /// <summary>
    /// 	Specifies the given text is a query in a search/retrieval setting.
    /// </summary>
    RetrievalQuery,
    
    /// <summary>
    ///     Specifies the given text is a document from the corpus being searched.
    /// </summary>
    RetrievalDocument,
    
    /// <summary>
    ///     Specifies the given text will be used for STS.
    /// </summary>
    SemanticSimilarity,
    
    /// <summary>
    ///     	Specifies that the given text will be classified.
    /// </summary>
    Classification,
    
    /// <summary>
    ///     Specifies that the embeddings will be used for clustering.
    /// </summary>
    Clustering,
    
    /// <summary>
    ///     Specifies that the given text will be used for question answering.
    /// </summary>
    QuestionAnswering,
    
    /// <summary>
    ///     Specifies that the given text will be used for fact verification.
    /// </summary>
    FactVerification
}