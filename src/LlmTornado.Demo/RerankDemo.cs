using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Rerank;
using LlmTornado.Rerank.Models;

namespace LlmTornado.Demo;

public class RerankDemo : DemoBase
{
    [TornadoTest]
    public static async Task RerankDocuments()
    {
        RerankRequest request = new RerankRequest(RerankModel.Voyage.Gen25.Rerank25, "Sample query",
        [
            "Sample document 1",
            "Sample document 2"
        ]);

        RerankResult? result = await Program.ConnectMulti().Rerank.CreateRerank(request);

        Assert.That(result, Is.NotNull);
        Assert.That(result.Data, Is.NotNull);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].RelevanceScore, Is.GreaterThan(0f));

        Console.WriteLine($"Reranked {result.Data.Count} documents.");
        foreach (RerankData data in result.Data)
        {
            Console.WriteLine($"- Index: {data.Index}, Score: {data.RelevanceScore}");
        }
    }

    [TornadoTest]
    public static async Task RerankWithParams()
    {
        RerankRequest request = new RerankRequest(RerankModel.Voyage.Gen25.Rerank25Lite, "Another query",
        [
            "Document A", "Document B", "Document C"
        ])
        {
            TopK = 2,
            ReturnDocuments = true,
            Truncation = true
        };

        RerankResult? result = await Program.ConnectMulti().Rerank.CreateRerank(request);

        Assert.That(result, Is.NotNull);
        Assert.That(result.Data, Is.NotNull);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Document, Is.NotNull);

        Console.WriteLine($"Reranked and returned top {result.Data.Count} documents.");
        foreach (RerankData data in result.Data)
        {
            Console.WriteLine($"- Index: {data.Index}, Score: {data.RelevanceScore}, Document: '{data.Document}'");
        }
    }
}