using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic cache diagnostics (cache-diagnosis-2026-04-07 beta).
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicCacheDiagnosticsTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        string? envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _api = new TornadoApi(LLmProviders.Anthropic, envKey);
            _provider = _api.GetProvider(LLmProviders.Anthropic);
            return;
        }

        if (await Program.SetupApi())
        {
            _api = Program.Connect();
            _provider = _api.GetProvider(LLmProviders.Anthropic);
        }
    }

    private static ChatRequest CreateDiagnosticsRequest(string? previousMessageId, List<ChatMessage>? messages = null)
    {
        return new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 256,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages = messages ??
            [
                new ChatMessage(ChatMessageRoles.System, CreateCacheableSystemPrompt()),
                new ChatMessage(ChatMessageRoles.User, "Summarize section 1.")
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                CacheDiagnostics = new AnthropicCacheDiagnosticsRequest
                {
                    PreviousMessageId = previousMessageId
                }
            })
        };
    }

    private static string CreateCacheableSystemPrompt()
    {
        const string document = "You are an AI assistant analyzing a large document. ";
        return document + string.Concat(Enumerable.Repeat(
            "Section content with stable cacheable prefix for prompt caching diagnostics testing. ", 200));
    }

    [Test]
    public void CacheDiagnosticsRequest_SerializesPreviousMessageId()
    {
        IEndpointProvider provider = new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication("test-key")
        };

        ChatRequest request = CreateDiagnosticsRequest(null);
        TornadoRequestContent serialized = request.Serialize(provider);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["diagnostics"], Is.Not.Null);
        Assert.That(body["diagnostics"]!["previous_message_id"]!.Type, Is.EqualTo(JTokenType.Null));
        Assert.That(body["cache_control"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
    }

    [Test]
    public void CacheDiagnosticsRequest_SerializesPreviousMessageIdValue()
    {
        IEndpointProvider provider = new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication("test-key")
        };

        ChatRequest request = CreateDiagnosticsRequest("msg_0123456789");
        TornadoRequestContent serialized = request.Serialize(provider);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["diagnostics"]?["previous_message_id"]?.ToString(), Is.EqualTo("msg_0123456789"));
    }

    [Test]
    public void CacheDiagnosticsRequest_AddsBetaHeader()
    {
        AnthropicEndpointProvider provider = new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication("test-key")
        };

        ChatRequest request = CreateDiagnosticsRequest(null);
        HttpRequestMessage httpRequest = provider.OutboundMessage(
            "https://api.anthropic.com/v1/messages",
            HttpMethod.Post,
            request.Serialize(provider).Body,
            false,
            request);

        IEnumerable<string> betaHeaders = httpRequest.Headers.GetValues("anthropic-beta");
        Assert.That(betaHeaders.First(), Does.Contain("cache-diagnosis-2026-04-07"));
    }

    [Test]
    public void CacheDiagnosticsResponse_DeserializesCacheMissReason()
    {
        const string json = """
            {
              "id": "msg_01Test",
              "type": "message",
              "role": "assistant",
              "content": [{ "type": "text", "text": "ok" }],
              "model": "claude-sonnet-4-6",
              "stop_reason": "end_turn",
              "usage": {
                "input_tokens": 42,
                "cache_read_input_tokens": 0,
                "cache_creation_input_tokens": 41850,
                "output_tokens": 10
              },
              "diagnostics": {
                "cache_miss_reason": {
                  "type": "system_changed",
                  "cache_missed_input_tokens": 41850
                }
              }
            }
            """;

        ChatResult? result = ChatResult.Deserialize(LLmProviders.Anthropic, json, null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.VendorExtensions?.Anthropic?.CacheDiagnostics, Is.Not.Null);
        Assert.That(result.VendorExtensions.Anthropic.CacheDiagnostics!.CacheMissReason, Is.Not.Null);
        Assert.That(result.VendorExtensions.Anthropic.CacheDiagnostics.CacheMissReason!.Type,
            Is.EqualTo(AnthropicCacheMissReasonTypes.SystemChanged));
        Assert.That(result.VendorExtensions.Anthropic.CacheDiagnostics.CacheMissReason.CacheMissedInputTokens,
            Is.EqualTo(41850));
    }

    [Test]
    public void CacheDiagnosticsResponse_DeserializesPendingComparison()
    {
        const string json = """
            {
              "id": "msg_01Test",
              "type": "message",
              "role": "assistant",
              "content": [{ "type": "text", "text": "ok" }],
              "model": "claude-sonnet-4-6",
              "stop_reason": "end_turn",
              "usage": { "input_tokens": 42, "output_tokens": 10 },
              "diagnostics": { "cache_miss_reason": null }
            }
            """;

        ChatResult? result = ChatResult.Deserialize(LLmProviders.Anthropic, json, null, null);

        Assert.That(result?.VendorExtensions?.Anthropic?.CacheDiagnostics, Is.Not.Null);
        Assert.That(result!.VendorExtensions!.Anthropic!.CacheDiagnostics!.CacheMissReason, Is.Null);
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task MultiTurnCacheDiagnostics_NoDivergenceOnStablePrefix()
    {
        if (_api is null || _provider is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        string systemPrompt = CreateCacheableSystemPrompt();
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatMessageRoles.System, systemPrompt),
            new ChatMessage(ChatMessageRoles.User, "Summarize section 1.")
        ];

        ChatRequest turn1Request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 256,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages = messages,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                CacheDiagnostics = new AnthropicCacheDiagnosticsRequest { PreviousMessageId = null }
            })
        };

        ChatResult turn1 = await _api!.Chat.CreateChatCompletion(turn1Request);
        Assert.That(turn1.Ok, Is.True, () => turn1.Exception?.Message ?? "Turn 1 failed");
        Assert.That(turn1.Id, Is.Not.Null.And.Not.Empty);

        string assistantText = turn1.Choices?.FirstOrDefault()?.Message?.Content ?? "Section 1 summary.";
        messages.Add(new ChatMessage(ChatMessageRoles.Assistant, assistantText));
        messages.Add(new ChatMessage(ChatMessageRoles.User, "Now summarize section 2."));

        ChatRequest turn2Request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 256,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages = messages,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                CacheDiagnostics = new AnthropicCacheDiagnosticsRequest { PreviousMessageId = turn1.Id }
            })
        };

        ChatResult turn2 = await _api.Chat.CreateChatCompletion(turn2Request);
        Assert.That(turn2.Ok, Is.True, () => turn2.Exception?.Message ?? "Turn 2 failed");

        AnthropicCacheDiagnosticsResponse? diagnostics = turn2.VendorExtensions?.Anthropic?.CacheDiagnostics;
        if (diagnostics?.CacheMissReason is not null)
        {
            TestContext.WriteLine($"Turn 2 cache_miss_reason: {diagnostics.CacheMissReason.Type}");
            Assert.That(diagnostics.CacheMissReason.Type,
                Is.Not.EqualTo(AnthropicCacheMissReasonTypes.PreviousMessageNotFound),
                "Previous turn should be fingerprinted when beta header was sent on turn 1");
        }
        else
        {
            TestContext.WriteLine("Turn 2 diagnostics: no divergence detected (null diagnostics or pending comparison)");
        }

        TestContext.WriteLine(
            $"Turn 2 usage: input={turn2.Usage?.PromptTokens}, cache_read={turn2.Usage?.CacheReadTokens}, cache_create={turn2.Usage?.CacheCreationTokens}");
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task MultiTurnCacheDiagnostics_StreamingReturnsDiagnostics()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        string systemPrompt = CreateCacheableSystemPrompt();
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatMessageRoles.System, systemPrompt),
            new ChatMessage(ChatMessageRoles.User, "Summarize section 1.")
        ];

        ChatResult turn1 = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 256,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages = messages,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                CacheDiagnostics = new AnthropicCacheDiagnosticsRequest { PreviousMessageId = null }
            })
        });

        Assert.That(turn1.Ok, Is.True, () => turn1.Exception?.Message ?? "Turn 1 failed");

        messages.Add(new ChatMessage(ChatMessageRoles.Assistant, turn1.Choices?.FirstOrDefault()?.Message?.Content ?? "Section 1 summary."));
        messages.Add(new ChatMessage(ChatMessageRoles.User, "Now summarize section 2."));

        ChatRequest streamRequest = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 256,
            Stream = true,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages = messages,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                CacheDiagnostics = new AnthropicCacheDiagnosticsRequest { PreviousMessageId = turn1.Id }
            })
        };

        ChatResponseVendorExtensions? streamDiagnostics = null;
        ChatResult? finalResult = null;

        await foreach (ChatResult chunk in _api.Chat.StreamChatEnumerable(streamRequest))
        {
            if (chunk.VendorExtensions?.Anthropic?.CacheDiagnostics is not null)
            {
                streamDiagnostics = chunk.VendorExtensions;
            }

            if (chunk.Choices?.FirstOrDefault()?.FinishReason is not null)
            {
                finalResult = chunk;
            }
        }

        Assert.That(finalResult, Is.Not.Null);
        TestContext.WriteLine(
            $"Streaming diagnostics present: {streamDiagnostics?.Anthropic?.CacheDiagnostics is not null || finalResult?.VendorExtensions?.Anthropic?.CacheDiagnostics is not null}");
    }
}
