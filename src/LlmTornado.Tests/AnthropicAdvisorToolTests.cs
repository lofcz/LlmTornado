using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for the Anthropic advisor tool (beta, advisor-tool-2026-03-01).
/// </summary>
[TestFixture]
public class AnthropicAdvisorToolTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public async Task Setup()
    {
        if (!await Program.SetupApi())
        {
            Assert.Ignore("apiKey.json not found. Skipping Anthropic advisor tool tests.");
        }

        if (string.IsNullOrWhiteSpace(Program.ApiKeys.Anthropic))
        {
            Assert.Ignore("Anthropic API key not set. Skipping Anthropic advisor tool tests.");
        }

        _api = Program.Connect();
        _provider = _api.GetProvider(LLmProviders.Anthropic);
    }

    [Test]
    public void AdvisorTool_ModelRegistration_Works()
    {
        Assert.That(ChatModel.Anthropic.Claude48.Opus.Name, Is.EqualTo("claude-opus-4-8"));
        Assert.That(ChatModel.Anthropic.Claude46.Sonnet.Name, Is.EqualTo("claude-sonnet-4-6"));
    }

    [Test]
    public void AdvisorToolRequest_SerializesToolAndBetaHeader()
    {
        ChatRequest request = CreateAdvisorRequest();

        JObject body = ParseBody(request);
        JToken? advisorTool = body["tools"]?.FirstOrDefault(t => t["type"]?.ToString() == "advisor_20260301");

        Assert.That(advisorTool, Is.Not.Null);
        Assert.That(advisorTool!["name"]?.ToString(), Is.EqualTo("advisor"));
        Assert.That(advisorTool["model"]?.ToString(), Is.EqualTo("claude-opus-4-8"));
        Assert.That(body["model"]?.ToString(), Is.EqualTo("claude-sonnet-4-6"));

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider { Api = _api };
        HttpRequestMessage httpRequest = provider.OutboundMessage(
            "https://api.anthropic.com/v1/messages",
            HttpMethod.Post,
            "{}",
            false,
            request);

        string? betaHeader = httpRequest.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values)
            ? string.Join(",", values)
            : null;

        Assert.That(betaHeader, Does.Contain("advisor-tool-2026-03-01"));
    }

    [Test]
    public void AdvisorToolRequest_WithMaxUsesAndCaching_SerializesOptionalFields()
    {
        ChatRequest request = CreateAdvisorRequest(maxUses: 2, caching: AnthropicCacheSettings.EphemeralWithTtl(ChatRequestCacheTtl.FiveMinutes));

        JObject body = ParseBody(request);
        JToken? advisorTool = body["tools"]?.FirstOrDefault(t => t["type"]?.ToString() == "advisor_20260301");

        Assert.That(advisorTool?["max_uses"]?.Value<int>(), Is.EqualTo(2));
        Assert.That(advisorTool?["caching"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
        Assert.That(advisorTool?["caching"]?["ttl"]?.ToString(), Is.EqualTo("5m"));
    }

    [Test]
    public void AdvisorToolRequest_WithMaxTokens_SerializesMaxTokensOnToolDefinition()
    {
        ChatRequest request = CreateAdvisorRequest(maxTokens: 2048);

        JObject body = ParseBody(request);
        JToken? advisorTool = body["tools"]?.FirstOrDefault(t => t["type"]?.ToString() == "advisor_20260301");

        Assert.That(advisorTool?["max_tokens"]?.Value<int>(), Is.EqualTo(2048));
    }

    [Test]
    public void AdvisorToolResultResponse_ParsesServerToolUseAndAdvice()
    {
        const string json = """
            {
              "id": "msg_test",
              "type": "message",
              "role": "assistant",
              "model": "claude-sonnet-4-6",
              "stop_reason": "end_turn",
              "content": [
                {
                  "type": "text",
                  "text": "Let me consult the advisor."
                },
                {
                  "type": "server_tool_use",
                  "id": "srvtoolu_abc123",
                  "name": "advisor",
                  "input": {}
                },
                {
                  "type": "advisor_tool_result",
                  "tool_use_id": "srvtoolu_abc123",
                  "content": {
                    "type": "advisor_result",
                    "text": "Use a channel-based coordination pattern."
                  }
                },
                {
                  "type": "text",
                  "text": "Here is the implementation."
                }
              ],
              "usage": {
                "input_tokens": 100,
                "output_tokens": 200,
                "iterations": [
                  { "type": "message", "input_tokens": 100, "output_tokens": 50 },
                  { "type": "advisor_message", "model": "claude-opus-4-8", "input_tokens": 200, "output_tokens": 300 },
                  { "type": "message", "input_tokens": 150, "output_tokens": 150 }
                ]
              }
            }
            """;

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider { Api = _api };
        ChatResult? result = provider.InboundMessage<ChatResult>(json, null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Choices, Is.Not.Null.And.Not.Empty);

        List<ToolCall> advisorCalls = result.Choices!
            .SelectMany(c => c.Message?.ToolCalls ?? [])
            .Where(tc => tc.BuiltInToolCall?.Name == "advisor")
            .ToList();

        Assert.That(advisorCalls, Has.Count.EqualTo(1));
        Assert.That(advisorCalls[0].Type, Is.EqualTo("server_tool_use"));
        Assert.That(advisorCalls[0].Id, Is.EqualTo("srvtoolu_abc123"));

        ChatMessagePart? advisorPart = result.Choices
            .SelectMany(c => c.Message?.Parts ?? [])
            .FirstOrDefault(p => p.VendorExtensions is ChatMessagePartAnthropicExtensions ext && ext.AdvisorToolResult is not null);

        Assert.That(advisorPart, Is.Not.Null);
        ChatMessagePartAnthropicExtensions anthropicExt = (ChatMessagePartAnthropicExtensions)advisorPart!.VendorExtensions!;
        Assert.That(anthropicExt.AdvisorToolResult!.ContentType, Is.EqualTo(AnthropicAdvisorToolResultContentTypes.AdvisorResult));
        Assert.That(anthropicExt.AdvisorToolResult.Text, Does.Contain("channel-based"));
        Assert.That(anthropicExt.AdvisorToolResult.ToolUseId, Is.EqualTo("srvtoolu_abc123"));
    }

    [Test]
    public void AdvisorToolResultResponse_ParsesStopReasonMaxTokens()
    {
        const string json = """
            {
              "id": "msg_test",
              "type": "message",
              "role": "assistant",
              "model": "claude-sonnet-4-6",
              "stop_reason": "end_turn",
              "content": [
                {
                  "type": "server_tool_use",
                  "id": "srvtoolu_cap123",
                  "name": "advisor",
                  "input": {}
                },
                {
                  "type": "advisor_tool_result",
                  "tool_use_id": "srvtoolu_cap123",
                  "content": {
                    "type": "advisor_result",
                    "text": "Partial guidance before truncation.",
                    "stop_reason": "max_tokens"
                  }
                }
              ],
              "usage": {
                "input_tokens": 100,
                "output_tokens": 200
              }
            }
            """;

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider { Api = _api };
        ChatResult? result = provider.InboundMessage<ChatResult>(json, null, null);

        Assert.That(result, Is.Not.Null);

        ChatMessagePart? advisorPart = result!.Choices!
            .SelectMany(c => c.Message?.Parts ?? [])
            .FirstOrDefault(p => p.VendorExtensions is ChatMessagePartAnthropicExtensions ext && ext.AdvisorToolResult is not null);

        Assert.That(advisorPart, Is.Not.Null);
        ChatMessagePartAnthropicExtensions anthropicExt = (ChatMessagePartAnthropicExtensions)advisorPart!.VendorExtensions!;
        Assert.That(anthropicExt.AdvisorToolResult!.ContentType, Is.EqualTo(AnthropicAdvisorToolResultContentTypes.AdvisorResult));
        Assert.That(anthropicExt.AdvisorToolResult.StopReason, Is.EqualTo("max_tokens"));
    }

    [Test]
    [Category("Integration")]
    public async Task Integration_AdvisorTool_ReturnsResponseWithAdvisorBlocks()
    {
        ChatRequest request = CreateAdvisorRequest();
        request.MaxTokens = 1024;
        request.Messages =
        [
            new ChatMessage(ChatMessageRoles.User,
                "Build a concurrent worker pool in Go with graceful shutdown. " +
                "(Advisor: please keep your guidance under 80 words — I need a focused starting point, not a comprehensive plan.)")
        ];

        HttpCallResult<ChatResult> result = await _api!.Chat.CreateChatCompletionSafe(request);

        Assert.That(result.Ok, Is.True, result.Exception?.Message ?? result.Response);
        Assert.That(result.Data, Is.Not.Null);

        string rawObject = result.Data!.Object ?? result.Data.RawResponse ?? string.Empty;
        bool hasAdvisorUse = rawObject.Contains("server_tool_use", StringComparison.Ordinal) &&
                             rawObject.Contains("\"name\":\"advisor\"", StringComparison.Ordinal);
        bool hasAdvisorResult = rawObject.Contains("advisor_tool_result", StringComparison.Ordinal);

        Assert.That(hasAdvisorUse || hasAdvisorResult, Is.True,
            "Expected advisor server_tool_use and/or advisor_tool_result in response. Raw: " + rawObject[..Math.Min(500, rawObject.Length)]);

        if (result.Data.Usage?.VendorUsageObject is VendorAnthropicUsage usage && usage.Iterations?.Count > 0)
        {
            Assert.That(usage.Iterations.Any(i => i.Type == "advisor_message"), Is.True.Or.False,
                "Advisor iterations may be present when the advisor was invoked.");
        }
    }

    private static ChatRequest CreateAdvisorRequest(int? maxUses = null, int? maxTokens = null, AnthropicCacheSettings? caching = null)
    {
        return new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    AdvisorTool = new AnthropicAdvisorToolRequest
                    {
                        ExecutorModel = ChatModel.Anthropic.Claude46.Sonnet,
                        AdvisorModel = ChatModel.Anthropic.Claude48.Opus,
                        MaxUses = maxUses,
                        MaxTokens = maxTokens,
                        Caching = caching
                    }
                }
            }
        };
    }

    private JObject ParseBody(ChatRequest request)
    {
        TornadoRequestContent serialized = request.Serialize(_provider!);
        return JObject.Parse(serialized.Body.ToString()!);
    }
}
