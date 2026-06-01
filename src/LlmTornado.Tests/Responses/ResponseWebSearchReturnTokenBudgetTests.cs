using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LlmTornado.Tests.Responses;

[TestFixture]
public class ResponseWebSearchReturnTokenBudgetTests
{
    private static string? ResolveOpenAiApiKey()
    {
        string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        string path = Path.Combine(AppContext.BaseDirectory, "apiKey.json");
        if (!File.Exists(path))
        {
            return null;
        }

        JObject keys = JObject.Parse(File.ReadAllText(path));
        return keys["OpenAi"]?.ToString();
    }

    [Test]
    public void Serialize_WebSearchTool_IncludesReturnTokenBudget()
    {
        var tool = new ResponseWebSearchTool
        {
            WebSearchToolType = ResponseWebSearchToolType.WebSearch,
            ReturnTokenBudget = ResponseWebSearchReturnTokenBudget.Unlimited
        };

        string json = JsonConvert.SerializeObject(tool);
        JObject jo = JObject.Parse(json);

        Assert.That(jo["type"]?.ToString(), Is.EqualTo("web_search"));
        Assert.That(jo["return_token_budget"]?.ToString(), Is.EqualTo("unlimited"));
    }

    [Test]
    public void Deserialize_WebSearchTool_RoundTripsReturnTokenBudget()
    {
        const string json = """
            {
              "type": "web_search",
              "return_token_budget": "default",
              "search_context_size": "low"
            }
            """;

        ResponseWebSearchTool? tool = JsonConvert.DeserializeObject<ResponseWebSearchTool>(json);

        Assert.That(tool, Is.Not.Null);
        Assert.That(tool!.WebSearchToolType, Is.EqualTo(ResponseWebSearchToolType.WebSearch));
        Assert.That(tool.ReturnTokenBudget, Is.EqualTo(ResponseWebSearchReturnTokenBudget.Default));
        Assert.That(tool.SearchContextSize, Is.EqualTo(ResponseSearchContextSize.Low));
    }

    [Test]
    [Category("Integration")]
    public async Task CreateResponse_WithWebSearchDefaultReturnTokenBudget_Succeeds()
    {
        await RunWebSearchIntegrationAsync(ResponseWebSearchReturnTokenBudget.Default);
    }

    [Test]
    [Category("Integration")]
    public async Task CreateResponse_WithWebSearchUnlimitedReturnTokenBudget_Succeeds()
    {
        await RunWebSearchIntegrationAsync(ResponseWebSearchReturnTokenBudget.Unlimited);
    }

    [Test]
    [Category("Integration")]
    public async Task CreateResponse_ReturnTokenBudgetOnPreviewTool_IsRejected()
    {
        string? apiKey = ResolveOpenAiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY or apiKey.json OpenAi key required for integration tests.");
        }

        TornadoApi api = new TornadoApi(LLmProviders.OpenAi, apiKey);
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Low),
            ToolChoice = OutboundToolChoice.Hosted(HostedToolTypes.WebSearchPreview),
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "What is the capital of France? Reply in one short sentence.")
            ],
            Tools =
            [
                new ResponseWebSearchTool
                {
                    WebSearchToolType = ResponseWebSearchToolType.WebSearchPreview,
                    ReturnTokenBudget = ResponseWebSearchReturnTokenBudget.Unlimited
                }
            ],
            MaxOutputTokens = 512
        };

        HttpCallResult<ResponseResult> result = await api.Responses.CreateResponseSafe(request);

        Assert.That(result.Ok, Is.False, result.Exception?.Message ?? result.ResponseError?.Error?.Message);
    }

    private static async Task RunWebSearchIntegrationAsync(ResponseWebSearchReturnTokenBudget budget)
    {
        string? apiKey = ResolveOpenAiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY or apiKey.json OpenAi key required for integration tests.");
        }

        TornadoApi api = new TornadoApi(LLmProviders.OpenAi, apiKey);
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Low),
            ToolChoice = OutboundToolChoice.Hosted(HostedToolTypes.WebSearch),
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "What is the capital of France? Reply in one short sentence with a citation.")
            ],
            Tools =
            [
                new ResponseWebSearchTool
                {
                    WebSearchToolType = ResponseWebSearchToolType.WebSearch,
                    ReturnTokenBudget = budget
                }
            ],
            MaxOutputTokens = 1024
        };

        HttpCallResult<ResponseResult> result = await api.Responses.CreateResponseSafe(request);

        Assert.That(result.Ok, Is.True, result.Exception?.Message ?? result.ResponseError?.Error?.Message);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Status, Is.EqualTo("completed").Or.EqualTo("incomplete"));

        bool hasWebSearchCall = result.Data.Output?.Any(item => item is ResponseWebSearchToolCallItem) == true;
        Assert.That(hasWebSearchCall, Is.True, "Expected a web_search_call output item.");
    }
}
