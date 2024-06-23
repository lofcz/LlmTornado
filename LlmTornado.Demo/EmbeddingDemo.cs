using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Demo;

public static class EmbeddingDemo
{
    public static async Task Embed()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen2.Ada, "lorem ipsum");
        float[]? data = result?.Data.FirstOrDefault()?.Embedding;

        if (data is not null)
        {
            for (int i = 0; i < Math.Min(data.Length, 10); i++)
            {
                Console.WriteLine(data[i]);
            }
        }
    }
    
    public static async Task EmbedVector()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen2.Ada, [ "how are you", "how are you doing" ]);
        Console.WriteLine(result?.Data.Count ?? 0);
    }

    public static async Task EmbedCohere()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Cohere.Gen3.Multilingual, "lorem ipsum");
        Console.WriteLine($"Count: {result?.Data.Count ?? 0}, dims: {result?.Data.FirstOrDefault()?.Embedding.Length ?? 0}");
        
        if (result is not null)
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(result.Data[0].Embedding[i]);
            }
            
            Console.WriteLine("...");
        }
    }
}