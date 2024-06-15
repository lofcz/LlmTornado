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
            _ => new OpenAiEndpointProvider(api)
        };
    }
}