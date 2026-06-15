using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Demo;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic fast mode (speed: "fast", fast-mode-2026-02-01 beta header).
/// </summary>
[TestFixture]
public class AnthropicFastModeTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public void Setup()
    {
        _api = new TornadoApi("test-key");
        _provider = _api.GetProvider(LLmProviders.Anthropic);
    }

    [Test]
    public void FastMode_ModelRegistration_Works()
    {
        Assert.That(ChatModel.Anthropic.Claude47.Opus.Name, Is.EqualTo("claude-opus-4-7"));
        Assert.That(ChatModel.Anthropic.Claude47.NextOpus.Name, Is.EqualTo("claude-opus-4-8"));
        Assert.That(ChatModel.Anthropic.Claude47.Opus48.Name, Is.EqualTo("claude-opus-4-8"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-opus-4-7"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-opus-4-8"));
    }

    [TestCase("claude-opus-4-7")]
    [TestCase("claude-opus-4-8")]
    public void FastMode_SerializesSpeedToRequestBody(string modelName)
    {
        ChatRequest request = new ChatRequest
        {
            Model = new ChatModel(modelName, LLmProviders.Anthropic, 200_000),
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Speed = ChatRequestSpeeds.Fast
        };

        JObject body = ParseBody(request);

        Assert.That(body["speed"]?.ToString(), Is.EqualTo("fast"));
    }

    [Test]
    public void FastMode_StandardSpeed_NotSerialized()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Speed = ChatRequestSpeeds.Standard
        };

        JObject body = ParseBody(request);

        Assert.That(body.ContainsKey("speed"), Is.False);
    }

    [Test]
    public void FastMode_UnsetSpeed_NotSerialized()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        JObject body = ParseBody(request);

        Assert.That(body.ContainsKey("speed"), Is.False);
    }

    [TestCase("claude-opus-4-7")]
    [TestCase("claude-opus-4-8")]
    public void FastMode_AddsBetaHeader(string modelName)
    {
        ChatRequest request = new ChatRequest
        {
            Model = new ChatModel(modelName, LLmProviders.Anthropic, 200_000),
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Speed = ChatRequestSpeeds.Fast
        };

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider
        {
            Api = _api
        };

        HttpRequestMessage httpRequest = provider.OutboundMessage(
            "https://api.anthropic.com/v1/messages",
            HttpMethod.Post,
            "{}",
            false,
            request);

        string? betaHeader = httpRequest.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values)
            ? string.Join(",", values)
            : null;

        Assert.That(betaHeader, Does.Contain("fast-mode-2026-02-01"));
    }

    [Test]
    public void FastMode_DoesNotAddBetaHeaderWhenUnset()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider
        {
            Api = _api
        };

        HttpRequestMessage httpRequest = provider.OutboundMessage(
            "https://api.anthropic.com/v1/messages",
            HttpMethod.Post,
            "{}",
            false,
            request);

        string? betaHeader = httpRequest.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values)
            ? string.Join(",", values)
            : null;

        Assert.That(betaHeader, Does.Not.Contain("fast-mode-2026-02-01"));
    }

    [Test]
    public void FastMode_Opus47_DoesNotIncludeInterleavedThinkingHeader()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Speed = ChatRequestSpeeds.Fast
        };

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider
        {
            Api = _api
        };

        HttpRequestMessage httpRequest = provider.OutboundMessage(
            "https://api.anthropic.com/v1/messages",
            HttpMethod.Post,
            "{}",
            false,
            request);

        string? betaHeader = httpRequest.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values)
            ? string.Join(",", values)
            : null;

        Assert.That(betaHeader, Does.Not.Contain("interleaved-thinking-2025-05-14"));
    }

    private JObject ParseBody(ChatRequest request)
    {
        TornadoRequestContent serialized = request.Serialize(_provider!);
        return JObject.Parse(serialized.Body.ToString()!);
    }
}

/// <summary>
/// Integration tests for Anthropic fast mode with the real API.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicFastModeIntegrationTests
{
    private TornadoApi? _api;

    [SetUp]
    public async Task Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            if (!await Program.SetupApi())
            {
                Assert.Ignore("ANTHROPIC_API_KEY not set and apiKey.json not found. Skipping Anthropic fast mode integration tests.");
            }

            apiKey = Program.ApiKeys.Anthropic;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Ignore("Anthropic API key not configured. Skipping Anthropic fast mode integration tests.");
        }

        _api = new TornadoApi(LLmProviders.Anthropic, apiKey);
    }

    [Test]
    [Explicit("Requires API key, fast mode access, and makes real API calls")]
    public async Task Opus47_FastMode_ReturnsResponse()
    {
        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: fast-mode-ok")],
            MaxTokens = 64,
            Speed = ChatRequestSpeeds.Fast
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Ok, Is.True, result.Exception?.Message);
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("fast-mode-ok").IgnoreCase);
        Assert.That(result.Speed, Is.EqualTo(ChatRequestSpeeds.Fast));
    }

    [Test]
    [Explicit("Requires API key, fast mode access, and makes real API calls")]
    public async Task NextOpus_FastMode_ReturnsResponse()
    {
        HttpCallResult<ChatResult> result = await _api!.Chat.CreateChatCompletionSafe(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.NextOpus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: next-opus-fast-ok")],
            MaxTokens = 64,
            Speed = ChatRequestSpeeds.Fast
        });

        Assert.That(result.Ok, Is.True, result.Response);
        Assert.That(result.Data?.Choices, Is.Not.Empty);
        Assert.That(result.Data!.Choices![0].Message?.Content, Does.Contain("next-opus-fast-ok").IgnoreCase);
        Assert.That(result.Data.Speed, Is.EqualTo(ChatRequestSpeeds.Fast));
    }
}
