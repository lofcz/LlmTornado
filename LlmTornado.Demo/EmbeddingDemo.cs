using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Embedding.Vendors.Cohere;

namespace LlmTornado.Demo;

public static class EmbeddingDemo
{
    [TornadoTest]
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
    
    [TornadoTest]
    public static async Task EmbedVector()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen2.Ada, [ "how are you", "how are you doing" ]);
        Console.WriteLine(result?.Data.Count ?? 0);
    }

    [TornadoTest]
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
    
    [TornadoTest]
    public static async Task EmbedCohereExtensions()
    {
        foreach (EmbeddingVendorCohereExtensionInputTypes mode in Enum.GetValues<EmbeddingVendorCohereExtensionInputTypes>())
        {
            EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Cohere.Gen3.Multilingual, "lorem ipsum", new EmbeddingRequestVendorExtensions
            {
                Cohere = new EmbeddingRequestVendorCohereExtensions
                {
                    InputType = mode,
                    Truncate = EmbeddingVendorCohereExtensionTruncation.End
                }
            });
            
            Console.WriteLine($"Mode: {mode}");
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
    
    [TornadoTest]
    public static async Task EmbedCohereVector()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Cohere.Gen3.Multilingual, [ "lorem ipsum", "dolor sit amet" ]);
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