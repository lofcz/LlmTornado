using LlmTornado.Images;
using LlmTornado.Models;

namespace LlmTornado.Demo;

public class ImagesDemo
{
    public static async Task Generate()
    {
        ImageResult? generatedImg = await Program.Connect().ImageGenerations.CreateImageAsync(new ImageGenerationRequest("a cute cat", quality: ImageQuality.Hd, responseFormat: ImageResponseFormat.Url, model: Model.Dalle3));
    }
}