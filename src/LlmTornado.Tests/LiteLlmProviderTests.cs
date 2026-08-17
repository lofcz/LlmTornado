using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Models.Vendors;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit tests for the LiteLLM AI gateway provider (OpenAI-compatible proxy).
/// </summary>
[TestFixture]
public class LiteLlmProviderTests
{
    [Test]
    public void LiteLlm_ResolvesOpenAiCompatibleProvider()
    {
        TornadoApi api = new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")]);
        IEndpointProvider provider = api.GetProvider(LLmProviders.LiteLlm);
        Assert.That(provider.Provider, Is.EqualTo(LLmProviders.LiteLlm));
    }

    [Test]
    public void LiteLlm_DefaultsToLocalProxyEndpoint()
    {
        TornadoApi api = new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")]);
        IEndpointProvider provider = api.GetProvider(LLmProviders.LiteLlm);

        string url = provider.ApiUrl(CapabilityEndpoints.Chat, null, new ChatModel("gpt-4o", LLmProviders.LiteLlm));

        Assert.That(url, Does.StartWith("http://localhost:4000/"));
        Assert.That(url, Does.Contain("chat/completions"));
    }

    [Test]
    public void LiteLlm_HonorsBaseUrlOverride()
    {
        TornadoApi api = new TornadoApi([
            new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test") { BaseUrl = "https://proxy.example.com" }
        ]);
        IEndpointProvider provider = api.GetProvider(LLmProviders.LiteLlm);

        string url = provider.ApiUrl(CapabilityEndpoints.Chat, null, new ChatModel("gpt-4o", LLmProviders.LiteLlm));

        Assert.That(url, Does.StartWith("https://proxy.example.com/"));
        Assert.That(url, Does.Contain("chat/completions"));
    }

    [Test]
    public void LiteLlm_HonorsApiUrlFormatOverride()
    {
        TornadoApi api = new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")])
        {
            ApiUrlFormat = "https://gateway.internal/{0}/{1}"
        };
        IEndpointProvider provider = api.GetProvider(LLmProviders.LiteLlm);

        string url = provider.ApiUrl(CapabilityEndpoints.Chat, null, new ChatModel("gpt-4o", LLmProviders.LiteLlm));

        Assert.That(url, Does.StartWith("https://gateway.internal/"));
    }

    [Test]
    public void LiteLlm_SerializesOpenAiCompatibleChatRequest()
    {
        IEndpointProvider provider =
            new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")])
                .GetProvider(LLmProviders.LiteLlm);

        ChatRequest request = new ChatRequest
        {
            Model = new ChatModel("claude-3-5-sonnet", LLmProviders.LiteLlm),
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
        };

        TornadoRequestContent serialized = request.Serialize(provider);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        // LiteLLM is OpenAI-compatible: the body must carry the proxy model alias
        // and OpenAI-shaped messages, so any model the proxy routes works verbatim.
        Assert.That(body["model"]?.ToString(), Is.EqualTo("claude-3-5-sonnet"));
        Assert.That(body["messages"], Is.Not.Null);
        Assert.That(body["messages"]!.First?["role"]?.ToString(), Is.EqualTo("user"));
        Assert.That(body["messages"]!.First?["content"]?.ToString(), Is.EqualTo("Hello"));
    }

    [Test]
    public void LiteLlm_SerializesNonEmptyEmbeddingsBody()
    {
        IEndpointProvider provider =
            new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")])
                .GetProvider(LLmProviders.LiteLlm);

        EmbeddingModel model = "text-embedding-3-small";
        EmbeddingRequest request = new EmbeddingRequest(model, "hello world");

        TornadoRequestContent serialized = request.Serialize(provider);

        // Regression: LiteLLM used to fall through to string.Empty in the embeddings
        // serializer, sending an empty body. It must now emit a valid OpenAI payload.
        string bodyStr = serialized.Body.ToString()!;
        Assert.That(bodyStr, Is.Not.Empty);
        JObject body = JObject.Parse(bodyStr);
        Assert.That(body["model"]?.ToString(), Is.EqualTo("text-embedding-3-small"));
        Assert.That(body["input"], Is.Not.Null);
    }

    [Test]
    public void LiteLlm_ModelsEndpointResolvesToProxyModelsUrl()
    {
        TornadoApi api = new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")]);
        IEndpointProvider provider = api.GetProvider(LLmProviders.LiteLlm);

        string url = provider.ApiUrl(CapabilityEndpoints.Models, null);

        // Model discovery reuses the generic ModelsEndpoint (GET /v1/models).
        Assert.That(url, Does.StartWith("http://localhost:4000/"));
        Assert.That(url, Does.Contain("models"));
    }

    [Test]
    public void LiteLlm_ParsesOpenAiCompatibleModelsDiscoveryResponse()
    {
        IEndpointProvider provider =
            new TornadoApi([new ProviderAuthentication(LLmProviders.LiteLlm, "sk-test")])
                .GetProvider(LLmProviders.LiteLlm);

        // The LiteLLM proxy returns the OpenAI-compatible model list shape.
        string sampleJson =
            @"{ ""object"": ""list"", ""data"": [ { ""id"": ""gpt-4o-mini"", ""object"": ""model"", ""owned_by"": ""openai"" }, { ""id"": ""claude-sonnet-4-6"", ""object"": ""model"", ""owned_by"": ""anthropic"" } ] }";

        RetrievedModelsResult? result = provider.InboundMessage<RetrievedModelsResult>(sampleJson, null, null);

        Assert.That(result?.Data, Is.Not.Null);
        Assert.That(result!.Data, Has.Count.EqualTo(2));
        Assert.That(result.Data[0].Id, Is.EqualTo("gpt-4o-mini"));
        Assert.That(result.Data[1].Id, Is.EqualTo("claude-sonnet-4-6"));
    }
}
