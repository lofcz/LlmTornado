using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases
{
    public class Document
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public Document(string id, string content, Dictionary<string, object> metadata = null)
        {
            Id = id;
            Content = content;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public static implicit operator VectorDocument(Document doc)
        {
            return new VectorDocument(doc.Id, doc.Content, doc.Metadata, null);
        }

        public static implicit operator Document(VectorDocument vdoc)
        {
            return new Document(vdoc.Id, vdoc.Content, vdoc.Metadata);
        }
    }

    public interface IDocumentStore
    {
        void SetDocument(Document document);
        Task SetDocumentAsync(Document document);
        IEnumerable<Document> GetDocuments(string[] id);

        Task<IEnumerable<Document>> GetDocumentsAsync(string[] id);

        void UpdateDocument(Document document);
        Task UpdateDocumentAsync(Document document);

        void DeleteDocument(string id);
        Task DeleteDocumentAsync(string id);
        IEnumerable<Document> GetAllDocuments();
        Task<IEnumerable<Document>> GetAllDocumentsAsync();
        string GetCollectionName();
    }

    public class MemoryDocumentStore : IDocumentStore
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<string, Document>> _collections = new ConcurrentDictionary<string, ConcurrentDictionary<string, Document>>();
        private ConcurrentDictionary<string, Document> _currentCollection => _collections[_collectionName];

        private string _collectionName = "default_collection";

        public string GetCollectionName() => _collectionName;

        public MemoryDocumentStore(string collectionName)
        {
            _collectionName = collectionName;
            if (!_collections.ContainsKey(collectionName))
            {
                _collections[collectionName] = new ConcurrentDictionary<string, Document>();
            }
        }

        public ValueTask SetOrCreateCollection(string collectionName)
        {
            _collectionName = collectionName;
            if (!_collections.TryGetValue(collectionName, out var col))
            {
                _collections[collectionName] = new ConcurrentDictionary<string, Document>();
            }
            return Threading.ValueTaskCompleted;
        }

        public void DeleteDocument(string id)
        {
            if (_currentCollection.TryRemove(id, out _))
            {
                return;
            }

            throw new KeyNotFoundException($"Document with id {id} not found.");
        }

        public IEnumerable<Document> GetAllDocuments()
        {
            return _currentCollection.Values;
        }

        public IEnumerable<Document> GetDocuments(string[] id)
        {
            var documents = new List<Document>();
            foreach (var docId in id)
            {
                if (_currentCollection.TryGetValue(docId, out var doc))
                {
                    documents.Add(doc);
                }
                else
                    throw new KeyNotFoundException($"Document with id {docId} not found.");
            }
            return documents;
        }

        public void SetDocument(Document document)
        {
            _currentCollection.AddOrUpdate(document.Id, document, (key, oldValue) => document);
        }

        public void UpdateDocument(Document document)
        {
            if (!_currentCollection.TryUpdate(document.Id, document, _currentCollection[document.Id]))
            {
                throw new KeyNotFoundException($"Document with id {document.Id} not found or currently in use");
            }
        }

        public async Task SetDocumentAsync(Document document)
        {
            await Task.Run(() => SetDocument(document));
        }

        public async Task<IEnumerable<Document>> GetDocumentsAsync(string[] id)
        {
            return await Task.Run(() => GetDocuments(id));
        }

        public async Task UpdateDocumentAsync(Document document)
        {
            await Task.Run(() => UpdateDocument(document));
        }

        public async Task DeleteDocumentAsync(string id)
        {
            await Task.Run(() => DeleteDocument(id));
        }

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            return await Task.Run(() => GetAllDocuments());
        }
    }

    public class LocalDocumentStore : IDocumentStore
    {
        public string StorePath;

        public string CollectionName = "default_collection";
        private string _collectionFilePath => System.IO.Path.Combine(StorePath, CollectionName);

        public LocalDocumentStore(string storePath, string collectionName)
        {
            StorePath = storePath;
            SetCollection(collectionName);
        }

        public void SetCollection(string collectionName)
        {
            CollectionName = collectionName;
            if (!Directory.Exists(_collectionFilePath))
            {
                Directory.CreateDirectory(_collectionFilePath);
            }
        }

        public void DeleteDocument(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Document> GetAllDocuments()
        {
            List<Document> documents = new List<Document>();
            if(Directory.Exists(_collectionFilePath))
            {
                var files = Directory.GetFiles(_collectionFilePath, "*.json");
                foreach(var file in files)
                {
                    var json = File.ReadAllText(file);
                    var doc = JsonSerializer.Deserialize<Document>(json);
                    documents.Add(doc);
                }
            }
            return documents;
        }

        public IEnumerable<Document> GetDocuments(string[] id)
        {
            List<Document> documents = new List<Document>();
            foreach(var docId in id)
            {
                documents.Add(GetDocument(docId));
            }
            return documents;
        }

        public Document GetDocument(string id)
        {
            var filePath = Path.Combine(_collectionFilePath, $"{id}.json");
            if(File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Document>(json);
            }
            throw new KeyNotFoundException($"Document with id {id} not found.");
        }

        public void SetDocument(Document document)
        {
            string json = JsonSerializer.Serialize(document);
            File.WriteAllText(Path.Combine(_collectionFilePath, $"{document.Id}.json"), json);
        }

        public void UpdateDocument(Document document)
        {
            SetDocument(document);
        }


        public Task SetDocumentAsync(Document document)
        {
            SetDocument(document); return Task.CompletedTask;
        }

        public Task<IEnumerable<Document>> GetDocumentsAsync(string[] id)
        {
            return Task.FromResult(GetDocuments(id));
        }

        public Task UpdateDocumentAsync(Document document)
        {
            return Task.Run(() => UpdateDocument(document));
        }

        public Task DeleteDocumentAsync(string id)
        {
            return Task.Run(() => DeleteDocument(id));
        }

        public Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            return Task.FromResult(GetAllDocuments());
        }

        public string GetCollectionName() => CollectionName;
    }
}
