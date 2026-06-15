using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

[TestFixture]
public class AnthropicThinkingDisplayTests
{
    private static IEndpointProvider Provider =>
        new TornadoApi(LLmProviders.Anthropic, "test-key").GetProvider(LLmProviders.Anthropic);

    [Test]
    public void RequestSerialization_IncludesDisplayOmitted()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages = [new ChatMessage(ChatMessageRoles.User, "What is 27 * 453?")],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    Enabled = true,
                    BudgetTokens = 10_000,
                    Display = AnthropicThinkingDisplay.Omitted
                }
            })
        };

        string json = request.Serialize(Provider).Body.ToString()!;
        JObject payload = JObject.Parse(json);

        Assert.That(payload["thinking"]?["type"]?.Value<string>(), Is.EqualTo("enabled"));
        Assert.That(payload["thinking"]?["budget_tokens"]?.Value<int>(), Is.EqualTo(10_000));
        Assert.That(payload["thinking"]?["display"]?.Value<string>(), Is.EqualTo("omitted"));
    }

    [Test]
    public void ResponseParsing_OmittedThinkingBlock_PreservesSignature()
    {
        const string responseJson = """
        {
          "id": "msg_test",
          "type": "message",
          "role": "assistant",
          "content": [
            {
              "type": "thinking",
              "thinking": "",
              "signature": "sig_omitted_123"
            },
            {
              "type": "text",
              "text": "12231"
            }
          ],
          "model": "claude-sonnet-4-6",
          "stop_reason": "end_turn",
          "usage": {
            "input_tokens": 10,
            "output_tokens": 20
          }
        }
        """;

        VendorAnthropicChatResult vendorResult = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(responseJson)!;
        ChatResult chatResult = vendorResult.ToChatResult(null, null);

        ChatMessage? assistantMessage = chatResult.Choices?
            .Select(x => x.Message)
            .FirstOrDefault(x => x?.Parts?.Any(p => p.Type == ChatMessageTypes.Text) == true);

        Assert.That(assistantMessage, Is.Not.Null);

        ChatMessagePart? reasoningPart = assistantMessage!.Parts?
            .FirstOrDefault(p => p.Type == ChatMessageTypes.Reasoning);

        Assert.That(reasoningPart, Is.Not.Null);
        Assert.That(reasoningPart!.Reasoning?.Content, Is.EqualTo(string.Empty));
        Assert.That(reasoningPart.Reasoning?.Signature, Is.EqualTo("sig_omitted_123"));
        Assert.That(reasoningPart.Reasoning?.IsOmitted, Is.True);
        Assert.That(reasoningPart.Reasoning?.IsRedacted, Is.False);

        ChatMessagePart? textPart = assistantMessage.Parts?
            .FirstOrDefault(p => p.Type == ChatMessageTypes.Text);

        Assert.That(textPart?.Text, Is.EqualTo("12231"));
    }

    [Test]
    public void RoundTrip_OmittedThinkingBlock_SerializesAsThinkingType()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.User, "What is 27 * 453?"),
                new ChatMessage(ChatMessageRoles.Assistant,
                [
                    new ChatMessagePart(ChatMessageTypes.Reasoning)
                    {
                        Reasoning = new ChatMessageReasoningData
                        {
                            Content = string.Empty,
                            Signature = "sig_omitted_123",
                            Provider = LLmProviders.Anthropic
                        }
                    },
                    new ChatMessagePart("12231")
                ])
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    Enabled = true,
                    BudgetTokens = 10_000,
                    Display = AnthropicThinkingDisplay.Omitted
                }
            })
        };

        string json = request.Serialize(Provider).Body.ToString()!;
        JArray assistantContent = (JArray)JObject.Parse(json)["messages"]![1]!["content"]!;

        JObject? thinkingBlock = assistantContent
            .OfType<JObject>()
            .FirstOrDefault(x => x["type"]?.Value<string>() == "thinking");

        Assert.That(thinkingBlock, Is.Not.Null);
        Assert.That(thinkingBlock!["thinking"]?.Value<string>(), Is.EqualTo(string.Empty));
        Assert.That(thinkingBlock["signature"]?.Value<string>(), Is.EqualTo("sig_omitted_123"));
    }
}

[TestFixture]
[Category("Integration")]
public class AnthropicThinkingDisplayIntegrationTests
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

        if (await Program.SetupApi())
        {
            _api = Program.ConnectMulti();
        }
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task CreateChatCompletion_OmittedThinking_ReturnsSignatureWithoutContent()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        HttpCallResult<ChatResult> result = await _api.Chat.CreateChatCompletionSafe(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 4096,
            Messages = [new ChatMessage(ChatMessageRoles.User, "What is 27 * 453? Reply with only the numeric result.")],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    Enabled = true,
                    BudgetTokens = 10_000,
                    Display = AnthropicThinkingDisplay.Omitted
                }
            })
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? result.Response ?? "Request failed");
        Assert.That(result.Data?.Choices, Is.Not.Null.And.Not.Empty);

        ChatMessage? assistantMessage = result.Data!.Choices!
            .Select(x => x.Message)
            .FirstOrDefault(x => x?.Parts?.Any(p => p.Type == ChatMessageTypes.Text) == true);

        Assert.That(assistantMessage, Is.Not.Null);

        ChatMessagePart? reasoningPart = assistantMessage!.Parts?
            .FirstOrDefault(p => p.Type == ChatMessageTypes.Reasoning);

        Assert.That(reasoningPart, Is.Not.Null, "Expected a reasoning block with preserved signature");
        Assert.That(reasoningPart!.Reasoning?.Content, Is.EqualTo(string.Empty));
        Assert.That(reasoningPart.Reasoning?.Signature, Is.Not.Null.And.Not.Empty);
        Assert.That(reasoningPart.Reasoning?.IsOmitted, Is.True);

        string? text = assistantMessage.Parts?
            .FirstOrDefault(p => p.Type == ChatMessageTypes.Text)?.Text;

        Assert.That(text, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"Anthropic omitted thinking response: {text}");
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task StreamChatEnumerable_OmittedThinking_ReturnsSignatureWithoutContent()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Sonnet,
            MaxTokens = 4096,
            Stream = true,
            Messages = [new ChatMessage(ChatMessageRoles.User, "What is 12 + 7? Reply with only the numeric result.")],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    Enabled = true,
                    BudgetTokens = 10_000,
                    Display = AnthropicThinkingDisplay.Omitted
                }
            })
        };

        ChatMessageReasoningData? finalReasoning = null;
        StringBuilder textBuilder = new StringBuilder();

        await foreach (ChatResult chunk in _api.Chat.StreamChatEnumerable(request))
        {
            ChatMessage? delta = chunk.Choices?.FirstOrDefault()?.Delta;
            if (delta?.Parts is null)
            {
                continue;
            }

            foreach (ChatMessagePart part in delta.Parts)
            {
                if (part.Type == ChatMessageTypes.Reasoning && part.Reasoning is not null)
                {
                    finalReasoning = part.Reasoning;
                }
                else if (part.Type == ChatMessageTypes.Text && part.Text is not null)
                {
                    textBuilder.Append(part.Text);
                }
            }
        }

        Assert.That(finalReasoning, Is.Not.Null, "Expected a streamed reasoning block with preserved signature");
        Assert.That(finalReasoning!.Content, Is.EqualTo(string.Empty));
        Assert.That(finalReasoning.Signature, Is.Not.Null.And.Not.Empty);
        Assert.That(finalReasoning.IsOmitted, Is.True);
        Assert.That(textBuilder.ToString(), Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"Anthropic omitted thinking stream response: {textBuilder}");
    }
}
