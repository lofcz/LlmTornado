using LlmTornado.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases.ChromaDB;
using LlmTornado.VectorDatabases.ChromaDB.Client;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models;


namespace LlmTornado.VectorDatabases.Intergrations
{
    public class TornadoChromaDB : IVectorDatabase
    {
        public ChromaClient ChromaClient { get; set; }
        public ChromaCollection ChromaCollection { get; set; }

        public ChromaCollectionClient CollectionClient { get; set; }
        public string CollectionName { get; set; } = "testCollection";

        private HttpClient _httpClient { get; set; }

        private ChromaConfigurationOptions _configOptions { get; set; }

        public TornadoChromaDB(string uri)
        {
            var handler = new ApiV1ToV2DelegatingHandler
            {
                InnerHandler = new HttpClientHandler()
            };

            _configOptions = new ChromaConfigurationOptions(uri: uri);
            _httpClient = new HttpClient(handler);
            ChromaClient = new ChromaClient(_configOptions, _httpClient);
        }

        public async Task InitializeCollection(string collectionName)
        {
            if (collectionName.Equals(CollectionName))
            {
                return;
            }
            CollectionName = collectionName;
            ChromaCollection = await ChromaClient.GetOrCreateCollection(collectionName);
            CollectionClient = new ChromaCollectionClient(ChromaCollection, _configOptions, _httpClient);
        }

        public void AddDocuments(VectorDocument[] documents)
        {
            Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
        }

        public async Task AddDocumentsAsync(VectorDocument[] documents)
        {
            ThrowIfCollectionNotInitialized();
            List<string> ids = new List<string>();
            List<ReadOnlyMemory<float>> embeddings = new List<ReadOnlyMemory<float>>();
            List<Dictionary<string, object>> metadatas = new List<Dictionary<string, object>>();
            List<string> contents = new List<string>();

            foreach (var doc in documents)
            {
                ids.Add(doc.Id);
                embeddings.Add(new(doc.Embedding ?? Array.Empty<float>()));
                metadatas.Add(doc.Metadata ?? new Dictionary<string, object>());
                contents.Add(doc.Content ?? "");
            }

            await CollectionClient.Add(ids, embeddings: embeddings, metadatas: metadatas, documents: contents);
        }

        public void DeleteCollection(string collectionName)
        {
            Task.Run(async () => await DeleteCollectionAsync(collectionName)).Wait();
        }

        public async Task DeleteCollectionAsync(string collectionName)
        {
            await ChromaClient.DeleteCollection(collectionName);
            if(CollectionName == collectionName)
            {
                CollectionClient = null;
                ChromaCollection = null;
            }
        }
        private void ThrowIfCollectionNotInitialized()
        {
            if(CollectionClient == null)
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
            await CollectionClient.Delete(ids.ToList());
        }

        public VectorDocument[]? GetDocuments(string[] ids)
        {
            return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
        }

        public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
        {
            ThrowIfCollectionNotInitialized();
            var docs = await CollectionClient.Get(ids.ToList(), include: ChromaGetInclude.Documents | ChromaGetInclude.Metadatas | ChromaGetInclude.Embeddings );
            return docs.Select(d => new VectorDocument(d.Id, d.Document ?? "", d.Metadata, d.Embeddings?.ToArray() ?? Array.Empty<float>())).ToArray();
        }

        public VectorDocument[] QueryByEmbedding(float[] embedding, List<MetadataFilter>? filters = null, int topK = 5, bool includeScore = false)
        {
            return Task.Run(async () => await QueryByEmbeddingAsync(embedding, filters , topK, includeScore)).Result;
        }

        public async Task<VectorDocument[]> QueryByEmbeddingAsync(float[] embedding, List<MetadataFilter>? filters = null, int topK = 5, bool includeScore = false)
        {
            ThrowIfCollectionNotInitialized();
            List<VectorDocument> results = new List<VectorDocument>();
            var queryData = await CollectionClient.Query([new(embedding)], include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances | ChromaQueryInclude.Documents);

            foreach (var item in queryData)
            {
                foreach (var entry in item)
                {
                    float[]? _embedding = entry.Embeddings.HasValue ? entry.Embeddings.Value.ToArray() : Array.Empty<float>();
                    results.Add(new VectorDocument(entry.Id, entry.Document ?? "", entry.Metadata, _embedding, entry.Distance));
                }
            }

            return results.ToArray();
        }

        public VectorDocument[] QueryByText(string text, List<MetadataFilter>? filters = null, int topK = 5, bool includeScore = false)
        {
            throw new NotImplementedException();
        }

        public Task<VectorDocument[]> QueryByTextAsync(string text, List<MetadataFilter>? filters = null, int topK = 5, bool includeScore = false)
        {
            ThrowIfCollectionNotInitialized();
            throw new NotImplementedException();
        }

        public void UpdateDocuments(VectorDocument[] documents)
        {
            Task.Run(async () => await UpdateDocumentsAsync(documents)).Wait();
        }

        public async Task UpdateDocumentsAsync(VectorDocument[] documents)
        {
            ThrowIfCollectionNotInitialized();
            await CollectionClient.Update(
                documents.Select(d => d.Id).ToList(),
                embeddings: documents.Select(d => new ReadOnlyMemory<float>(d.Embedding ?? Array.Empty<float>())).ToList(),
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
            await CollectionClient.Upsert(
                documents.Select(d => d.Id).ToList(),
                embeddings: documents.Select(d => new ReadOnlyMemory<float>(d.Embedding ?? Array.Empty<float>())).ToList(),
                metadatas: documents.Select(d => d.Metadata ?? new Dictionary<string, object>()).ToList(),
                documents: documents.Select(d => d.Content ?? "").ToList()
                );
        }
    }
}
