namespace LlmTornado.VectorDatabases.PgVector.Integrations;

public class TornadoPgVector : IVectorDatabase
{
    public PgVectorClient PgVectorClient { get; set; }
    public PgVectorCollection? PgVectorCollection { get; set; }
    public PgVectorCollectionClient? CollectionClient { get; set; }
    public string CollectionName { get; set; } = "defaultCollection";

    private PgVectorConfigurationOptions _configOptions { get; set; }
    private int _vectorDimension { get; set; }

    public TornadoPgVector(string connectionString, int vectorDimension = 1536, string? schema = null)
    {
        _vectorDimension = vectorDimension;
        _configOptions = new PgVectorConfigurationOptions(connectionString, schema);
        PgVectorClient = new PgVectorClient(_configOptions);
        Task.Run(async () => await TestPgVectorConnection()).Wait();
    }

    private async Task TestPgVectorConnection()
    {
        try
        {
            await PgVectorClient.InitializeAsync();
            
            string testCollectionName = $"test_collection_{Guid.NewGuid().ToString().Substring(0, 4)}";
            await InitializeCollection(testCollectionName);
            await DeleteCollectionAsync(testCollectionName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PgVector instance not reachable or vector extension not available", ex);
        }
    }

    public async Task InitializeCollection(string collectionName)
    {
        if (collectionName.Equals(CollectionName) && CollectionClient != null)
        {
            return;
        }
        
        CollectionName = collectionName;
        PgVectorCollection = await PgVectorClient.GetOrCreateCollectionAsync(collectionName, _vectorDimension);
        CollectionClient = new PgVectorCollectionClient(PgVectorCollection, PgVectorClient);
    }

    public void AddDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
    }

    public async Task AddDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();
        
        List<string> ids = new List<string>();
        List<float[]> embeddings = new List<float[]>();
        List<Dictionary<string, object>> metadatas = new List<Dictionary<string, object>>();
        List<string> contents = new List<string>();

        foreach (var doc in documents)
        {
            ids.Add(doc.Id);
            embeddings.Add(doc.Embedding ?? Array.Empty<float>());
            metadatas.Add(doc.Metadata ?? new Dictionary<string, object>());
            contents.Add(doc.Content ?? "");
        }

        await CollectionClient!.AddAsync(ids, embeddings: embeddings, metadatas: metadatas, documents: contents);
    }

    public void DeleteCollection(string collectionName)
    {
        Task.Run(async () => await DeleteCollectionAsync(collectionName)).Wait();
    }

    public async Task DeleteCollectionAsync(string collectionName)
    {
        await PgVectorClient.DeleteCollectionAsync(collectionName);
        if (CollectionName == collectionName)
        {
            CollectionClient = null;
            PgVectorCollection = null;
        }
    }

    private void ThrowIfCollectionNotInitialized()
    {
        if (CollectionClient == null)
        {
            throw new InvalidOperationException("CollectionClient is not initialized. Please initialize the collection first.");
        }
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

    public VectorDocument[]? GetDocuments(string[] ids)
    {
        return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
    }

    public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();
        var entries = await CollectionClient!.GetAsync(ids);
        return entries.Select(e => new VectorDocument(
            e.Id, 
            e.Document ?? "", 
            e.Metadata, 
            e.Embedding ?? Array.Empty<float>()
        )).ToArray();
    }

    public VectorDocument[] QueryByEmbedding(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        return Task.Run(async () => await QueryByEmbeddingAsync(embedding, where, topK, includeScore)).Result;
    }

    public async Task<VectorDocument[]> QueryByEmbeddingAsync(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false)
    {
        ThrowIfCollectionNotInitialized();
        
        Dictionary<string, object>? whereDict = null;
        if (where != null)
        {
            var tornadoPgWhere = new TornadoPgVectorWhere(where);
            whereDict = tornadoPgWhere.ToWhere();
        }

        var entries = await CollectionClient!.QueryAsync(embedding, topK, whereDict);

        List<VectorDocument> results = new List<VectorDocument>();
        foreach (var entry in entries)
        {
            float[]? entryEmbedding = entry.Embedding ?? Array.Empty<float>();
            results.Add(new VectorDocument(
                entry.Id, 
                entry.Document ?? "", 
                entry.Metadata, 
                entryEmbedding, 
                entry.Distance
            ));
        }

        return results.ToArray();
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
            embeddings: documents.Select(d => d.Embedding ?? Array.Empty<float>()).ToList(),
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
            embeddings: documents.Select(d => d.Embedding ?? Array.Empty<float>()).ToList(),
            metadatas: documents.Select(d => d.Metadata ?? new Dictionary<string, object>()).ToList(),
            documents: documents.Select(d => d.Content ?? "").ToList()
        );
    }

    public async Task DeleteAllDocumentsAsync()
    {
        ThrowIfCollectionNotInitialized();
        await CollectionClient!.DeleteAllAsync();
    }

    public string GetCollectionName() => CollectionName;
}
