using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Integration and serialization tests for GPT-5.6 Sol/Terra/Luna (Jul 9, 2026 release).
/// </summary>
[TestFixture]
[Category("Integration")]
public class Gpt56IntegrationTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping GPT-5.6 integration tests.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
        _provider = _api.GetProvider(LLmProviders.OpenAi);
    }

    [Test]
    public void Gpt56_ModelRegistration_Works()
    {
        Assert.That(ChatModel.OpenAi.Gpt56.V56.Name, Is.EqualTo("gpt-5.6"));
        Assert.That(ChatModel.OpenAi.Gpt56.V56Sol.Name, Is.EqualTo("gpt-5.6-sol"));
        Assert.That(ChatModel.OpenAi.Gpt56.V56Terra.Name, Is.EqualTo("gpt-5.6-terra"));
        Assert.That(ChatModel.OpenAi.Gpt56.V56Luna.Name, Is.EqualTo("gpt-5.6-luna"));
        Assert.That(ChatModel.OpenAi.Gpt56.V56.ContextTokens, Is.EqualTo(1_050_000));
        Assert.That(ChatModel.OpenAi.Gpt56.V56Sol.ContextTokens, Is.EqualTo(1_050_000));
        Assert.That(ChatModel.OpenAi.Gpt56.V56.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Chat));
        Assert.That(ChatModel.OpenAi.Gpt56.V56.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Responses));
        Assert.That(ChatModel.OpenAi.Gpt56.V56.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Batch));
        Assert.That(ChatModelOpenAiGpt56.ModelsAll, Has.Count.EqualTo(4));
        Assert.That(ChatModelOpenAi.ReasoningModelsAll, Does.Contain(ChatModel.OpenAi.Gpt56.V56Sol));
        Assert.That(ChatModelOpenAi.WebSearchCompatibleModelsAll, Does.Contain(ChatModel.OpenAi.Gpt56.V56Terra));
        Assert.That(ChatModelOpenAi.ComputerUseModelsAllSet, Does.Contain(ChatModel.OpenAi.Gpt56.V56Luna));
        Assert.That(ChatModelOpenAi.ToolSearchModelsAllSet, Does.Contain(ChatModel.OpenAi.Gpt56.V56));
        Assert.That(ChatModelOpenAi.CompactionModelsAllSet, Does.Contain(ChatModel.OpenAi.Gpt56.V56Sol));
        Assert.That(ChatModelOpenAi.SamplingParamsConditionallySupported, Does.Contain(ChatModel.OpenAi.Gpt56.V56Sol));
    }

    [Test]
    public void Gpt56_SamplingParams_ClearedWhenReasoningIsNotNone()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt56.V56Sol,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Temperature = 0.7,
            TopP = 0.9,
            ReasoningEffort = ChatReasoningEfforts.Medium
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["temperature"], Is.Null);
        Assert.That(body["top_p"], Is.Null);
        Assert.That(body["reasoning_effort"]?.ToString(), Is.EqualTo("medium"));
    }

    [Test]
    public void Gpt56_SamplingParams_KeptWhenReasoningIsNone()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt56.V56Terra,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Temperature = 0.7,
            TopP = 0.9,
            ReasoningEffort = ChatReasoningEfforts.None
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["temperature"]?.Value<double>(), Is.EqualTo(0.7).Within(0.001));
        Assert.That(body["top_p"]?.Value<double>(), Is.EqualTo(0.9).Within(0.001));
        Assert.That(body["reasoning_effort"]?.ToString(), Is.EqualTo("none"));
    }

    [Test]
    public void Gpt56_MaxReasoningEffort_Serializes()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt56.V56,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningEffort = ChatReasoningEfforts.Max
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["reasoning_effort"]?.ToString(), Is.EqualTo("max"));
    }

    [Test]
    public void Gpt56_ResponseRequest_MaxReasoningEffort_Serializes()
    {
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt56.V56Sol,
            InputString = "Hello",
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Max)
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["reasoning"]?["effort"]?.ToString(), Is.EqualTo("max"));
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt56_ChatCompletion_ReturnsResponse()
    {
        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt56.V56Luna,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with the single word: pong")],
            ReasoningEffort = ChatReasoningEfforts.None,
            MaxTokens = 32
        });

        Assert.That(result.Choices, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Is.Not.Null.And.Not.Empty);
    }
}

