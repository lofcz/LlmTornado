namespace LlmTornado.Embedding.Vendors.Google;

/// <summary>
/// Embedding features supported only by Google.
/// </summary>
public class EmbeddingRequestVendorGoogleExtensions
{
    /// <summary>
    /// Type of task for which the embedding will be used with <c>gemini-embedding-001</c> via the <c>taskType</c> API field.
    /// </summary>
    public EmbeddingRequestVendorGoogleExtensionsTaskTypes? TaskType { get; set; }
    
    /// <summary>
    /// Task type for <c>gemini-embedding-2</c> models. Formats text inputs with the required task prefix before embedding.
    /// </summary>
    public EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes? Embedding2Task { get; set; }
    
    /// <summary>
    /// Optional. Optional reduced dimension for the output embedding. If set, excessive values in the output embedding are truncated from the end.
    /// </summary>
    public int? OutputDimensionality { get; set; }
    
    /// <summary>
    /// Optional. An optional title for the text. Only applicable when TaskType is RETRIEVAL_DOCUMENT.
    /// Note: Specifying a title for RETRIEVAL_DOCUMENT provides better quality embeddings for retrieval.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// When <see cref="Embedding2Task"/> is an asymmetric retrieval document task, formats text with
    /// <c>title: {title} | text: {content}</c>. Defaults to <c>title: none</c> when unset.
    /// </summary>
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Formats a query for asymmetric <c>gemini-embedding-2</c> retrieval tasks.
    /// </summary>
    public static string FormatEmbedding2Query(EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes task, string content)
    {
        return task switch
        {
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.SearchResult => $"task: search result | query: {content}",
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.QuestionAnswering => $"task: question answering | query: {content}",
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.FactChecking => $"task: fact checking | query: {content}",
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.CodeRetrieval => $"task: code retrieval | query: {content}",
            _ => content
        };
    }

    /// <summary>
    /// Formats a document for asymmetric <c>gemini-embedding-2</c> retrieval tasks.
    /// </summary>
    public static string FormatEmbedding2Document(string content, string? title = null)
    {
        return $"title: {title ?? "none"} | text: {content}";
    }

    /// <summary>
    /// Formats input for symmetric <c>gemini-embedding-2</c> tasks such as classification or clustering.
    /// </summary>
    public static string FormatEmbedding2Symmetric(EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes task, string content)
    {
        return task switch
        {
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.Classification => $"task: classification | query: {content}",
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.Clustering => $"task: clustering | query: {content}",
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.SemanticSimilarity => $"task: sentence similarity | query: {content}",
            _ => content
        };
    }

    internal string? FormatEmbedding2Text(string content)
    {
        if (Embedding2Task is null)
        {
            return null;
        }

        return Embedding2Task switch
        {
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.RetrievalDocument
                or EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.SearchDocument
                => FormatEmbedding2Document(content, DocumentTitle ?? Title),
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.SearchResult
                or EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.QuestionAnswering
                or EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.FactChecking
                or EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.CodeRetrieval
                => FormatEmbedding2Query(Embedding2Task.Value, content),
            EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.Classification
                or EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.Clustering
                or EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes.SemanticSimilarity
                => FormatEmbedding2Symmetric(Embedding2Task.Value, content),
            _ => null
        };
    }
}

/// <summary>
/// Type of task for which the embedding will be used with <c>gemini-embedding-001</c>.
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

/// <summary>
/// Task types for <c>gemini-embedding-2</c> models. These are applied as text prefixes rather than the <c>taskType</c> API field.
/// </summary>
public enum EmbeddingRequestVendorGoogleExtensionsEmbedding2TaskTypes
{
    /// <summary>
    /// Asymmetric search query: <c>task: search result | query: {content}</c>
    /// </summary>
    SearchResult,

    /// <summary>
    /// Asymmetric retrieval document: <c>title: {title} | text: {content}</c>
    /// </summary>
    SearchDocument,

    /// <summary>
    /// Alias for asymmetric retrieval document formatting.
    /// </summary>
    RetrievalDocument = SearchDocument,

    /// <summary>
    /// Asymmetric question answering query.
    /// </summary>
    QuestionAnswering,

    /// <summary>
    /// Asymmetric fact checking query.
    /// </summary>
    FactChecking,

    /// <summary>
    /// Asymmetric code retrieval query.
    /// </summary>
    CodeRetrieval,

    /// <summary>
    /// Symmetric classification input.
    /// </summary>
    Classification,

    /// <summary>
    /// Symmetric clustering input.
    /// </summary>
    Clustering,

    /// <summary>
    /// Symmetric semantic similarity input. Not intended for search or retrieval.
    /// </summary>
    SemanticSimilarity
}
