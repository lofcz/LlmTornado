using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

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
