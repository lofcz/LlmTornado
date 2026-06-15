using System.Net;
using LlmTornado.Batch;
using LlmTornado.Batch.Vendors.OpenAi;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

[TestFixture]
public class PromptCacheRetentionTests
{
    private const string CachePrefixLine =
        "Reference material for prompt-cache integration tests. This line is repeated to exceed the minimum cacheable prefix length. ";

    private static string BuildCacheablePrefix(int repeatCount = 180) =>
        string.Concat(Enumerable.Repeat(CachePrefixLine, repeatCount));

    private static TornadoApi CreateOpenAiApi()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return new TornadoApi(LLmProviders.OpenAi, apiKey);
        }

        if (Program.ApiKeys?.OpenAi is { Length: > 0 } demoKey)
        {
            return new TornadoApi(LLmProviders.OpenAi, demoKey);
        }

        return null!;
    }

    private static async Task<TornadoApi> RequireOpenAiApiAsync()
    {
        string? envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(envKey))
        {
            await Program.SetupApi();
        }

        TornadoApi? api = CreateOpenAiApi();
        if (api is null)
        {
            Assert.Ignore("OPENAI_API_KEY or Demo apiKey.json OpenAi key required for integration tests.");
        }

        return api;
    }

    [Test]
    public void Serialize_ChatCompletions_EmitsPromptCacheRetentionValues()
    {
        TornadoApi api = new TornadoApi("test-key");
        IEndpointProvider provider = api.GetProvider(LLmProviders.OpenAi);

        ChatRequest inMemoryRequest = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Messages = [new ChatMessage(ChatMessageRoles.User, "hi")],
            PromptCacheRetention = PromptCacheRetention.InMemory
        };

        ChatRequest extendedRequest = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Messages = [new ChatMessage(ChatMessageRoles.User, "hi")],
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours
        };

        string inMemoryJson = inMemoryRequest.Serialize(provider).Body?.ToString() ?? string.Empty;
        string extendedJson = extendedRequest.Serialize(provider).Body?.ToString() ?? string.Empty;

        Assert.That(inMemoryJson, Does.Contain("\"prompt_cache_retention\":\"in_memory\""));
        Assert.That(extendedJson, Does.Contain("\"prompt_cache_retention\":\"24h\""));
    }

    [Test]
    public void Serialize_Responses_EmitsPromptCacheRetentionValues()
    {
        TornadoApi api = new TornadoApi("test-key");
        IEndpointProvider provider = api.GetProvider(LLmProviders.OpenAi);

        ResponseRequest request = new ResponseRequest(ChatModel.OpenAi.Gpt54.V54, "hello")
        {
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours
        };

        string body = request.Serialize(provider).Body?.ToString() ?? string.Empty;
        Assert.That(body, Does.Contain("\"prompt_cache_retention\":\"24h\""));
    }

    [Test]
    public void Serialize_BatchJsonl_EmitsPromptCacheRetention()
    {
        TornadoApi api = new TornadoApi("test-key");
        IEndpointProvider provider = api.GetProvider(LLmProviders.OpenAi);

        BatchRequest batch = new BatchRequest([
            new BatchRequestItem("item-1", new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt54.V54,
                Messages = [new ChatMessage(ChatMessageRoles.User, "batch hello")],
                PromptCacheKey = "batch-cache-key",
                PromptCacheRetention = PromptCacheRetention.TwentyFourHours
            })
        ]);

        string jsonl = VendorOpenAiBatchRequest.SerializeToJsonl(batch, provider);
        JObject line = JObject.Parse(jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]);

        Assert.That(line["url"]?.Value<string>(), Is.EqualTo("/v1/chat/completions"));
        Assert.That(line["body"]?["prompt_cache_retention"]?.Value<string>(), Is.EqualTo("24h"));
        Assert.That(line["body"]?["prompt_cache_key"]?.Value<string>(), Is.EqualTo("batch-cache-key"));
    }

    [Test]
    public void Serialize_Gpt55_CoercesInMemoryToTwentyFourHours()
    {
        TornadoApi api = new TornadoApi("test-key");
        IEndpointProvider provider = api.GetProvider(LLmProviders.OpenAi);

        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            Messages = [new ChatMessage(ChatMessageRoles.User, "hi")],
            PromptCacheRetention = PromptCacheRetention.InMemory
        };

        string body = request.Serialize(provider).Body?.ToString() ?? string.Empty;
        Assert.That(body, Does.Contain("\"prompt_cache_retention\":\"24h\""));
        Assert.That(body, Does.Not.Contain("\"prompt_cache_retention\":\"in_memory\""));
    }

    [Test]
    public void ChatRequest_CopyConstructor_CopiesPromptCacheRetention()
    {
        ChatRequest source = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours,
            PromptCacheKey = "copy-key"
        };

        ChatRequest copy = new ChatRequest(source);
        Assert.That(copy.PromptCacheRetention, Is.EqualTo(PromptCacheRetention.TwentyFourHours));
        Assert.That(copy.PromptCacheKey, Is.EqualTo("copy-key"));
    }

    [Test]
    [Category("Integration")]
    [Explicit("Calls OpenAI production APIs")]
    public async Task Integration_ExtendedCaching_ChatCompletions_SecondRequestUsesCache()
    {
        TornadoApi api = await RequireOpenAiApiAsync();

        string prefix = BuildCacheablePrefix();
        string cacheKey = $"llmtornado-cache-test-{Guid.NewGuid():N}";

        ChatRequest warmRequest = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            ReasoningEffort = ChatReasoningEfforts.None,
            PromptCacheKey = cacheKey,
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, prefix),
                new ChatMessage(ChatMessageRoles.User, "Reply with exactly: warm")
            ],
            MaxTokens = 16
        };

        ChatRequest hitRequest = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            ReasoningEffort = ChatReasoningEfforts.None,
            PromptCacheKey = cacheKey,
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, prefix),
                new ChatMessage(ChatMessageRoles.User, "Reply with exactly: hit")
            ],
            MaxTokens = 16
        };

        HttpCallResult<ChatResult> warm = await api.Chat.CreateChatCompletionSafe(warmRequest);
        Assert.That(warm.Ok, Is.True, warm.Response);

        HttpCallResult<ChatResult> hit = await api.Chat.CreateChatCompletionSafe(hitRequest);
        Assert.That(hit.Ok, Is.True, hit.Response);

        int? cachedTokens = hit.Data?.Usage?.PromptTokenDetails?.CachedTokens;
        Assert.That(cachedTokens, Is.GreaterThan(0), "Expected cached_tokens on the second request with the same prefix and prompt_cache_key.");
    }

    [Test]
    [Category("Integration")]
    [Explicit("Calls OpenAI production APIs")]
    public async Task Integration_ExtendedCaching_Responses_SecondRequestUsesCache()
    {
        TornadoApi api = await RequireOpenAiApiAsync();

        string prefix = BuildCacheablePrefix();
        string cacheKey = $"llmtornado-responses-cache-{Guid.NewGuid():N}";

        ResponseRequest warmRequest = new ResponseRequest(ChatModel.OpenAi.Gpt54.V54, prefix + "\n\nReply with exactly: warm")
        {
            Reasoning = new ReasoningConfiguration { Effort = ResponseReasoningEfforts.None },
            PromptCacheKey = cacheKey,
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours,
            MaxOutputTokens = 16
        };

        ResponseRequest hitRequest = new ResponseRequest(ChatModel.OpenAi.Gpt54.V54, prefix + "\n\nReply with exactly: hit")
        {
            Reasoning = new ReasoningConfiguration { Effort = ResponseReasoningEfforts.None },
            PromptCacheKey = cacheKey,
            PromptCacheRetention = PromptCacheRetention.TwentyFourHours,
            MaxOutputTokens = 16
        };

        HttpCallResult<ResponseResult> warm = await api.Responses.CreateResponseSafe(warmRequest);
        Assert.That(warm.Ok, Is.True, warm.Response);

        HttpCallResult<ResponseResult> hit = await api.Responses.CreateResponseSafe(hitRequest);
        Assert.That(hit.Ok, Is.True, hit.Response);

        int cachedTokens = hit.Data?.Usage?.InputTokenDetails?.CachedTokens ?? 0;
        Assert.That(cachedTokens, Is.GreaterThan(0), "Expected cached_tokens on the second Responses request.");
    }

    [Test]
    [Category("Integration")]
    [Explicit("Calls OpenAI production APIs")]
    public async Task Integration_Gpt55_RejectsInMemoryWhenSentExplicitly()
    {
        TornadoApi api = await RequireOpenAiApiAsync();

        HttpCallResult<ChatResult> result = await api.Chat.CreateChatCompletionSafe(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt55.V55,
            ReasoningEffort = ChatReasoningEfforts.None,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Say hi.")],
            MaxTokens = 8,
            OnSerialize = (j, _) => j["prompt_cache_retention"] = "in_memory"
        });

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Code, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(result.Response, Does.Contain("prompt_cache_retention").Or.Contain("in_memory"));
    }
}
