using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Demo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic adaptive thinking (<c>thinking.type = "adaptive"</c>).
/// </summary>
[TestFixture]
public class AnthropicAdaptiveThinkingTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public async Task Setup()
    {
        _api = new TornadoApi("test-key");
        _provider = _api.GetProvider(LLmProviders.Anthropic);

        if (!await Program.SetupApi())
        {
            return;
        }

        _api = Program.Connect();
        _provider = _api.GetProvider(LLmProviders.Anthropic);
    }

    [Test]
    public void ReasoningBudgetMinusOne_SerializesAdaptiveThinking()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningBudget = -1
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("adaptive"));
        Assert.That(body["thinking"]?["budget_tokens"], Is.Null);
    }

    [Test]
    public void VendorTypeAdaptive_SerializesAdaptiveThinking()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Thinking = new AnthropicThinkingSettings
                    {
                        Type = AnthropicThinkingTypes.Adaptive
                    }
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("adaptive"));
    }

    [Test]
    public void LegacyAdaptiveFlag_SerializesAdaptiveThinking()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Thinking = new AnthropicThinkingSettings { Adaptive = true }
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("adaptive"));
    }

    [Test]
    public void VendorThinkingTypeEnabled_SerializesBudgetTokens()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Thinking = AnthropicThinkingSettings.CreateEnabled(2048)
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("enabled"));
        Assert.That(body["thinking"]?["budget_tokens"]?.Value<int>(), Is.EqualTo(2048));
    }

    [Test]
    public void Opus47EnabledThinking_UpgradesToAdaptive()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningBudget = 4096
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("adaptive"));
        Assert.That(body["thinking"]?["budget_tokens"], Is.Null);
    }

    [Test]
    public void AdaptiveThinkingWithDisplay_SerializesDisplayField()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Thinking = new AnthropicThinkingSettings
                    {
                        Type = AnthropicThinkingTypes.Adaptive,
                        Display = AnthropicThinkingDisplay.Omitted
                    }
                }
            }
        };

        JObject body = ParseBody(request);

        Assert.That(body["thinking"]?["type"]?.ToString(), Is.EqualTo("adaptive"));
        Assert.That(body["thinking"]?["display"]?.ToString(), Is.EqualTo("omitted"));
    }

    [Test]
    public void AdaptiveResponse_ParsesThinkingAndTextBlocks()
    {
        const string json = """
            {
              "id": "msg_adaptive_test",
              "type": "message",
              "role": "assistant",
              "model": "claude-opus-4-6",
              "stop_reason": "end_turn",
              "content": [
                {
                  "type": "thinking",
                  "thinking": "Working through the parity proof.",
                  "signature": "sig_adaptive_123"
                },
                {
                  "type": "text",
                  "text": "The sum of two even numbers is always even."
                }
              ],
              "usage": {
                "input_tokens": 12,
                "output_tokens": 34
              }
            }
            """;

        VendorAnthropicChatResult vendorResult = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(json)!;
        ChatResult result = vendorResult.ToChatResult(json, null);

        ChatMessage? message = result.Choices?.FirstOrDefault()?.Message;
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Parts, Is.Not.Null.And.Count.EqualTo(2));

        ChatMessagePart? reasoningPart = message.Parts!.FirstOrDefault(x => x.Type == ChatMessageTypes.Reasoning);
        ChatMessagePart? textPart = message.Parts!.FirstOrDefault(x => x.Type == ChatMessageTypes.Text);

        Assert.That(reasoningPart?.Reasoning?.Content, Is.EqualTo("Working through the parity proof."));
        Assert.That(reasoningPart?.Reasoning?.Signature, Is.EqualTo("sig_adaptive_123"));
        Assert.That(textPart?.Text, Is.EqualTo("The sum of two even numbers is always even."));
    }

    [Test]
    public void AdaptiveResponseWithOmittedThinking_PreservesSignature()
    {
        const string json = """
            {
              "id": "msg_adaptive_omitted",
              "type": "message",
              "role": "assistant",
              "model": "claude-opus-4-6",
              "stop_reason": "end_turn",
              "content": [
                {
                  "type": "thinking",
                  "thinking": "",
                  "signature": "sig_only"
                },
                {
                  "type": "text",
                  "text": "Done."
                }
              ],
              "usage": {
                "input_tokens": 5,
                "output_tokens": 6
              }
            }
            """;

        VendorAnthropicChatResult vendorResult = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(json)!;
        ChatResult result = vendorResult.ToChatResult(json, null);

        ChatMessagePart? reasoningPart = result.Choices?.FirstOrDefault()?.Message?.Parts?
            .FirstOrDefault(x => x.Type == ChatMessageTypes.Reasoning);

        Assert.That(reasoningPart?.Reasoning?.Content, Is.EqualTo(string.Empty));
        Assert.That(reasoningPart?.Reasoning?.Signature, Is.EqualTo("sig_only"));
    }

    [Test]
    public void AnthropicThinkingSettings_ResolvedTypePrefersExplicitType()
    {
        AnthropicThinkingSettings settings = new AnthropicThinkingSettings
        {
            Type = AnthropicThinkingTypes.Adaptive,
            Enabled = true,
            BudgetTokens = 1024
        };

        Assert.That(settings.ResolvedType, Is.EqualTo(AnthropicThinkingTypes.Adaptive));
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task Integration_AdaptiveThinking_ReturnsResponse()
    {
        if (_api is null || string.IsNullOrWhiteSpace(Program.ApiKeys?.Anthropic))
        {
            Assert.Ignore("Anthropic API key not configured.");
        }

        ChatResult result = await _api.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            MaxTokens = 256,
            ReasoningBudget = -1,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: adaptive-ok")]
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Ok, Is.True, result.Exception?.Message);
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("adaptive-ok").IgnoreCase);
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task Integration_NextOpusAdaptiveThinking_ReturnsResponse()
    {
        if (_api is null || string.IsNullOrWhiteSpace(Program.ApiKeys?.Anthropic))
        {
            Assert.Ignore("Anthropic API key not configured.");
        }

        ChatResult result = await _api.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            MaxTokens = 256,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    Thinking = AnthropicThinkingSettings.CreateAdaptive()
                }
            },
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: next-opus-ok")]
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Ok, Is.True, result.Exception?.Message);
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("next-opus-ok").IgnoreCase);
    }

    private JObject ParseBody(ChatRequest request)
    {
        TornadoRequestContent serialized = request.Serialize(_provider!);
        return JObject.Parse(serialized.Body.ToString()!);
    }
}
