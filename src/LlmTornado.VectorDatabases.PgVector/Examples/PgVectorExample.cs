using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.PgVector.Integrations;

namespace LlmTornado.VectorDatabases.PgVector.Examples;

/// <summary>
/// Example usage of the PgVector implementation for LlmTornado
/// </summary>
public class PgVectorExample
{
    public static async Task BasicUsageExample()
    {
        // Initialize PgVector with connection string and vector dimension
        string connectionString = "Host=localhost;Database=vectordb;Username=postgres;Password=password";
        TornadoPgVector pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536);

        // Initialize a collection
        await pgVector.InitializeCollection("documents");

        // Create sample documents with embeddings
        VectorDocument[] documents =
        [
            new VectorDocument(
                id: "doc1",
                content: "PostgreSQL is a powerful open-source relational database",
                embedding: GenerateRandomEmbedding(1536),
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
                embedding: GenerateRandomEmbedding(1536),
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
                embedding: GenerateRandomEmbedding(1536),
                metadata: new Dictionary<string, object>
                {
                    { "category", "ai" },
                    { "year", 2024 },
                    { "source", "research" }
                }
            )
        ];

        // Add documents to the collection
        await pgVector.AddDocumentsAsync(documents);

        // Query by embedding similarity
        float[] queryEmbedding = GenerateRandomEmbedding(1536);
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

    public static async Task AdvancedFilteringExample()
    {
        string connectionString = "Host=localhost;Database=vectordb;Username=postgres;Password=password";
        TornadoPgVector pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536);

        await pgVector.InitializeCollection("products");

        // Complex metadata filtering examples
        float[] queryEmbedding = GenerateRandomEmbedding(1536);

        // Equal filter
        VectorDocument[] results1 = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "electronics"),
            topK: 5
        );

        // Greater than filter
        VectorDocument[] results2 = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.GreaterThan("price", 100),
            topK: 5
        );

        // In array filter
        VectorDocument[] results3 = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.In("brand", "Apple", "Samsung", "Sony"),
            topK: 5
        );

        // Combined filters with AND
        VectorDocument[] results4 = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "electronics") 
                 & TornadoWhereOperator.GreaterThan("price", 100),
            topK: 5
        );

        // Combined filters with OR
        VectorDocument[] results5 = await pgVector.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "electronics") 
                 | TornadoWhereOperator.Equal("category", "computers"),
            topK: 5
        );

        await pgVector.DeleteCollectionAsync("products");
    }

    private static float[] GenerateRandomEmbedding(int dimension)
    {
        Random random = new Random();
        float[] embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)random.NextDouble();
        }
        return embedding;
    }
}
