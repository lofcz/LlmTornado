using LlmTornado.VectorDatabases.Faiss.Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases.Faiss.Examples;

/// <summary>
/// Example usage of the FAISS vector database integration.
/// </summary>
public class FaissExample
{
    public static async Task Main()
    {
        // Initialize the FAISS vector database
        var faissDb = new FaissVectorDatabase(
            indexDirectory: "./faiss_example_indexes",
            vectorDimension: 128 // Using smaller dimension for example
        );

        // Initialize a collection
        await faissDb.InitializeCollection("example_collection");

        // Create some sample documents with embeddings
        var documents = new[]
        {
            new VectorDocument(
                id: "doc1",
                content: "The quick brown fox jumps over the lazy dog",
                embedding: GenerateRandomEmbedding(128),
                metadata: new Dictionary<string, object> 
                { 
                    ["category"] = "animals",
                    ["year"] = 2024 
                }
            ),
            new VectorDocument(
                id: "doc2",
                content: "Machine learning is a subset of artificial intelligence",
                embedding: GenerateRandomEmbedding(128),
                metadata: new Dictionary<string, object> 
                { 
                    ["category"] = "technology",
                    ["year"] = 2024 
                }
            ),
            new VectorDocument(
                id: "doc3",
                content: "The cat sat on the mat",
                embedding: GenerateRandomEmbedding(128),
                metadata: new Dictionary<string, object> 
                { 
                    ["category"] = "animals",
                    ["year"] = 2023 
                }
            )
        };

        // Add documents
        Console.WriteLine("Adding documents...");
        await faissDb.AddDocumentsAsync(documents);
        Console.WriteLine($"Added {documents.Length} documents");

        // Get documents by ID
        Console.WriteLine("\nGetting documents by ID...");
        var retrievedDocs = await faissDb.GetDocumentsAsync(new[] { "doc1", "doc2" });
        foreach (var doc in retrievedDocs)
        {
            Console.WriteLine($"  {doc.Id}: {doc.Content}");
        }

        // Query by embedding (using first document's embedding as query)
        Console.WriteLine("\nQuerying by embedding...");
        var queryEmbedding = documents[0].Embedding!;
        var results = await faissDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            topK: 3,
            includeScore: true
        );

        Console.WriteLine("Top 3 results:");
        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Id}: {result.Content} (Distance: {result.Score:F4})");
        }

        // Query with metadata filter
        Console.WriteLine("\nQuerying with metadata filter (category = animals)...");
        var whereFilter = TornadoWhereOperator.Equal("category", "animals");
        var filteredResults = await faissDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: whereFilter,
            topK: 3,
            includeScore: true
        );

        Console.WriteLine("Filtered results:");
        foreach (var result in filteredResults)
        {
            Console.WriteLine($"  {result.Id}: {result.Content} (Distance: {result.Score:F4})");
        }

        // Update a document
        Console.WriteLine("\nUpdating document...");
        var updatedDoc = new VectorDocument(
            id: "doc1",
            content: "The quick brown fox jumps over the lazy dog - UPDATED",
            metadata: new Dictionary<string, object> 
            { 
                ["category"] = "animals",
                ["year"] = 2024,
                ["updated"] = true
            }
        );
        await faissDb.UpdateDocumentsAsync(new[] { updatedDoc });
        
        var updated = await faissDb.GetDocumentsAsync(new[] { "doc1" });
        Console.WriteLine($"Updated: {updated[0].Content}");

        // Delete a document
        Console.WriteLine("\nDeleting document...");
        await faissDb.DeleteDocumentsAsync(new[] { "doc3" });
        
        var remaining = await faissDb.GetDocumentsAsync(new[] { "doc1", "doc2", "doc3" });
        Console.WriteLine($"Remaining documents: {remaining.Length}");

        // Clean up
        Console.WriteLine("\nCleaning up...");
        await faissDb.DeleteCollectionAsync("example_collection");
        
        Console.WriteLine("Example completed successfully!");
    }

    private static float[] GenerateRandomEmbedding(int dimension)
    {
        var random = new Random();
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random values between -1 and 1
        }
        // Normalize the embedding
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] /= (float)magnitude;
        }
        return embedding;
    }
}
