using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
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
}
