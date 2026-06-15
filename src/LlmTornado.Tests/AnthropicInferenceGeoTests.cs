using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic data residency (<c>inference_geo</c>).
/// </summary>
[TestFixture]
public class AnthropicInferenceGeoTests
{
    private static IEndpointProvider CreateProvider()
    {
        return new TornadoApi(LLmProviders.Anthropic, "test-key").GetProvider(LLmProviders.Anthropic);
    }

    [Test]
    public void InferenceGeo_VendorExtensions_CanBeSet()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    InferenceGeo = AnthropicInferenceGeoOptions.Us
                }
            }
        };

        Assert.That(request.VendorExtensions?.Anthropic?.InferenceGeo, Is.EqualTo(AnthropicInferenceGeoOptions.Us));
    }

    [Test]
    public void InferenceGeo_SerializesUs_InOutboundRequest()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 64,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    InferenceGeo = AnthropicInferenceGeoOptions.Us
                }
            }
        };

        TornadoRequestContent serialized = request.Serialize(CreateProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["inference_geo"]?.ToString(), Is.EqualTo("us"));
    }

    [Test]
    public void InferenceGeo_SerializesGlobal_WhenExplicitlySet()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 64,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    InferenceGeo = AnthropicInferenceGeoOptions.Global
                }
            }
        };

        TornadoRequestContent serialized = request.Serialize(CreateProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["inference_geo"]?.ToString(), Is.EqualTo("global"));
    }

    [Test]
    public void InferenceGeo_Omitted_WhenNotSet()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 64,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        TornadoRequestContent serialized = request.Serialize(CreateProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body.ContainsKey("inference_geo"), Is.False);
    }

    [Test]
    public void InferenceGeo_Usage_DeserializesFromResponse()
    {
        const string json = """
            {
              "input_tokens": 10,
              "output_tokens": 5,
              "inference_geo": "us"
            }
            """;

        VendorAnthropicUsage? usage = Newtonsoft.Json.JsonConvert.DeserializeObject<VendorAnthropicUsage>(json);

        Assert.That(usage, Is.Not.Null);
        Assert.That(usage!.InferenceGeo, Is.EqualTo(AnthropicInferenceGeoOptions.Us));
        Assert.That(usage.InputTokens, Is.EqualTo(10));
        Assert.That(usage.OutputTokens, Is.EqualTo(5));
    }

    [Test]
    public void InferenceGeo_Usage_DeserializesNotAvailable_FromResponse()
    {
        const string json = """
            {
              "input_tokens": 10,
              "output_tokens": 5,
              "inference_geo": "not_available"
            }
            """;

        VendorAnthropicUsage? usage = Newtonsoft.Json.JsonConvert.DeserializeObject<VendorAnthropicUsage>(json);

        Assert.That(usage, Is.Not.Null);
        Assert.That(usage!.InferenceGeo, Is.EqualTo(AnthropicInferenceGeoOptions.NotAvailable));
    }

}

/// <summary>
/// Live API tests for Anthropic <c>inference_geo</c> (requires API key).
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicInferenceGeoIntegrationTests
{
    private TornadoApi? _api;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        string? envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _api = new TornadoApi(LLmProviders.Anthropic, envKey);
            return;
        }

        if (await Program.SetupApi() && !string.IsNullOrWhiteSpace(Program.ApiKeys.Anthropic))
        {
            _api = new TornadoApi(LLmProviders.Anthropic, Program.ApiKeys.Anthropic);
        }
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task CreateChatCompletion_UsInferenceGeo_ReturnsUsInUsage()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        HttpCallResult<ChatResult> result = await _api.Chat.CreateChatCompletionSafe(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 32,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: pong")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    InferenceGeo = AnthropicInferenceGeoOptions.Us
                }
            }
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? "Request failed");
        Assert.That(result.Data?.Usage, Is.Not.Null);

        VendorAnthropicUsage? vendorUsage = result.Data!.Usage!.VendorUsageObject as VendorAnthropicUsage;
        Assert.That(vendorUsage, Is.Not.Null);
        Assert.That(vendorUsage!.InferenceGeo, Is.EqualTo(AnthropicInferenceGeoOptions.Us),
            "API should report inference_geo=us in usage when US-only inference is requested");
    }
}
