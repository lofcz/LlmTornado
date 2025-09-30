using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;



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


