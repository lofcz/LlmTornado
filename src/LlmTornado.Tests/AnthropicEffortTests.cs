using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Code.Vendor;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic effort parameter (GA, output_config.effort).
/// </summary>
[TestFixture]
public class AnthropicEffortTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public async Task Setup()
    {
        if (!await Program.SetupApi())
        {
            Assert.Ignore("apiKey.json not found. Skipping Anthropic effort tests.");
        }

        _api = Program.Connect();
        _provider = _api.GetProvider(LLmProviders.Anthropic);
    }

    [TestCase(AnthropicEffortLevels.Low, "low")]
    [TestCase(AnthropicEffortLevels.Medium, "medium")]
    [TestCase(AnthropicEffortLevels.High, "high")]
    public void VendorExtensionEffort_SerializesToOutputConfig(AnthropicEffortLevels effort, string expected)
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Effort = effort
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["output_config"]?["effort"]?.ToString(), Is.EqualTo(expected));
    }

    [TestCase(ChatReasoningEfforts.Low, "low")]
    [TestCase(ChatReasoningEfforts.Medium, "medium")]
    [TestCase(ChatReasoningEfforts.High, "high")]
    public void HarmonizedReasoningEffort_SerializesToOutputConfig(ChatReasoningEfforts effort, string expected)
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningEffort = effort
        };

        JObject body = ParseBody(request);

        Assert.That(body["output_config"]?["effort"]?.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void VendorExtensionEffort_TakesPrecedenceOverReasoningEffort()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningEffort = ChatReasoningEfforts.High,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Effort = AnthropicEffortLevels.Low
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["output_config"]?["effort"]?.ToString(), Is.EqualTo("low"));
    }

    [Test]
    public void AdaptiveThinkingWithVendorEffort_SerializesBoth()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude48.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "What is the capital of France?")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Thinking = AnthropicThinkingSettings.CreateAdaptive(),
                    Effort = AnthropicEffortLevels.Medium
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("adaptive"));
        Assert.That(body["output_config"]?["effort"]?.ToString(), Is.EqualTo("medium"));
    }

    [Test]
    public void XHighReasoningEffort_SerializesAsXHighNotMax()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningEffort = ChatReasoningEfforts.XHigh
        };

        JObject body = ParseBody(request);

        Assert.That(body["output_config"]?["effort"]?.ToString(), Is.EqualTo("xhigh"));
    }

    [Test]
    public void EffortRequest_DoesNotIncludeEffortBetaHeader()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Opus251101,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Effort = AnthropicEffortLevels.Medium
                }
            }
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

        Assert.That(betaHeader, Does.Not.Contain("effort-2025-11-24"));
    }

    [Test]
    public void AnthropicEffortHelper_MapsAllDocumentedLevels()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AnthropicEffortHelper.ToApiValue(AnthropicEffortLevels.Low), Is.EqualTo("low"));
            Assert.That(AnthropicEffortHelper.ToApiValue(AnthropicEffortLevels.Medium), Is.EqualTo("medium"));
            Assert.That(AnthropicEffortHelper.ToApiValue(AnthropicEffortLevels.High), Is.EqualTo("high"));
            Assert.That(AnthropicEffortHelper.ToApiValue(AnthropicEffortLevels.XHigh), Is.EqualTo("xhigh"));
            Assert.That(AnthropicEffortHelper.ToApiValue(AnthropicEffortLevels.Max), Is.EqualTo("max"));
        });
    }

    [Explicit("Requires Anthropic API key and makes real production API calls")]
    [TestCase(AnthropicEffortLevels.Low)]
    [TestCase(AnthropicEffortLevels.Medium)]
    [TestCase(AnthropicEffortLevels.High)]
    public async Task Integration_EffortLevels_ReturnResponse(AnthropicEffortLevels effort)
    {
        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 64,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: effort-ok")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Effort = effort
                }
            }
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("effort-ok").IgnoreCase);
    }

    private JObject ParseBody(ChatRequest request)
    {
        TornadoRequestContent serialized = request.Serialize(_provider!);
        return JObject.Parse(serialized.Body.ToString()!);
    }
}
