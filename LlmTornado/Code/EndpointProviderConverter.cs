using LlmTornado.Code.Vendor;

namespace LlmTornado.Code;

internal static class EndpointProviderConverter
{
    public static IEndpointProvider CreateProvider(LLmProviders provider, OpenAiApi api)
    {
        return provider switch
        {
            LLmProviders.OpenAi => new OpenAiEndpointProvider(api),
            LLmProviders.Anthropic => new AnthropicEndpointProvider(api),
            _ => new OpenAiEndpointProvider(api)
        };
    }
}