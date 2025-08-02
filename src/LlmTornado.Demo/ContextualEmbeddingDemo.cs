using System;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Demo;

public class ContextualEmbeddingDemo : DemoBase
{
    [TornadoTest]
    public static async Task EmbedDocumentChunks()
    {
        ContextualEmbeddingRequest request = new ContextualEmbeddingRequest(ContextualEmbeddingModel.Voyage.Gen3.Context3,
        [
            ["doc_1_chunk_1", "doc_1_chunk_2"],
            ["doc_2_chunk_1", "doc_2_chunk_2"]
        ])
        {
            InputType = ContextualEmbeddingInputType.Document
        };
        
        ContextualEmbeddingResult? result = await Program.ConnectMulti().ContextualEmbeddings.CreateContextualEmbedding(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result.Data, Is.NotNull);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[1].Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Data[0].ContextualEmbeddingVector, Is.InstanceOf<ContextualEmbeddingValueFloat>());

        if (result?.Data is not null)
        {
            foreach (ContextualEmbeddingData data in result.Data)
            {
                Console.WriteLine($"- Document Index: {data.Index}, Chunks: {data.Data.Count}");
                foreach (ContextualEmbedding embedding in data.Data)
                {
                    switch (embedding.ContextualEmbeddingVector)
                    {
                        case ContextualEmbeddingValueFloat floatVec:
                            Console.WriteLine($"  - Embedding Index: {embedding.Index}, Type: Float, Length: {floatVec.Values.Length}");
                            Console.WriteLine($"    Values: [{string.Join(", ", floatVec.Values.Take(5))}, ...]");
                            break;
                    }
                }
                Console.WriteLine(new string('-', 20));
            }
        }
    }

    [TornadoTest]
    public static async Task EmbedQueries()
    {
        ContextualEmbeddingRequest request = new ContextualEmbeddingRequest(ContextualEmbeddingModel.Voyage.Gen3.Context3,
        [
            ["query_1"],
            ["query_2"]
        ])
        {
            InputType = ContextualEmbeddingInputType.Query
        };
        
        ContextualEmbeddingResult? result = await Program.ConnectMulti().ContextualEmbeddings.CreateContextualEmbedding(request);

        Assert.That(result, Is.NotNull);
        Assert.That(result.Data, Is.NotNull);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Data.Count, Is.EqualTo(1));
        Assert.That(result.Data[1].Data.Count, Is.EqualTo(1));
        Assert.That(result.Data[0].Data[0].ContextualEmbeddingVector, Is.InstanceOf<ContextualEmbeddingValueFloat>());

        if (result?.Data is not null)
        {
            foreach (ContextualEmbeddingData data in result.Data)
            {
                Console.WriteLine($"- Query Index: {data.Index}");
                foreach (ContextualEmbedding embedding in data.Data)
                {
                    if (embedding.ContextualEmbeddingVector is ContextualEmbeddingValueFloat floatVec)
                    {
                        Console.WriteLine($"  - Type: Float, Length: {floatVec.Values.Length}");
                        Console.WriteLine($"    Values: [{string.Join(", ", floatVec.Values.Take(5))}, ...]");
                    }
                }
                Console.WriteLine(new string('-', 20));
            }
        }
    }
    
    [TornadoTest]
    public static async Task EmbedWithParams()
    {
        ContextualEmbeddingRequest request = new ContextualEmbeddingRequest(ContextualEmbeddingModel.Voyage.Gen3.Context3,
        [
            ["doc_1_chunk_1", "doc_1_chunk_2"],
            ["doc_2_chunk_1", "doc_2_chunk_2"]
        ])
        {
            InputType = ContextualEmbeddingInputType.Document,
            OutputDimension = 256,
            OutputDataType = ContextualEmbeddingOutputDataType.Int8,
            EncodingFormat = ContextualEmbeddingEncodingFormat.Base64
        };
        
        ContextualEmbeddingResult? result = await Program.ConnectMulti().ContextualEmbeddings.CreateContextualEmbedding(request);

        Assert.That(result, Is.NotNull);
        Assert.That(result.Data, Is.NotNull);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[1].Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Data[0].ContextualEmbeddingVector, Is.InstanceOf<ContextualEmbeddingValueString>());
        
        if (result?.Data is not null)
        {
            foreach (ContextualEmbeddingData data in result.Data)
            {
                Console.WriteLine($"- Document Index: {data.Index}, Chunks: {data.Data.Count}");
                foreach (ContextualEmbedding embedding in data.Data)
                {
                    if (embedding.ContextualEmbeddingVector is ContextualEmbeddingValueString stringVec)
                    {
                        Console.WriteLine($"  - Embedding Index: {embedding.Index}, Type: Base64 String, Length: {stringVec.Base64.Length}");
                        Console.WriteLine($"    Value: {stringVec.Base64[..Math.Min(30, stringVec.Base64.Length)]}...");
                    }
                }
                Console.WriteLine(new string('-', 20));
            }
        }
    }
}