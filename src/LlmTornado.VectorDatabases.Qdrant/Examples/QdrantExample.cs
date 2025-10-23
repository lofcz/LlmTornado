using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Qdrant;

namespace LlmTornado.VectorDatabases.Qdrant.Examples;

/// <summary>
/// Example usage of the Qdrant implementation for LlmTornado
/// </summary>
public class QdrantExample
{
    public static async Task BasicUsageExample()
    {
        // Initialize Qdrant with connection details and vector dimension
        var qdrantDb = new QdrantVectorDatabase(
            host: "localhost",
            port: 6334,
            vectorDimension: 1536,
            https: false
        );

        // Initialize a collection
        await qdrantDb.InitializeCollectionAsync("documents");

        // Create sample documents with embeddings
        var documents = new[]
        {
            new VectorDocument(
                id: "doc1",
                content: "Qdrant is a high-performance vector database for AI applications",
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
        };

        // Add documents to the collection
        await qdrantDb.AddDocumentsAsync(documents);

        // Query by embedding similarity
        var queryEmbedding = GenerateRandomEmbedding(1536);
        var results = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            topK: 2,
            includeScore: true
        );

        Console.WriteLine("Top 2 similar documents:");
        foreach (var doc in results)
        {
            Console.WriteLine($"- {doc.Content} (Score: {doc.Score})");
        }

        // Query with metadata filtering
        var filteredResults = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "database"),
            topK: 2,
            includeScore: true
        );

        Console.WriteLine("\nTop 2 database documents:");
        foreach (var doc in filteredResults)
        {
            Console.WriteLine($"- {doc.Content} (Score: {doc.Score})");
        }

        // Update a document
        var updatedDoc = new VectorDocument(
            id: "doc1",
            content: "Qdrant is a high-performance vector database optimized for similarity search",
            embedding: GenerateRandomEmbedding(1536),
            metadata: new Dictionary<string, object>
            {
                { "category", "database" },
                { "year", 2024 },
                { "updated", true }
            }
        );
        await qdrantDb.UpdateDocumentsAsync(new[] { updatedDoc });

        // Get specific documents by ID
        var retrievedDocs = await qdrantDb.GetDocumentsAsync(new[] { "doc1", "doc2" });
        Console.WriteLine($"\nRetrieved {retrievedDocs.Length} documents by ID");

        // Upsert documents (insert or update)
        var upsertDoc = new VectorDocument(
            id: "doc4",
            content: "New document to be upserted",
            embedding: GenerateRandomEmbedding(1536),
            metadata: new Dictionary<string, object>
            {
                { "category", "example" },
                { "year", 2024 }
            }
        );
        await qdrantDb.UpsertDocumentsAsync(new[] { upsertDoc });
        Console.WriteLine("Document upserted");

        // Delete a document
        await qdrantDb.DeleteDocumentsAsync(new[] { "doc3" });
        Console.WriteLine("Deleted doc3");

        // Clean up - delete the collection
        await qdrantDb.DeleteCollectionAsync("documents");
        Console.WriteLine("Collection deleted");
    }

    public static async Task AdvancedFilteringExample()
    {
        var qdrantDb = new QdrantVectorDatabase(
            host: "localhost",
            port: 6334,
            vectorDimension: 1536
        );

        await qdrantDb.InitializeCollectionAsync("products");

        // Complex metadata filtering examples
        var queryEmbedding = GenerateRandomEmbedding(1536);

        // Equal filter
        var results1 = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "electronics"),
            topK: 5,
            includeScore: true
        );

        // Greater than filter
        var results2 = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.GreaterThan("price", 100),
            topK: 5,
            includeScore: true
        );

        // In array filter
        var results3 = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.In("brand", "Apple", "Samsung", "Sony"),
            topK: 5,
            includeScore: true
        );

        // Combined filters with AND
        var results4 = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "electronics") 
                 & TornadoWhereOperator.GreaterThan("price", 100),
            topK: 5,
            includeScore: true
        );

        // Combined filters with OR
        var results5 = await qdrantDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: TornadoWhereOperator.Equal("category", "electronics") 
                 | TornadoWhereOperator.Equal("category", "computers"),
            topK: 5,
            includeScore: true
        );

        await qdrantDb.DeleteCollectionAsync("products");
    }

    public static async Task SyncMethodsExample()
    {
        // The library also provides synchronous versions of all async methods
        var qdrantDb = new QdrantVectorDatabase(
            host: "localhost",
            port: 6334,
            vectorDimension: 1536
        );

        await qdrantDb.InitializeCollectionAsync("sync_example");

        var documents = new[]
        {
            new VectorDocument(
                id: "sync1",
                content: "Synchronous example document",
                embedding: GenerateRandomEmbedding(1536)
            )
        };

        // Synchronous add
        qdrantDb.AddDocuments(documents);

        // Synchronous query
        var results = qdrantDb.QueryByEmbedding(
            GenerateRandomEmbedding(1536),
            topK: 5
        );

        // Synchronous get
        var docs = qdrantDb.GetDocuments(new[] { "sync1" });

        // Synchronous delete
        qdrantDb.DeleteDocuments(new[] { "sync1" });

        await qdrantDb.DeleteCollectionAsync("sync_example");
    }

    private static float[] GenerateRandomEmbedding(int dimension)
    {
        var random = new Random();
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)random.NextDouble();
        }
        return embedding;
    }
}
