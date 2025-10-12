using LlmTornado.Code.Vendor;

namespace LlmTornado.Code;

internal static class EndpointProviderConverter
{
    public static IEndpointProvider CreateProvider(LLmProviders provider, TornadoApi api)
    {
        IEndpointProvider createdProvider = provider switch
        {
            LLmProviders.OpenAi => new OpenAiEndpointProvider(),
            LLmProviders.Anthropic => new AnthropicEndpointProvider(),
            LLmProviders.Cohere => new CohereEndpointProvider(),
            LLmProviders.Google => new GoogleEndpointProvider(),
            LLmProviders.DeepSeek => new OpenAiEndpointProvider(LLmProviders.DeepSeek)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.deepseek.com/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepSeek))}{url}"
            },
            LLmProviders.Mistral => new OpenAiEndpointProvider(LLmProviders.Mistral)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.mistral.ai/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Mistral))}{url}"
            },
            LLmProviders.Groq => new OpenAiEndpointProvider(LLmProviders.Groq)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.groq.com/openai/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Groq))}{url}"
            },
            LLmProviders.XAi => new OpenAiEndpointProvider(LLmProviders.XAi)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.x.ai/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.XAi))}{url}"
            },
            LLmProviders.Perplexity => new OpenAiEndpointProvider(LLmProviders.Perplexity)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.perplexity.ai/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Perplexity))}{url}"
            },
            LLmProviders.Zai => new OpenAiEndpointProvider(LLmProviders.Zai)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.z.ai/api/paas/v4/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Zai))}{url}"
            },
            LLmProviders.Voyage => new OpenAiEndpointProvider(LLmProviders.Voyage)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.voyageai.com/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Voyage))}{url}"
            },
            LLmProviders.DeepInfra => new OpenAiEndpointProvider(LLmProviders.DeepInfra)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.deepinfra.com/{0}/openai/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepInfra))}{url}"
            },
            LLmProviders.OpenRouter => new OpenAiEndpointProvider(LLmProviders.OpenRouter)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://openrouter.ai/api/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.OpenRouter))}{url}"
            },
            LLmProviders.MoonshotAi => new OpenAiEndpointProvider(LLmProviders.MoonshotAi)
            {
                UrlResolver = (endpoint, url, ctx) => $"{string.Format(api.ApiUrlFormat ?? "https://api.moonshot.ai/{0}/{1}", api.ResolveApiVersion(), OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.MoonshotAi))}{url}"
            },
            _ => new OpenAiEndpointProvider()
        };

        createdProvider.Api = api;
        
        if (api.Authentications.TryGetValue(provider, out ProviderAuthentication? auth))
        {
            createdProvider.Auth = auth;
        }

        return createdProvider;
    }
}