using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

/// <summary>
/// Vector document with content, metadata, and embedding.
/// Used to represent documents stored in or retrieved from a vector database.
/// </summary>
public class VectorDocument
{
    /// <summary>
    /// ID of the document.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Content stored in the document.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Metadata associated with the document.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
    /// <summary>
    /// Vector embedding representing the document in vector space.
    /// </summary>
    public float[]? Embedding { get; set; }
    /// <summary>
    /// Dimension of the embedding vector.
    /// </summary>
    public int Dimension => Embedding?.Length ?? 0;
    /// <summary>
    /// Queried relevance score for the document.
    /// </summary>
    public float? Score { get; set; } // Optional relevance score for query results
    public VectorDocument(string id, string content, Dictionary<string, object>? metadata = null, float[]? embedding = null, float? score = null)
    {
        Id = id;
        Content = content;
        Metadata = metadata;
        Embedding = embedding;
        Score = score;
    }

    public override string ToString()
    {
        string metadataStr = Metadata != null
            ? "{" + string.Join(", ", Metadata.Select(kv => $"{kv.Key}: {kv.Value}")) + "}"
            : "null";
        return $"VectorDocument(Id={Id},\n Content={Content},\n Metadata={metadataStr},\n Score={Score}\n)";
    }
}

public interface IVectorDatabase
{
    public string GetCollectionName();
    /// <summary>
    /// Add Documents to the vector database.
    /// </summary>
    /// <param name="documents"></param>
    public void AddDocuments(VectorDocument[] documents);
    /// <summary>
    /// Get Documents by ID
    /// </summary>
    /// <param name="ids"></param>
    /// <returns>List of retrieved documents</returns>
    public VectorDocument[]? GetDocuments(string[] ids);
    /// <summary>
    /// Insert or update documents in the vector database.
    /// </summary>
    /// <param name="documents"></param>
    public void UpsertDocuments(VectorDocument[] documents);

    /// <summary>
    /// Update existing documents in the vector database.
    /// </summary>
    /// <param name="documents"></param>
    public void UpdateDocuments(VectorDocument[] documents);
    /// <summary>
    /// Delete documents by ID.
    /// </summary>
    /// <param name="ids"></param>
    public void DeleteDocuments(string[] ids);
    /// <summary>
    /// Adds a collection of vector documents to the system asynchronously.
    /// </summary>
    /// <remarks>This method processes the provided documents and adds them to the system. Ensure that the
    /// documents meet the required format and constraints before calling this method. The operation is performed
    /// asynchronously and may involve I/O or other resource-intensive tasks.</remarks>
    /// <param name="documents">An array of <see cref="VectorDocument"/> objects to be added. Each document must be valid and cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task completes when all documents have been added.</returns>
    public Task AddDocumentsAsync(VectorDocument[] documents);
    /// <summary>
    /// Retrieves an array of documents corresponding to the specified identifiers.
    /// </summary>
    /// <remarks>This method performs an asynchronous operation to retrieve documents. The order of the
    /// returned  documents in the array corresponds to the order of the provided identifiers in <paramref name="ids"/>.
    /// If an identifier does not match any document, no entry will be included for that identifier.</remarks>
    /// <param name="ids">An array of document identifiers. Each identifier must be a non-null, non-empty string.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of  <see
    /// cref="VectorDocument"/> objects corresponding to the provided identifiers. If no documents  are found for the
    /// given identifiers, the array will be empty.</returns>
    public Task<VectorDocument[]> GetDocumentsAsync(string[] ids);
    /// <summary>
    /// Updates the specified collection of vector documents asynchronously.
    /// </summary>
    /// <remarks>Each document in the <paramref name="documents"/> array will be processed and updated. 
    /// Ensure that the array contains valid and non-null <see cref="VectorDocument"/> instances.</remarks>
    /// <param name="documents">An array of <see cref="VectorDocument"/> objects to be updated. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateDocumentsAsync(VectorDocument[] documents);
    /// <summary>
    /// Inserts or updates the specified vector documents in the data store.
    /// </summary>
    /// <remarks>If a document with the same identifier already exists, it will be updated; otherwise, a new
    /// document will be inserted.</remarks>
    /// <param name="documents">An array of <see cref="VectorDocument"/> objects to be inserted or updated. Each document must have a unique
    /// identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task completes when all documents have been successfully
    /// upserted.</returns>
    public Task UpsertDocumentsAsync(VectorDocument[] documents);
    /// <summary>
    /// Deletes the documents with the specified identifiers asynchronously.
    /// </summary>
    /// <param name="ids">An array of document identifiers to delete. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public Task DeleteDocumentsAsync(string[] ids);
    /// <summary>
    /// Deletes all documents in the current collection/namespace.
    /// This operation is irreversible and will remove all vectors but preserve the collection structure.
    /// </summary>
    /// <returns>A task that represents the asynchronous delete all operation.</returns>
    public Task DeleteAllDocumentsAsync();
    /// <summary>
    /// Query the vector database using an embedding vector to find the most similar documents.
    /// </summary>
    /// <param name="embedding">The embedding vector to query against.</param>
    /// <param name="where"> Used to filter the metadata</param>
    /// <param name="topK">How many results to report back</param>
    /// <param name="includeScore">A value indicating whether to include the similarity score in the returned results.</param>
    /// <returns>An array of <see cref="VectorDocument"/> objects representing the most similar documents.</returns>
    public VectorDocument[] QueryByEmbedding(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false);
    /// <summary>
    /// Queries the vector database using the provided embedding and retrieves the most relevant documents.
    /// </summary>
    /// <param name="embedding">The embedding vector to query against the database. Must not be null or empty.</param>
    /// <param name="where">An optional list of metadata filters to refine the query results. If null, no filtering is applied.</param>
    /// <param name="topK">The maximum number of top results to return. Must be greater than zero.</param>
    /// <param name="includeScore">A value indicating whether to include the similarity score in the returned results.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of <see
    /// cref="VectorDocument"/> objects  representing the most relevant documents. The array will be empty if no
    /// matching documents are found.</returns>
    public Task<VectorDocument[]> QueryByEmbeddingAsync(float[] embedding, TornadoWhereOperator? where = null, int topK = 5, bool includeScore = false);
}
