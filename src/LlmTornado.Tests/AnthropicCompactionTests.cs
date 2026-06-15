using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Compaction;
using LlmTornado.Common;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit tests for Anthropic server-side compaction (compact-2026-01-12 beta).
/// </summary>
[TestFixture]
public class AnthropicCompactionTests
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
    public void CompactionRequest_SerializesContextManagement()
    {
        CompactionRequest request = new CompactionRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Help me build a website")],
            MaxTokens = 4096,
            TriggerTokenThreshold = 100_000,
            Instructions = "Preserve technical decisions and open tasks.",
            PauseAfterCompaction = true
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["model"]?.ToString(), Is.EqualTo("claude-opus-4-6"));
        Assert.That(body["context_management"]?["edits"]?[0]?["type"]?.ToString(), Is.EqualTo("compact_20260112"));
        Assert.That(body["context_management"]?["edits"]?[0]?["trigger"]?["type"]?.ToString(), Is.EqualTo("input_tokens"));
        Assert.That(body["context_management"]?["edits"]?[0]?["trigger"]?["value"]?.Value<int>(), Is.EqualTo(100_000));
        Assert.That(body["context_management"]?["edits"]?[0]?["instructions"]?.ToString(), Is.EqualTo("Preserve technical decisions and open tasks."));
        Assert.That(body["context_management"]?["edits"]?[0]?["pause_after_compaction"]?.Value<bool>(), Is.True);
        Assert.That(serialized.CapabilityEndpoint, Is.EqualTo(CapabilityEndpoints.Compaction));
    }

    [Test]
    public void CompactionRequest_DefaultContextManagement_UsesCompactEdit()
    {
        CompactionRequest request = new CompactionRequest(ChatModel.Anthropic.Claude46.Sonnet, "Hello");

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["context_management"]?["edits"]?[0]?["type"]?.ToString(), Is.EqualTo("compact_20260112"));
        Assert.That(body["context_management"]?["edits"]?[0]?["trigger"], Is.Null);
    }

    [Test]
    public void CompactionRequest_RoundTripsCompactionBlock()
    {
        CompactionRequest request = new CompactionRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.Assistant,
                [
                    new ChatMessagePart
                    {
                        Type = ChatMessageTypes.Compaction,
                        Text = "Summary of earlier conversation.",
                        EncryptedContent = "opaque-metadata"
                    }
                ]),
                new ChatMessage(ChatMessageRoles.User, "Continue from the summary.")
            ]
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        JToken compactionBlock = body["messages"]![0]!["content"]![0]!;
        Assert.That(compactionBlock["type"]?.ToString(), Is.EqualTo("compaction"));
        Assert.That(compactionBlock["content"]?.ToString(), Is.EqualTo("Summary of earlier conversation."));
        Assert.That(compactionBlock["encrypted_content"]?.ToString(), Is.EqualTo("opaque-metadata"));
    }

    [Test]
    public void CompactionRequest_AddsBetaHeader()
    {
        CompactionRequest request = new CompactionRequest(ChatModel.Anthropic.Claude46.Opus, "Hello");

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider();
        HttpRequestMessage message = provider.OutboundMessage("https://api.anthropic.com/v1/messages", HttpMethod.Post, null, false, request);

        Assert.That(message.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values), Is.True);
        string betaHeader = string.Join(",", values!);
        Assert.That(betaHeader, Does.Contain(CompactionRequest.BetaHeader));
    }

    [Test]
    public void CompactionResult_FromChatResult_ExtractsCompactionBlock()
    {
        ChatResult chatResult = new ChatResult
        {
            Id = "msg_123",
            Choices =
            [
                new ChatChoice
                {
                    FinishReason = ChatMessageFinishReasons.Compaction,
                    Message = new ChatMessage(ChatMessageRoles.Assistant,
                    [
                        new ChatMessagePart
                        {
                            Type = ChatMessageTypes.Compaction,
                            Text = "Compacted summary",
                            EncryptedContent = "opaque"
                        },
                        new ChatMessagePart("Follow-up text")
                    ])
                }
            ],
            Usage = new ChatUsage(LLmProviders.Anthropic) { TotalTokens = 42 }
        };

        CompactionResult result = CompactionResult.FromChatResult(chatResult);

        Assert.That(result.Id, Is.EqualTo("msg_123"));
        Assert.That(result.WasCompacted, Is.True);
        Assert.That(result.CompactionContent, Is.EqualTo("Compacted summary"));
        Assert.That(result.EncryptedContent, Is.EqualTo("opaque"));
        Assert.That(result.Text, Is.EqualTo("Follow-up text"));
        Assert.That(result.Usage?.TotalTokens, Is.EqualTo(42));
    }
}

/// <summary>
/// Integration tests for Anthropic compaction with the real API.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicCompactionIntegrationTests
{
    private TornadoApi? _api;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("ANTHROPIC_API_KEY environment variable not set. Skipping Anthropic compaction integration tests.");
        }

        _api = new TornadoApi(LLmProviders.Anthropic, apiKey);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Compaction_EnabledRequest_ReturnsResponse()
    {
        CompactionResult result = await _api!.Compaction.Compact(new CompactionRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: compaction-ok")],
            MaxTokens = 256
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Text, Does.Contain("compaction-ok").IgnoreCase);
        Assert.That(result.Usage?.TotalTokens, Is.GreaterThan(0));
        Assert.That(result.WasCompacted, Is.False);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Compaction_TokenizeWithContextManagement_AcceptedByApi()
    {
        ChatRequest chatRequest = new CompactionRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Count tokens for this compaction-enabled request.")]
        }.ToChatRequest();

        Tokenize.TokenizeResult? tokenResult = await _api!.Tokenize.CountTokens(new Tokenize.TokenizeRequest(chatRequest));

        Assert.That(tokenResult, Is.Not.Null);
        Assert.That(tokenResult!.TotalTokens, Is.GreaterThan(0));
    }
}
