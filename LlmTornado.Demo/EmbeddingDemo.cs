using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Embedding.Vendors.Cohere;
using LlmTornado.Embedding.Vendors.Google;
using LlmTornado.Embedding.Vendors.Voyage;

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
            
            Console.WriteLine($"... (length: {data.Length})");
        }
    }
    
    [TornadoTest]
    public static async Task EmbedGoogle()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Google.Gemini.Embedding4, "lorem ipsum");
        float[]? data = result?.Data.FirstOrDefault()?.Embedding;

        if (data is not null)
        {
            for (int i = 0; i < Math.Min(data.Length, 10); i++)
            {
                Console.WriteLine(data[i]);
            }
            
            Console.WriteLine($"... (length: {data.Length})");
        }
    }
    
    [TornadoTest]
    public static async Task EmbedVoyage()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Voyage.Gen35.Default, "lorem ipsum", 256, new EmbeddingRequestVendorExtensions
        {
            Voyage = new EmbeddingRequestVendorVoyageExtensions
            {
                OutputDtype = EmbeddingVendorVoyageOutputDtypes.Uint8,
                InputType = EmbeddingVendorVoyageInputTypes.Document
            }
        });
        
        float[]? data = result?.Data.FirstOrDefault()?.Embedding;

        if (data is not null)
        {
            for (int i = 0; i < Math.Min(data.Length, 10); i++)
            {
                Console.WriteLine(data[i]);
            }
            
            Console.WriteLine($"... (length: {data.Length})");
        }
    }
    
    [TornadoTest]
    public static async Task EmbedGoogleExtensions()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(new EmbeddingRequest(EmbeddingModel.Google.Gemini.Embedding4, "This is content of a document")
        {
            VendorExtensions = new EmbeddingRequestVendorExtensions(new EmbeddingRequestVendorGoogleExtensions
            {
                TaskType = EmbeddingRequestVendorGoogleExtensionsTaskTypes.RetrievalDocument,
                Title = "My document 1"
            })
        });
        float[]? data = result?.Data.FirstOrDefault()?.Embedding;

        if (data is not null)
        {
            for (int i = 0; i < Math.Min(data.Length, 10); i++)
            {
                Console.WriteLine(data[i]);
            }
            
            Console.WriteLine($"... (length: {data.Length})");
        }
    }
    
    [TornadoTest]
    public static async Task EmbedGoogleMultiple()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Google.Gemini.Embedding4, [
            "lorem ipsum",
            "dolor sit amet"
        ]);

        if (result is not null)
        {
            foreach (EmbeddingEntry x in result.Data)
            {
                float[] data = x.Embedding;
                
                for (int i = 0; i < Math.Min(data.Length, 10); i++)
                {
                    Console.WriteLine(data[i]);
                }
        
                Console.WriteLine($"... (length: {data.Length})");
                Console.WriteLine("--------------");
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
            
            Console.WriteLine($"... (length: {result.Data[0].Embedding.Length})");
        }
    }
    
    [TornadoTest]
    public static async Task EmbedCohereGen4()
    {
        EmbeddingResult? result = await Program.ConnectMulti().Embeddings.CreateEmbedding(EmbeddingModel.Cohere.Gen4.V4, "lorem ipsum");
        Console.WriteLine($"Count: {result?.Data.Count ?? 0}, dims: {result?.Data.FirstOrDefault()?.Embedding.Length ?? 0}");
        
        if (result is not null)
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(result.Data[0].Embedding[i]);
            }
            
            Console.WriteLine($"... (length: {result.Data[0].Embedding.Length})");
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