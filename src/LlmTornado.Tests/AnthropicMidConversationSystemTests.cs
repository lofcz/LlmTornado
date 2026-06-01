using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Tokenize;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Serialization and integration tests for Anthropic mid-conversation system messages (May 2026).
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicMidConversationSystemTests
{
    private IEndpointProvider? _provider;

    [SetUp]
    public void Setup()
    {
        _provider = new TornadoApi("test-key").GetProvider(LLmProviders.Anthropic);
    }

    [Test]
    public void InitialSystemMessage_SerializesToTopLevelSystemField()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, "You are a code review assistant. Be concise."),
                new ChatMessage(ChatMessageRoles.User, "Review process() in utils.py for performance issues.")
            ]
        };

        JObject body = SerializeChatRequest(request);

        Assert.That(body["system"]?.Type, Is.EqualTo(JTokenType.Array));
        Assert.That(body["system"]?[0]?["type"]?.ToString(), Is.EqualTo("text"));
        Assert.That(body["system"]?[0]?["text"]?.ToString(), Is.EqualTo("You are a code review assistant. Be concise."));
        Assert.That(body["messages"]?.Count(), Is.EqualTo(1));
        Assert.That(body["messages"]?[0]?["role"]?.ToString(), Is.EqualTo("user"));
    }

    [Test]
    public void MidConversationSystemMessage_SerializesInMessagesArray()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, "You are a code review assistant. Be concise."),
                new ChatMessage(ChatMessageRoles.User, "Review process() in utils.py for performance issues."),
                new ChatMessage(ChatMessageRoles.Assistant, "The list comprehension is fine for small inputs."),
                new ChatMessage(ChatMessageRoles.User, "Now review the calling code that invokes process()."),
                new ChatMessage(ChatMessageRoles.System, "From now on, every suggestion must include explicit type annotations.")
            ]
        };

        JObject body = SerializeChatRequest(request);

        Assert.That(body["system"]?[0]?["text"]?.ToString(), Is.EqualTo("You are a code review assistant. Be concise."));
        Assert.That(body["messages"]?.Count(), Is.EqualTo(4));

        JToken? lastMessage = body["messages"]?.Last;
        Assert.That(lastMessage?["role"]?.ToString(), Is.EqualTo("system"));
        Assert.That(lastMessage?["content"]?[0]?["type"]?.ToString(), Is.EqualTo("text"));
        Assert.That(lastMessage?["content"]?[0]?["text"]?.ToString(),
            Is.EqualTo("From now on, every suggestion must include explicit type annotations."));
    }

    [Test]
    public void MidConversationSystemMessage_WithoutInitialSystem_UsesMessagesArrayOnly()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.User, "Hello"),
                new ChatMessage(ChatMessageRoles.Assistant, "Hi there."),
                new ChatMessage(ChatMessageRoles.User, "Switch to formal tone."),
                new ChatMessage(ChatMessageRoles.System, "Respond formally from this point onward.")
            ]
        };

        JObject body = SerializeChatRequest(request);

        Assert.That(body["system"], Is.Null);
        Assert.That(body["messages"]?.Last?["role"]?.ToString(), Is.EqualTo("system"));
        Assert.That(body["messages"]?.Last?["content"]?[0]?["text"]?.ToString(),
            Is.EqualTo("Respond formally from this point onward."));
    }

    [Test]
    public void TokenizeRequest_MidConversationSystemMessage_SerializesInMessagesArray()
    {
        TokenizeRequest request = new TokenizeRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, "Base instructions."),
                new ChatMessage(ChatMessageRoles.User, "First question."),
                new ChatMessage(ChatMessageRoles.System, "Additional constraint.")
            ]
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["system"]?[0]?["text"]?.ToString(), Is.EqualTo("Base instructions."));
        Assert.That(body["messages"]?.Count(), Is.EqualTo(2));
        Assert.That(body["messages"]?.Last?["role"]?.ToString(), Is.EqualTo("system"));
        Assert.That(body["messages"]?.Last?["content"]?[0]?["text"]?.ToString(), Is.EqualTo("Additional constraint."));
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real API calls")]
    public async Task MidConversationSystemMessage_ReturnsResponse()
    {
        TornadoApi? api = await CreateAnthropicApi();
        if (api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        ChatResult result = await api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            MaxTokens = 256,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant. Be concise."),
                new ChatMessage(ChatMessageRoles.User, "What is 2+2?"),
                new ChatMessage(ChatMessageRoles.Assistant, "4."),
                new ChatMessage(ChatMessageRoles.User, "What is 3+3?"),
                new ChatMessage(ChatMessageRoles.System, "End every reply with the exact phrase: mid-system-ok")
            ]
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? "Request failed");
        Assert.That(result.Choices, Is.Not.Null.And.Not.Empty);

        string? content = result.Choices![0].Message?.Content;
        Assert.That(content, Is.Not.Null.And.Not.Empty);
        Assert.That(content, Does.Contain("mid-system-ok").IgnoreCase);
        TestContext.WriteLine($"Anthropic mid-conversation system response: {content}");
    }

    private JObject SerializeChatRequest(ChatRequest request)
    {
        TornadoRequestContent serialized = request.Serialize(_provider!);
        return JObject.Parse(serialized.Body.ToString()!);
    }

    private static async Task<TornadoApi?> CreateAnthropicApi()
    {
        string? envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return new TornadoApi(LLmProviders.Anthropic, envKey);
        }

        if (await Program.SetupApi() && !string.IsNullOrWhiteSpace(Program.ApiKeys.Anthropic))
        {
            return new TornadoApi(LLmProviders.Anthropic, Program.ApiKeys.Anthropic);
        }

        return null;
    }
}
