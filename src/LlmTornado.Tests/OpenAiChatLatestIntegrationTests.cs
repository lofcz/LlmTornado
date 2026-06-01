using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Responses;

namespace LlmTornado.Tests;

/// <summary>
/// Integration tests for OpenAI chat-latest family model slugs against production API.
/// </summary>
[TestFixture]
[Category("Integration")]
public class OpenAiChatLatestIntegrationTests
{
    private TornadoApi? _api;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        string? envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _api = new TornadoApi(LLmProviders.OpenAi, envKey);
            return;
        }

        if (await Program.SetupApi())
        {
            _api = Program.Connect();
        }
    }

    private static IEnumerable<ChatModel> ChatLatestModels()
    {
        yield return ChatModel.OpenAi.ChatLatest;
        yield return ChatModel.OpenAi.Gpt53.V53ChatLatest;
        yield return ChatModel.OpenAi.Gpt52.V52ChatLatest;
    }

    [Test]
    [TestCaseSource(nameof(ChatLatestModels))]
    [Explicit("Requires OpenAI API key and makes real production API calls")]
    public async Task ChatCompletions_ReturnsResponse(ChatModel model)
    {
        if (_api is null)
        {
            Assert.Ignore("OpenAI API key not configured. Set OPENAI_API_KEY or provide apiKey.json.");
        }

        ChatResult result = await _api.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = model,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: pong")]
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? "Request failed");
        Assert.That(result.Choices, Is.Not.Null.And.Not.Empty);

        string? content = result.Choices![0].Message?.Content;
        Assert.That(content, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"{model.Name} chat completion: {content}");
    }

    [Test]
    [TestCaseSource(nameof(ChatLatestModels))]
    [Explicit("Requires OpenAI API key and makes real production API calls")]
    public async Task Responses_ReturnsResponse(ChatModel model)
    {
        if (_api is null)
        {
            Assert.Ignore("OpenAI API key not configured. Set OPENAI_API_KEY or provide apiKey.json.");
        }

        ResponseResult result = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = model,
            Instructions = "You are a helpful assistant.",
            InputItems = [new ResponseInputMessage(ChatMessageRoles.User, "Reply with exactly: pong")]
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Output, Is.Not.Null.And.Not.Empty);

        ResponseOutputMessageItem? message = result.Output!.OfType<ResponseOutputMessageItem>().FirstOrDefault();
        Assert.That(message, Is.Not.Null);

        ResponseOutputTextContent? text = message!.Content.OfType<ResponseOutputTextContent>().FirstOrDefault();
        Assert.That(text?.Text, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"{model.Name} response: {text!.Text}");
    }

    [Test]
    public void ModelConstants_AreRegistered()
    {
        Assert.That(ChatModel.OpenAi.OwnsModel("chat-latest"), Is.True);
        Assert.That(ChatModel.OpenAi.OwnsModel("gpt-5.3-chat-latest"), Is.True);
        Assert.That(ChatModel.OpenAi.OwnsModel("gpt-5.2-chat-latest"), Is.True);

        Assert.That(ChatModel.OpenAi.ChatLatest.Name, Is.EqualTo("chat-latest"));
        Assert.That(ChatModel.OpenAi.Gpt53.V53ChatLatest.Name, Is.EqualTo("gpt-5.3-chat-latest"));
        Assert.That(ChatModel.OpenAi.Gpt52.V52ChatLatest.Name, Is.EqualTo("gpt-5.2-chat-latest"));

        Assert.That(ChatModel.OpenAi.ChatLatest.EndpointCapabilities,
            Does.Contain(ChatModelEndpointCapabilities.Chat).And.Contain(ChatModelEndpointCapabilities.Responses));
        Assert.That(ChatModel.OpenAi.Gpt53.V53ChatLatest.EndpointCapabilities,
            Does.Contain(ChatModelEndpointCapabilities.Chat).And.Contain(ChatModelEndpointCapabilities.Responses));
    }
}
