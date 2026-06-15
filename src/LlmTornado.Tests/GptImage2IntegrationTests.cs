using LlmTornado.Batch;
using LlmTornado.Batch.Vendors.OpenAi;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using LlmTornado.Images.Models.OpenAi;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

[TestFixture]
public class GptImage2IntegrationTests
{
	private static TornadoApi Api => Program.Connect();

	[SetUp]
	public async Task Setup()
	{
		await Program.SetupApi();
	}

	[Test]
	public void ModelRegistration_IncludesGptImage2()
	{
		Assert.That(ImageModel.OpenAi.Gpt.V2.Name, Is.EqualTo("gpt-image-2"));
		Assert.That(ImageModel.AllModelsMap.ContainsKey("gpt-image-2"), Is.True);
		Assert.That(ImageModelOpenAi.AllModelsMap.Contains("gpt-image-2"), Is.True);
	}

	[Test]
	public void GenerationRequest_Serialization_StripsUnsupportedGptImage2Fields()
	{
		IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
		ImageGenerationRequest request = new ImageGenerationRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "A red circle on white background",
			ResponseFormat = TornadoImageResponseFormats.Url,
			Background = ImageBackgroundTypes.Transparent
		};

		string body = request.Serialize(provider).Body as string ?? string.Empty;

		Assert.That(body, Does.Contain("gpt-image-2"));
		Assert.That(body, Does.Not.Contain("response_format"));
		Assert.That(body, Does.Not.Contain("transparent"));
	}

	[Test]
	public void GenerationRequest_Serialization_SupportsFlexibleSize()
	{
		IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
		ImageGenerationRequest request = new ImageGenerationRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "A minimalist landscape",
			Width = 1536,
			Height = 864,
			Quality = TornadoImageQualities.Low
		};

		string body = request.Serialize(provider).Body as string ?? string.Empty;

		Assert.That(body, Does.Contain("\"size\":\"1536x864\""));
	}

	[Test]
	public void EditRequest_Serialization_OmitsInputFidelityForGptImage2()
	{
		IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
		ImageEditRequest request = new ImageEditRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "Add sunglasses",
			InputFidelity = TornadoImageInputFidelity.High,
			Size = TornadoImageSizes.Size1024x1024
		};

		string body = request.Serialize(provider).Body as string ?? string.Empty;

		Assert.That(body, Does.Contain("gpt-image-2"));
		Assert.That(body, Does.Not.Contain("input_fidelity"));
	}

	[Test]
	public void EditRequest_Serialization_SupportsFlexibleSize()
	{
		IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
		ImageEditRequest request = new ImageEditRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "Make the sky purple",
			Width = 2048,
			Height = 1152
		};

		string body = request.Serialize(provider).Body as string ?? string.Empty;

		Assert.That(body, Does.Contain("\"size\":\"2048x1152\""));
	}

	[Test]
	public void BatchRequest_SerializesGptImage2Generation()
	{
		IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
		BatchRequest batch = new BatchRequest
		{
			Requests =
			[
				new BatchRequestItem("img-gen-1", new ImageGenerationRequest
				{
					Model = ImageModel.OpenAi.Gpt.V2,
					Prompt = "A tiny blue square",
					Width = 1024,
					Height = 1024,
					Quality = TornadoImageQualities.Low
				})
			]
		};

		string jsonl = VendorOpenAiBatchRequest.SerializeToJsonl(batch, provider);
		JObject line = JObject.Parse(jsonl.Trim());

		Assert.That(line["url"]?.ToString(), Is.EqualTo("/v1/images/generations"));
		Assert.That(line["body"]?["model"]?.ToString(), Is.EqualTo("gpt-image-2"));
		Assert.That(line["body"]?["size"]?.ToString(), Is.EqualTo("1024x1024"));
	}

	[Test]
	[Category("Integration")]
	public async Task GenerateImage_Production_ReturnsBase64AndUsage()
	{
		ImageGenerationResult? result = await Api.ImageGenerations.CreateImage(new ImageGenerationRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "A simple flat icon of a green leaf on a white background",
			Quality = TornadoImageQualities.Low,
			Size = TornadoImageSizes.Size1024x1024,
			OutputFormat = ImageOutputFormats.Png
		});

		Assert.That(result, Is.Not.Null);
		Assert.That(result!.Data, Is.Not.Null);
		Assert.That(result.Data!.Count, Is.GreaterThan(0));
		Assert.That(result.Data[0].Base64, Is.Not.Null.And.Not.Empty);
		Assert.That(result.Usage, Is.Not.Null);
		Assert.That(result.Usage!.TotalTokens, Is.GreaterThan(0));
		Assert.That(result.Usage.OutputTokens, Is.GreaterThan(0));
	}

	[Test]
	[Category("Integration")]
	public async Task GenerateImage_Production_FlexibleSize()
	{
		ImageGenerationResult? result = await Api.ImageGenerations.CreateImage(new ImageGenerationRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "A wide panoramic view of a calm ocean at dawn, minimal detail",
			Quality = TornadoImageQualities.Low,
			Width = 1536,
			Height = 864,
			OutputFormat = ImageOutputFormats.Png
		});

		Assert.That(result, Is.Not.Null);
		Assert.That(result!.Data?[0].Base64, Is.Not.Null.And.Not.Empty);
		Assert.That(result.Usage?.TotalTokens, Is.GreaterThan(0));
	}

	[Test]
	[Category("Integration")]
	public async Task EditImage_Production_WithHighFidelityInput()
	{
		ImageGenerationResult? source = await Api.ImageGenerations.CreateImage(new ImageGenerationRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "A simple cartoon sun with a smiling face on a plain blue background",
			Quality = TornadoImageQualities.Low,
			Size = TornadoImageSizes.Size1024x1024
		});

		Assert.That(source?.Data?[0].Base64, Is.Not.Null.And.Not.Empty);

		ImageGenerationResult? edited = await Api.ImageEdit.EditImage(new ImageEditRequest
		{
			Model = ImageModel.OpenAi.Gpt.V2,
			Prompt = "Give the sun sunglasses",
			Quality = TornadoImageQualities.Low,
			Size = TornadoImageSizes.Size1024x1024,
			InputFidelity = TornadoImageInputFidelity.High,
			Image = new TornadoInputFile(source!.Data![0].Base64!, "image/png")
		});

		Assert.That(edited, Is.Not.Null);
		Assert.That(edited!.Data?[0].Base64, Is.Not.Null.And.Not.Empty);
		Assert.That(edited.Usage?.InputTokens, Is.GreaterThan(0));
		Assert.That(edited.Usage?.TotalTokens, Is.GreaterThan(0));
	}
}
