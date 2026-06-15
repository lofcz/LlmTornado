using System;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using NUnit.Framework;

namespace LlmTornado.Tests.Images;

/// <summary>
/// Integration tests for OpenAI GPT Image models against the production API.
/// Requires OPENAI_API_KEY environment variable.
/// </summary>
[TestFixture]
[Category("Integration")]
public class OpenAiGptImageIntegrationTests
{
    private TornadoApi _api = null!;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping integration tests.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Generate_gpt_image_2_ReturnsBase64Image()
    {
        ImageGenerationResult? result = await _api.ImageGenerations.CreateImage(new ImageGenerationRequest
        {
            Prompt = "A simple red circle on a white background, flat vector style",
            Model = ImageModel.OpenAi.Gpt.V2,
            Quality = TornadoImageQualities.Low,
            Size = TornadoImageSizes.Size1024x1024,
            NumOfImages = 1
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Data, Is.Not.Null);
        Assert.That(result.Data!.Count, Is.GreaterThan(0));
        Assert.That(result.Data[0].Base64, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Generate_gpt_image_1_mini_ReturnsBase64Image()
    {
        ImageGenerationResult? result = await _api.ImageGenerations.CreateImage(new ImageGenerationRequest
        {
            Prompt = "A simple blue square on a white background, flat vector style",
            Model = ImageModel.OpenAi.Gpt.V1Mini,
            Quality = TornadoImageQualities.Low,
            Size = TornadoImageSizes.Size1024x1024,
            NumOfImages = 1
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Data, Is.Not.Null);
        Assert.That(result.Data!.Count, Is.GreaterThan(0));
        Assert.That(result.Data[0].Base64, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Generate_gpt_image_1_ReturnsBase64Image()
    {
        ImageGenerationResult? result = await _api.ImageGenerations.CreateImage(new ImageGenerationRequest
        {
            Prompt = "A simple green triangle on a white background, flat vector style",
            Model = ImageModel.OpenAi.Gpt.V1,
            Quality = TornadoImageQualities.Low,
            Size = TornadoImageSizes.Size1024x1024,
            NumOfImages = 1
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Data, Is.Not.Null);
        Assert.That(result.Data!.Count, Is.GreaterThan(0));
        Assert.That(result.Data[0].Base64, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task DefaultModel_UsesGptImage2()
    {
        ImageGenerationRequest request = new ImageGenerationRequest("A yellow star on white background, flat vector style");

        Assert.That(request.Model?.Name, Is.EqualTo("gpt-image-2"));

        ImageGenerationResult? result = await _api.ImageGenerations.CreateImage(request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Data, Is.Not.Null);
        Assert.That(result.Data![0].Base64, Is.Not.Null.And.Not.Empty);
    }
}
