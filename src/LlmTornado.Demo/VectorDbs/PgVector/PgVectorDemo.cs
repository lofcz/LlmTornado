using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.PgVector.Integrations;

namespace LlmTornado.Demo.VectorDbs;

public class PgVectorDemo
{
    [TornadoTest, Flaky("requires specific index")]
    public static async Task PgVector()
    {
        // Initialize PgVector with connection string and vector dimension
        string connectionString = "Host=localhost;Database=vectordb;Username=postgres;Password=";
        TornadoPgVector pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536, metric: SimilarityMetric.DotProduct);

        // Initialize a collection
        await pgVector.InitializeCollection("documents");

        // Create sample documents with embeddings
        VectorDocument[] documents =
        [
            new VectorDocument(
                id: "doc1",
                content: "PostgreSQL is a powerful open-source relational database",
                embedding: VectorDbInternal.GenerateRandomEmbedding(1536),
                metadata: new Dictionary<string, object>
                {
                    { "category", "database" },
                    { "year", 2024 },
                    { "source", "technical" }
                }
            ),
            new VectorDocument(
                id: "doc2",
                content: "Vector databases enable similarity search over embeddings",
                embedding: VectorDbInternal.GenerateRandomEmbedding(1536),
                metadata: new Dictionary<string, object>
                {
                    { "category", "database" },
                    { "year", 2024 },
                    { "source", "technical" }
                }
            ),
            new VectorDocument(
                id: "doc3",
                content: "Machine learning models generate vector embeddings",
                embedding: VectorDbInternal.GenerateRandomEmbedding(1536),
                metadata: new Dictionary<string, object>
                {
                    { "category", "ai" },
                    { "year", 2024 },
                    { "source", "research" }
                }
            )
        ];

        await pgVector.DeleteAllDocumentsAsync();

        // Add documents to the collection
        await pgVector.AddDocumentsAsync(documents);

        // Query by embedding similarity
        float[] queryEmbedding = VectorDbInternal.GenerateRandomEmbedding(1536);
        VectorDocument[] results = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            topK: 2
        );

        Console.WriteLine("Top 2 similar documents:");
        foreach (VectorDocument doc in results)
        {
            Console.WriteLine($"- {doc.Content} (Score: {doc.Score})");
        }

        // Query with metadata filtering
        VectorDocument[] filteredResults = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "database"),
            topK: 2
        );

        Console.WriteLine("\nTop 2 database documents:");
        foreach (VectorDocument doc in filteredResults)
        {
            Console.WriteLine($"- {doc.Content} (Score: {doc.Score})");
        }

        // Update a document
        VectorDocument updatedDoc = new VectorDocument(
            id: "doc1",
            embedding: queryEmbedding,
            content: "PostgreSQL with pgvector extension enables vector similarity search",
            metadata: new Dictionary<string, object>
            {
                { "category", "database" },
                { "year", 2024 },
                { "updated", true }
            }
        );
        await pgVector.UpdateDocumentsAsync([updatedDoc]);

        // Get specific documents by ID
        VectorDocument[] retrievedDocs = await pgVector.GetDocumentsAsync(["doc1", "doc2"]);
        Console.WriteLine($"\nRetrieved {retrievedDocs.Length} documents by ID");

        // Delete a document
        await pgVector.DeleteDocumentsAsync(["doc3"]);
        Console.WriteLine("Deleted doc3");

        // Clean up - delete the collection
        await pgVector.DeleteCollectionAsync("documents");
        Console.WriteLine("Collection deleted");
    }
}