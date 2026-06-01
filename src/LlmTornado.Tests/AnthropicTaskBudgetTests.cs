using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic task budgets (NextOpus / Claude Opus 4.7+).
/// </summary>
[TestFixture]
public class AnthropicTaskBudgetTests
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
    public void NextOpus_ModelRegistration_Works()
    {
        Assert.That(ChatModel.Anthropic.Claude47.Opus.Name, Is.EqualTo("claude-opus-4-7"));
        Assert.That(ChatModel.Anthropic.Claude47.Opus48.Name, Is.EqualTo("claude-opus-4-8"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-opus-4-7"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-opus-4-8"));
    }

    [Test]
    public void TaskBudget_SerializesToOutputConfig()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus48,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Review the codebase and propose a refactor plan.")],
            MaxTokens = 128_000,
            ReasoningEffort = ChatReasoningEfforts.High,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    TaskBudget = AnthropicTaskBudget.Tokens(64_000)
                }
            }
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["output_config"]?["effort"]?.ToString(), Is.EqualTo("high"));
        Assert.That(body["output_config"]?["task_budget"]?["type"]?.ToString(), Is.EqualTo("tokens"));
        Assert.That(body["output_config"]?["task_budget"]?["total"]?.Value<int>(), Is.EqualTo(64_000));
        Assert.That(body["output_config"]?["task_budget"]?["remaining"], Is.Null);
    }

    [Test]
    public void TaskBudget_SerializesRemaining()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Continue the audit.")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    TaskBudget = AnthropicTaskBudget.Tokens(128_000, 100_000)
                }
            }
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["output_config"]?["task_budget"]?["total"]?.Value<int>(), Is.EqualTo(128_000));
        Assert.That(body["output_config"]?["task_budget"]?["remaining"]?.Value<int>(), Is.EqualTo(100_000));
    }

    [Test]
    public void TaskBudget_DeserializesFromJson()
    {
        const string json = """
            {
              "type": "tokens",
              "total": 64000,
              "remaining": 55000
            }
            """;

        AnthropicTaskBudget? budget = JsonConvert.DeserializeObject<AnthropicTaskBudget>(json);

        Assert.That(budget, Is.Not.Null);
        Assert.That(budget!.Type, Is.EqualTo(AnthropicTaskBudgetTypes.Tokens));
        Assert.That(budget.Total, Is.EqualTo(64_000));
        Assert.That(budget.Remaining, Is.EqualTo(55_000));
    }

    [Test]
    public void TaskBudget_OmitsRemainingWhenNull()
    {
        AnthropicTaskBudget budget = AnthropicTaskBudget.Tokens(64_000);
        string json = JsonConvert.SerializeObject(budget);
        JObject obj = JObject.Parse(json);

        Assert.That(obj["type"]?.ToString(), Is.EqualTo("tokens"));
        Assert.That(obj["total"]?.Value<int>(), Is.EqualTo(64_000));
        Assert.That(obj.ContainsKey("remaining"), Is.False);
    }

    [Test]
    public void TaskBudget_AddsBetaHeader()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus48,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    TaskBudget = AnthropicTaskBudget.Tokens(64_000)
                }
            }
        };

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider();
        HttpRequestMessage message = provider.OutboundMessage("https://api.anthropic.com/v1/messages", HttpMethod.Post, null, false, request);

        Assert.That(message.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values), Is.True);
        string betaHeader = string.Join(",", values!);
        Assert.That(betaHeader, Does.Contain("task-budgets-2026-03-13"));
    }

    [Test]
    public void TaskBudget_DoesNotAddBetaHeaderWhenUnset()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude46.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider();
        HttpRequestMessage message = provider.OutboundMessage("https://api.anthropic.com/v1/messages", HttpMethod.Post, null, false, request);

        Assert.That(message.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values), Is.True);
        string betaHeader = string.Join(",", values!);
        Assert.That(betaHeader, Does.Not.Contain("task-budgets-2026-03-13"));
    }
}

/// <summary>
/// Integration tests for Anthropic task budgets with the real API.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicTaskBudgetIntegrationTests
{
    private TornadoApi? _api;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("ANTHROPIC_API_KEY environment variable not set. Skipping Anthropic task budget integration tests.");
        }

        _api = new TornadoApi(LLmProviders.Anthropic, apiKey);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task NextOpus_TaskBudget_ReturnsResponse()
    {
        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude47.Opus48,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: task-budget-ok")],
            MaxTokens = 256,
            ReasoningEffort = ChatReasoningEfforts.High,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    TaskBudget = AnthropicTaskBudget.Tokens(64_000)
                }
            }
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("task-budget-ok").IgnoreCase);
        Assert.That(result.Usage?.TotalTokens, Is.GreaterThan(0));
    }
}
