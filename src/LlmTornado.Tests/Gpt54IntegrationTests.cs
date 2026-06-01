using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Integration and serialization tests for GPT-5.4 and GPT-5.4-pro (Mar 5, 2026 release).
/// </summary>
[TestFixture]
[Category("Integration")]
public class Gpt54IntegrationTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping GPT-5.4 integration tests.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
        _provider = _api.GetProvider(LLmProviders.OpenAi);
    }

    [Test]
    public void Gpt54_ModelRegistration_Works()
    {
        Assert.That(ChatModel.OpenAi.Gpt54.V54.Name, Is.EqualTo("gpt-5.4"));
        Assert.That(ChatModel.OpenAi.Gpt54.V54Pro.Name, Is.EqualTo("gpt-5.4-pro"));
        Assert.That(ChatModel.OpenAi.Gpt54.V54.ContextTokens, Is.EqualTo(1_050_000));
        Assert.That(ChatModel.OpenAi.Gpt54.V54.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Chat));
        Assert.That(ChatModel.OpenAi.Gpt54.V54.EndpointCapabilities, Does.Contain(ChatModelEndpointCapabilities.Responses));
        Assert.That(ChatModel.OpenAi.Gpt54.V54Pro.EndpointCapabilities, Does.Not.Contain(ChatModelEndpointCapabilities.Chat));
        Assert.That(ChatModel.OpenAi.Gpt54.V54.Aliases, Does.Contain("gpt-5.4-2026-03-05"));
        Assert.That(ChatModel.OpenAi.Gpt54.V54Pro.Aliases, Does.Contain("gpt-5.4-pro-2026-03-05"));
    }

    [Test]
    public void Gpt54_ToolSearchRequest_SerializesCorrectly()
    {
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            InputString = "Find customer orders",
            Tools =
            [
                new ResponseToolNamespace
                {
                    Name = "crm",
                    Description = "CRM tools for customer lookup and order management.",
                    Tools =
                    [
                        new ResponseFunctionTool
                        {
                            Name = "list_open_orders",
                            Description = "List open orders for a customer ID.",
                            DeferLoading = true,
                            Parameters = JObject.Parse("""
                                {
                                  "type": "object",
                                  "properties": { "customer_id": { "type": "string" } },
                                  "required": ["customer_id"],
                                  "additionalProperties": false
                                }
                                """)
                        }
                    ]
                },
                new ResponseToolSearchTool()
            ]
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        JArray tools = (JArray)body["tools"]!;
        Assert.That(tools[0]!["type"]?.ToString(), Is.EqualTo("namespace"));
        Assert.That(tools[0]!["name"]?.ToString(), Is.EqualTo("crm"));
        Assert.That(tools[0]!["tools"]?[0]?["defer_loading"]?.Value<bool>(), Is.True);
        Assert.That(tools[1]!["type"]?.ToString(), Is.EqualTo("tool_search"));
    }

    [Test]
    public void Gpt54_ChatCompletion_ClearsSamplingParamsWhenReasoningIsSet()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningEffort = ChatReasoningEfforts.Medium,
            Temperature = 0.5,
            TopP = 0.9
        };

        TornadoRequestContent serialized = request.Serialize(_provider!);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["temperature"], Is.Null);
        Assert.That(body["top_p"], Is.Null);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt54_ChatCompletion_ReturnsResponse()
    {
        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Reply with exactly: GPT-5.4 OK")],
            ReasoningEffort = ChatReasoningEfforts.None,
            MaxTokens = 32
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Does.Contain("GPT-5.4 OK").IgnoreCase);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt54Pro_ResponsesApi_ReturnsResponse()
    {
        ResponseResult result = await _api!.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54Pro,
            Instructions = "Reply concisely.",
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Reply with exactly: GPT-5.4-pro OK")
            ],
            Reasoning = new ReasoningConfiguration { Effort = ResponseReasoningEfforts.Medium },
            MaxOutputTokens = 64
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Output, Is.Not.Empty);

        ResponseOutputMessageItem? message = result.Output.OfType<ResponseOutputMessageItem>().FirstOrDefault();
        ResponseOutputTextContent? text = message?.Content.OfType<ResponseOutputTextContent>().FirstOrDefault();

        Assert.That(text?.Text, Does.Contain("GPT-5.4-pro OK").IgnoreCase);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt54_ResponsesApi_XHighReasoning_ReturnsResponse()
    {
        ResponseResult result = await _api!.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            InputString = "Reply with exactly: xhigh-ok",
            Reasoning = new ReasoningConfiguration { Effort = ResponseReasoningEfforts.XHigh },
            MaxOutputTokens = 64
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo(ResponseStatuses.Completed).Or.EqualTo(ResponseStatuses.Incomplete));
    }
}
