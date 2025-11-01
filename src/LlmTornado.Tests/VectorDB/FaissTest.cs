using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Faiss.Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Tests.VectorDB;

internal class FaissTest
{
    [Test]
    public async Task FaissDBTest()
    {
        // Initialize the FAISS vector database
        var faissDb = new FaissVectorDatabase(
            indexDirectory: "./faiss_example_indexes",
            vectorDimension: 1536 // Using smaller dimension for example
        );

        // Initialize a collection
        await faissDb.InitializeCollection("example_collection");

        TornadoApi api = new TornadoApi(Code.LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        // Create some sample documents with embeddings
        var documents = new[]
        {
            new VectorDocument(
                id: "doc1",
                content: "The quick brown fox jumps over the lazy dog",
                embedding: (await api.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, "The quick brown fox jumps over the lazy dog")).Data.FirstOrDefault().Embedding,
                metadata: new Dictionary<string, object>
                {
                    ["category"] = "animals",
                    ["year"] = 2024
                }
            ),
            new VectorDocument(
                id: "doc2",
                content: "Machine learning is a subset of artificial intelligence",
                embedding: (await api.Embeddings.CreateEmbedding(Embedding.Models.OpenAi.EmbeddingModelOpenAiGen3.ModelSmall, "Machine learning is a subset of artificial intelligence")).Data.FirstOrDefault().Embedding,
                metadata: new Dictionary<string, object>
                {
                    ["category"] = "technology",
                    ["year"] = 2024
                }
            ),
            new VectorDocument(
                id: "doc3",
                content: "The cat sat on the mat",
                embedding:  (await api.Embeddings.CreateEmbedding(Embedding.Models.OpenAi.EmbeddingModelOpenAiGen3.ModelSmall, "The cat sat on the mat")).Data.FirstOrDefault().Embedding,
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
}
