namespace LlmTornado.VectorDatabases.Pinecone;

/// <summary>
/// Cloud providers supported by Pinecone for serverless indexes.
/// </summary>
public enum PineconeCloud
{
    /// <summary>
    /// Amazon Web Services
    /// </summary>
    Aws,
    
    /// <summary>
    /// Google Cloud Platform
    /// </summary>
    Gcp,
    
    /// <summary>
    /// Microsoft Azure
    /// </summary>
    Azure
}

/// <summary>
/// Configuration options for connecting to Pinecone.
/// </summary>
public class PineconeConfigurationOptions
{
    /// <summary>
    /// The Pinecone API key for authentication.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// The name of the Pinecone index to use. If not specified and dimension is provided,
    /// a new index will be created with a generated name.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// The dimension of the vectors to be stored. Required if creating a new index.
    /// </summary>
    public int? Dimension { get; set; }

    /// <summary>
    /// The distance metric to use for similarity calculations.
    /// Defaults to Cosine.
    /// </summary>
    public SimilarityMetric Metric { get; set; } = SimilarityMetric.DotProduct;

    /// <summary>
    /// The cloud provider for serverless indexes. Defaults to AWS.
    /// </summary>
    public PineconeCloud Cloud { get; set; } = PineconeCloud.Aws;

    /// <summary>
    /// The region for serverless indexes. Defaults to "us-east-1".
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Optional embedding model to use for auto-generating embeddings.
    /// If not specified, will attempt to use the model from index's Embed property.
    /// If null and index has no Embed config, embeddings must be provided explicitly.
    /// Examples: "llama-text-embed-v2", "multilingual-e5-large"
    /// </summary>
    public string? EmbeddingModel { get; set; }

    /// <summary>
    /// Optional parameters for embedding generation.
    /// If null, uses sensible defaults (InputType=Passage, Truncate=End).
    /// </summary>
    public PineconeEmbeddingParameters? EmbeddingParameters { get; set; }

    /// <summary>
    /// Creates a new instance of PineconeConfigurationOptions.
    /// </summary>
    /// <param name="apiKey">The Pinecone API key</param>
    /// <param name="indexName">Optional index name. If not provided and dimension is set, an index will be created.</param>
    /// <param name="dimension">Optional dimension for vector embeddings</param>
    /// <param name="metric">Distance metric (default: Cosine)</param>
    /// <param name="cloud">Cloud provider (default: AWS)</param>
    /// <param name="region">Cloud region (default: us-east-1)</param>
    public PineconeConfigurationOptions(
        string apiKey,
        string? indexName = null,
        int? dimension = null,
        SimilarityMetric metric = SimilarityMetric.DotProduct,
        PineconeCloud cloud = PineconeCloud.Aws,
        string region = "us-east-1")
    {
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        IndexName = indexName;
        Dimension = dimension;
        Metric = metric;
        Cloud = cloud;
        Region = region;
    }
}

