using Pinecone;

namespace LlmTornado.VectorDatabases.Pinecone.Integrations;

/// <summary>
/// Pinecone implementation of the IVectorDatabase interface for LlmTornado.
/// Uses Pinecone namespaces to represent collections within a single index.
/// </summary>
public class TornadoPinecone : IVectorDatabase
{
    public PineconeVectorClient PineconeClient { get; set; }
    public PineconeCollection? PineconeCollection { get; set; }
    public PineconeCollectionClient? CollectionClient { get; set; }
    public string CollectionName { get; set; } = "defaultCollection";

    private PineconeConfigurationOptions configOptions { get; set; }
    private string indexName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new TornadoPinecone instance.
    /// </summary>
    /// <param name="apiKey">Pinecone API key</param>
    /// <param name="indexName">Optional: name of existing index</param>
    /// <param name="dimension">Optional: dimension for auto-creating index</param>
    /// <param name="metric">Distance metric (default: dot)</param>
    /// <param name="cloud">Cloud provider (default: aws)</param>
    /// <param name="region">Region (default: us-east-1)</param>
    public TornadoPinecone(
        string apiKey,
        string? indexName = null,
        int? dimension = null,
        SimilarityMetric metric = SimilarityMetric.DotProduct,
        PineconeCloud cloud = PineconeCloud.Aws,
        string region = "us-east-1")
    {
        configOptions = new PineconeConfigurationOptions(
            apiKey: apiKey,
            indexName: indexName,
            dimension: dimension,
            metric: metric,
            cloud: cloud,
            region: region
        );

        PineconeClient = new PineconeVectorClient(configOptions);
        Task.Run(async () => await TestPineconeConnection()).Wait();
    }

    /// <summary>
    /// Creates a new TornadoPinecone instance with configuration options.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public TornadoPinecone(PineconeConfigurationOptions options)
    {
        configOptions = options ?? throw new ArgumentNullException(nameof(options));
        PineconeClient = new PineconeVectorClient(configOptions);
        Task.Run(async () => await TestPineconeConnection()).Wait();
    }

    private async Task TestPineconeConnection()
    {
        try
        {
            // Test connection by getting or creating the index
            indexName = await PineconeClient.GetOrCreateIndexAsync();
            
            // Initialize default namespace (empty string) by default
            await InitializeCollection("");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Pinecone instance not reachable. Check your API key and configuration.", ex);
        }
    }

    /// <summary>
    /// Initializes a collection (namespace) within the Pinecone index.
    /// Pinecone uses empty string "" as the default namespace.
    /// </summary>
    /// <param name="collectionName">The namespace name (use "" for default namespace)</param>
    public async Task InitializeCollection(string collectionName = "")
    {
        if (collectionName.Equals(CollectionName) && CollectionClient != null)
        {
            return;
        }

        CollectionName = collectionName;
        
        // Get index dimension
        int dimension = await PineconeClient.GetIndexDimensionAsync(indexName);

        PineconeCollection = new PineconeCollection(collectionName, indexName, dimension);
        PineconeClient client = PineconeClient.GetPineconeClient();
        CollectionClient = new PineconeCollectionClient(
            PineconeCollection, 
            client, 
            PineconeClient, 
            configOptions);
    }

    /// <summary>
    /// Deletes an index.
    /// </summary>
    public void DeleteIndex(string indexName)
    {
        Task.Run(async () => await DeleteIndexAsync(indexName)).Wait();
    }

    /// <summary>
    /// Deletes an index asynchronously.
    /// </summary>
    public async Task DeleteIndexAsync(string indexName)
    {
        await PineconeClient.DeleteIndexAsync(indexName);
        if (this.indexName == indexName)
        {
            CollectionClient = null;
            PineconeCollection = null;
        }
    }

    private void ThrowIfCollectionNotInitialized()
    {
        if (CollectionClient == null)
        {
            throw new InvalidOperationException(
                "CollectionClient is not initialized. Please initialize the collection first.");
        }
    }

