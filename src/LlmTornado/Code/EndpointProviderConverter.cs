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
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.deepseek.com/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepSeek))}{url}"
            },
            LLmProviders.Mistral => new OpenAiEndpointProvider(LLmProviders.Mistral)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.mistral.ai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepSeek))}{url}"
            },
            LLmProviders.Groq => new OpenAiEndpointProvider(LLmProviders.Groq)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.groq.com/openai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Groq))}{url}"
            },
            LLmProviders.XAi => new OpenAiEndpointProvider(LLmProviders.XAi)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.x.ai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.XAi))}{url}"
            },
            LLmProviders.Perplexity => new OpenAiEndpointProvider(LLmProviders.Perplexity)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.perplexity.ai/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Perplexity))}{url}"
            },
            LLmProviders.Voyage => new OpenAiEndpointProvider(LLmProviders.Voyage)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.voyageai.com/v1/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Voyage))}{url}"
            },
            LLmProviders.DeepInfra => new OpenAiEndpointProvider(LLmProviders.DeepInfra)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.deepinfra.com/v1/openai/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepInfra))}{url}"
            },
            LLmProviders.OpenRouter => new OpenAiEndpointProvider(LLmProviders.OpenRouter)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://openrouter.ai/api/v1/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.OpenRouter))}{url}"
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