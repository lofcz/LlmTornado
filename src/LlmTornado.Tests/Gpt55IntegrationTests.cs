using LlmTornado.Batch;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Images;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Integration and serialization tests for GPT-5.5 and GPT-5.5-pro (Apr 24, 2026 release).
/// </summary>
[TestFixture]
[Category("Integration")]
public class Gpt55IntegrationTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping GPT-5.5 integration tests.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
        _provider = _api.GetProvider(LLmProviders.OpenAi);
    }

    [Test]
    public void Gpt55_ModelRegistration_Works()
    {
        Assert.That(ChatModel.OpenAi.Gpt55.V55.Name, Is.EqualTo("gpt-5.5"));
        Assert.That(ChatModel.OpenAi.Gpt55.V55Pro.Name, Is.EqualTo("gpt-5.5-pro"));
        Assert.That(ChatModel.OpenAi.Gpt55.V55.ContextTokens, Is.EqualTo(1_050_000));
        Assert.That(ChatModel.OpenAi.Gpt55.V55.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Batch));
        Assert.That(ChatModel.OpenAi.Gpt55.V55Pro.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Batch));
        Assert.That(ChatModel.OpenAi.Gpt55.V55Pro.EndpointCapabilities, Does.Not.Contain(ChatModelEndpointCapabilities.Chat));
    }

    [Test]
    public void Gpt55_PromptCacheRetention_DefaultsToExtendedCaching()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["prompt_cache_retention"]?.ToString(), Is.EqualTo("24h"));
    }

    [Test]
    public void Gpt55_PromptCacheRetention_UpgradesInMemoryToExtended()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            PromptCacheRetention = PromptCacheRetention.InMemory
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["prompt_cache_retention"]?.ToString(), Is.EqualTo("24h"));
    }

    [Test]
    public void Gpt55Pro_ResponseRequest_PromptCacheRetention_DefaultsToExtendedCaching()
    {
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55Pro,
            InputString = "Hello"
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["prompt_cache_retention"]?.ToString(), Is.EqualTo("24h"));
    }

    [Test]
    public void Gpt55_ImageDetailAuto_IsSerialized()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.User,
                [
                    new ChatMessagePart("https://example.com/image.jpg", ImageDetail.Auto),
                    new ChatMessagePart("Describe this image briefly.")
                ])
            ]
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        string? detail = body["messages"]?[0]?["content"]?[0]?["image_url"]?["detail"]?.ToString();
        Assert.That(detail, Is.EqualTo("auto"));
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt55_ChatCompletion_ReturnsResponse()
    {
        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: GPT-5.5 OK")],
            MaxTokens = 32
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("GPT-5.5 OK").IgnoreCase);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt55Pro_ResponsesApi_ReturnsResponse()
    {
        ResponseResult result = await _api!.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55Pro,
            Instructions = "Reply concisely.",
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Reply with exactly: GPT-5.5-pro OK")
            ],
            MaxOutputTokens = 32
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Output, Is.Not.Empty);

        ResponseOutputMessageItem? message = result.Output.OfType<ResponseOutputMessageItem>().FirstOrDefault();
        ResponseOutputTextContent? text = message?.Content.OfType<ResponseOutputTextContent>().FirstOrDefault();

        Assert.That(text?.Text, Does.Contain("GPT-5.5-pro OK").IgnoreCase);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt55_BatchCreate_IsAccepted()
    {
        BatchRequest request = new BatchRequest
        {
            Requests =
            [
                new BatchRequestItem("gpt55-1", new ChatRequest
                {
                    Model = ChatModel.OpenAi.Gpt55.V55,
                    Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: batch-ok")]
                })
            ],
            CompletionWindow = BatchCompletionWindow.Hours24
        };

        HttpCallResult<BatchItem> result = await _api!.Batch.Create(request, LLmProviders.OpenAi);

        Assert.That(result.Ok, Is.True, result.Exception?.Message);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Id, Is.Not.Empty);
    }
}
