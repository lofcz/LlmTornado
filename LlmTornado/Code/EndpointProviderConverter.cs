using LlmTornado.Code.Vendor;

namespace LlmTornado.Code;

internal static class EndpointProviderConverter
{
    public static IEndpointProvider CreateProvider(LLmProviders provider, TornadoApi api)
    {
        return provider switch
        {
            LLmProviders.OpenAi => new OpenAiEndpointProvider(api),
            LLmProviders.Anthropic => new AnthropicEndpointProvider(api),
            LLmProviders.Cohere => new CohereEndpointProvider(api),
            LLmProviders.Google => new GoogleEndpointProvider(api),
            LLmProviders.DeepSeek => new OpenAiEndpointProvider(api, LLmProviders.DeepSeek)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.deepseek.com/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepSeek))}{url}"
            },
            LLmProviders.Mistral => new OpenAiEndpointProvider(api, LLmProviders.Mistral)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.mistral.ai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.DeepSeek))}{url}"
            },
            LLmProviders.Groq => new OpenAiEndpointProvider(api, LLmProviders.Groq)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.groq.com/openai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Groq))}{url}"
            },
            LLmProviders.XAi => new OpenAiEndpointProvider(api, LLmProviders.XAi)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.x.ai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Groq))}{url}"
            },
            LLmProviders.Perplexity => new OpenAiEndpointProvider(api, LLmProviders.Perplexity)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.perplexity.ai/{0}", OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint, LLmProviders.Groq))}{url}"
            },
            _ => new OpenAiEndpointProvider(api)
        };
    }
}