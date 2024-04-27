using LlmTornado.Embedding;

namespace LlmTornado.Demo;

public static class EmbeddingDemo
{
    public static async Task Embed()
    {
        EmbeddingResult result = await Program.Connect().Embeddings.CreateEmbeddingAsync("lorem ipsum");
        float[]? data = result.Data.FirstOrDefault()?.Embedding;

        if (data is not null)
        {
            for (var i = 0; i < Math.Min(data.Length, 10); i++)
            {
                Console.WriteLine(data[i]);
            }
        }
    }
}