    public void AddDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
    }

    public async Task AddDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();
        
        List<string> ids = [];
        List<float[]> embeddings = [];
        List<Dictionary<string, object>> metadatas = [];
        List<string> contents = [];

        foreach (VectorDocument doc in documents)
        {
            ids.Add(doc.Id);
            embeddings.Add(doc.Embedding ?? []);
            metadatas.Add(doc.Metadata ?? new Dictionary<string, object>());
            contents.Add(doc.Content ?? "");
        }

        await CollectionClient!.AddAsync(ids, embeddings, metadatas, contents);
    }

    public VectorDocument[]? GetDocuments(string[] ids)
    {
        return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
    }

    public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();
        
        List<PineconeEntry> entries = await CollectionClient!.GetAsync(ids);
        return entries.Select(e => new VectorDocument(
            e.Id,
            e.Document ?? "",
            e.Metadata,
            e.Embedding ?? []
        )).ToArray();
    }

    public void UpdateDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpdateDocumentsAsync(documents)).Wait();
    }

    public async Task UpdateDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();
        
        await CollectionClient!.UpdateAsync(
            documents.Select(d => d.Id).ToList(),
            embeddings: documents.Select(d => d.Embedding ?? []).ToList(),
            metadatas: documents.Select(d => d.Metadata ?? new Dictionary<string, object>()).ToList(),
            documents: documents.Select(d => d.Content ?? "").ToList()
        );
    }

    public void UpsertDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpsertDocumentsAsync(documents)).Wait();
    }

    public async Task UpsertDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();
        
        await CollectionClient!.UpsertAsync(
            documents.Select(d => d.Id).ToList(),
            embeddings: documents.Select(d => d.Embedding ?? []).ToList(),
            metadatas: documents.Select(d => d.Metadata ?? new Dictionary<string, object>()).ToList(),
            documents: documents.Select(d => d.Content ?? "").ToList()
        );
    }

    public void DeleteDocuments(string[] ids)
    {
        Task.Run(async () => await DeleteDocumentsAsync(ids)).Wait();
    }

    public async Task DeleteDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();
        await CollectionClient!.DeleteAsync(ids.ToList());
    }

    public VectorDocument[] QueryByEmbedding(
        float[] embedding,
        TornadoWhereOperator? where = null,
        int topK = 5,
        bool includeScore = true)
    {
        return Task.Run(async () => await QueryByEmbeddingAsync(embedding, where, topK, includeScore)).Result;
    }

    public async Task<VectorDocument[]> QueryByEmbeddingAsync(
        float[] embedding,
        TornadoWhereOperator? where = null,
        int topK = 5,
        bool includeScore = true)
    {
        ThrowIfCollectionNotInitialized();
        
        Dictionary<string, object>? whereDict = null;
        if (where != null)
        {
            TornadoPineconeWhere tornadoPineconeWhere = new TornadoPineconeWhere(where);
            whereDict = tornadoPineconeWhere.ToWhere();
        }

        List<PineconeEntry> entries = await CollectionClient!.QueryAsync(embedding, topK, whereDict);

        List<VectorDocument> results = [];
        foreach (PineconeEntry? entry in entries)
        {
            float[]? entryEmbedding = entry.Embedding ?? [];
            results.Add(new VectorDocument(
                entry.Id,
                entry.Document ?? "",
                entry.Metadata,
                entryEmbedding,
                entry.Distance
            ));
        }

        return includeScore ? results.OrderByDescending(x => x.Score).ToArray() : results.ToArray();
    }

    /// <summary>
    /// Deletes all documents in the current collection/namespace.
    /// </summary>
    public async Task DeleteAllDocumentsAsync()
    {
        ThrowIfCollectionNotInitialized();
        await CollectionClient!.DeleteAllAsync();
    }

    /// <summary>
    /// Generates an embedding for a single text using Pinecone's inference API.
    /// Useful for embedding search queries or additional content.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="inputType">The type of input (Passage for documents, Query for search queries). Default: Query.</param>
    /// <returns>The embedding vector for the input text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no embedding model is configured.</exception>
    public async Task<float[]> EmbedAsync(string text, PineconeInputType inputType = PineconeInputType.Query)
    {
        List<float[]> embeddings = await EmbedAsync(new List<string> { text }, inputType);
        return embeddings[0];
    }

    /// <summary>
    /// Generates embeddings for multiple texts using Pinecone's inference API.
    /// Useful for batch embedding of search queries or additional content.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="inputType">The type of input (Passage for documents, Query for search queries). Default: Query.</param>
    /// <returns>A list of embedding vectors corresponding to the input texts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no embedding model is configured.</exception>
    public async Task<List<float[]>> EmbedAsync(List<string> texts, PineconeInputType inputType = PineconeInputType.Query)
    {
        string? embeddingModel = await PineconeClient.GetEmbeddingModelAsync();
        if (embeddingModel == null)
        {
            throw new InvalidOperationException(
                "No embedding model configured. Set EmbeddingModel in PineconeConfigurationOptions or ensure your index has integrated embedding enabled.");
        }

        // Use custom parameters if provided, otherwise create with specified input type
        PineconeEmbeddingParameters parameters = configOptions.EmbeddingParameters ?? new PineconeEmbeddingParameters
        {
            InputType = inputType,
            Truncate = PineconeTruncate.End
        };

        EmbedRequest embedRequest = new EmbedRequest
        {
            Model = embeddingModel,
            Inputs = texts.Select(text => new EmbedRequestInputsItem { Text = text }).ToList(),
            Parameters = parameters.ToDictionary()
        };

        PineconeClient client = PineconeClient.GetClient();
        EmbeddingsList response = await client.Inference.EmbedAsync(embedRequest);

        // Convert response to List<float[]>
        return response.Data.Select(embedding =>
        {
            if (embedding.IsDense)
            {
                DenseEmbedding dense = embedding.AsDense();
                return dense.Values.ToArray();
            }
            else
            {
                throw new NotSupportedException("Sparse embeddings are not supported. Use dense embeddings.");
            }
        }).ToList();
    }

    public string GetCollectionName() => CollectionName;
}

