using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.PgVector.Integrations;

namespace LlmTornado.VectorDatabases.PgVector.Examples;

/// <summary>
/// Example usage of the PgVector implementation for LlmTornado
/// </summary>
public class PgVectorExample
{
    /*public static async Task AdvancedFilteringExample()
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
    }*/
}
