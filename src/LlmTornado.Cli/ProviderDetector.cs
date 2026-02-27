using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli;

/// <summary>
/// Detected provider with its API key and available models.
/// </summary>
internal sealed class DetectedProvider
{
    public required LLmProviders Provider { get; init; }
    public required string ApiKey { get; init; }
    public required List<ChatModel> Models { get; init; }
    public required ChatModel DefaultModel { get; init; }
}

/// <summary>
/// Result of provider detection.
/// </summary>
internal sealed class ProviderDetectionResult
{
    public required TornadoApi Api { get; init; }
    public required List<DetectedProvider> Providers { get; init; }
    public required ChatModel ActiveModel { get; set; }

    public List<ChatModel> AllModels => Providers.SelectMany(p => p.Models).ToList();
}

/// <summary>
/// Auto-detect LLM providers from standard environment variables.
/// </summary>
internal static class ProviderDetector
{
    private static readonly (string EnvVar, LLmProviders Provider)[] ProviderEnvVars =
    [
        ("OPENAI_API_KEY", LLmProviders.OpenAi),
        ("ANTHROPIC_API_KEY", LLmProviders.Anthropic),
        ("GOOGLE_API_KEY", LLmProviders.Google),
        ("GROQ_API_KEY", LLmProviders.Groq),
        ("COHERE_API_KEY", LLmProviders.Cohere),
        ("MISTRAL_API_KEY", LLmProviders.Mistral),
        ("DEEPSEEK_API_KEY", LLmProviders.DeepSeek),
        ("XAI_API_KEY", LLmProviders.XAi),
        ("PERPLEXITY_API_KEY", LLmProviders.Perplexity),
        ("OPENROUTER_API_KEY", LLmProviders.OpenRouter),
        ("DEEPINFRA_API_KEY", LLmProviders.DeepInfra),
        ("VOYAGE_API_KEY", LLmProviders.Voyage),
    ];

    /// <summary>
    /// Default model priority when multiple providers are detected.
    /// </summary>
    private static readonly LLmProviders[] DefaultPriority =
    [
        LLmProviders.Anthropic,
        LLmProviders.OpenAi,
        LLmProviders.Google,
        LLmProviders.XAi,
        LLmProviders.DeepSeek,
        LLmProviders.Groq,
        LLmProviders.Mistral,
    ];

    public static ProviderDetectionResult? Detect()
    {
        List<DetectedProvider> detected = [];

        foreach ((string envVar, LLmProviders provider) in ProviderEnvVars)
        {
            string? key = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            List<ChatModel> models = GetModelsForProvider(provider);
            ChatModel? defaultModel = GetDefaultModel(provider);

            if (defaultModel is null && models.Count > 0)
                defaultModel = models[0];
            if (defaultModel is null)
                continue;

            detected.Add(new DetectedProvider
            {
                Provider = provider,
                ApiKey = key,
                Models = models,
                DefaultModel = defaultModel,
            });
        }

        if (detected.Count == 0)
            return null;

        List<ProviderAuthentication> providerAuths = detected
            .Select(p => new ProviderAuthentication(p.Provider, p.ApiKey))
            .ToList();

        TornadoApi api = new(providerAuths);

        // Pick active model based on priority
        ChatModel activeModel = detected[0].DefaultModel;
        foreach (LLmProviders priority in DefaultPriority)
        {
            DetectedProvider? match = detected.FirstOrDefault(d => d.Provider == priority);
            if (match is not null)
            {
                activeModel = match.DefaultModel;
                break;
            }
        }

        return new ProviderDetectionResult
        {
            Api = api,
            Providers = detected,
            ActiveModel = activeModel,
        };
    }

    private static ChatModel? GetDefaultModel(LLmProviders provider) => provider switch
    {
        LLmProviders.Anthropic => ChatModel.Anthropic.Claude37.Sonnet,
        LLmProviders.OpenAi => ChatModel.OpenAi.Gpt41.V41Mini,
        LLmProviders.Google => ChatModel.Google.Gemini.Gemini25Pro,
        LLmProviders.XAi => ChatModel.XAi.Grok3.V3,
        LLmProviders.DeepSeek => ChatModel.DeepSeek.Models.Chat,
        LLmProviders.Groq => ChatModel.Groq.Meta.Llama370B,
        LLmProviders.Mistral => ChatModel.Mistral.Premier.MistralLarge,
        LLmProviders.Cohere => ChatModel.Cohere.Command.RPlus2408,
        LLmProviders.Perplexity => ChatModel.Perplexity.Sonar.Pro,
        _ => null,
    };

    private static List<ChatModel> GetModelsForProvider(LLmProviders provider) => provider switch
    {
        LLmProviders.OpenAi =>
        [
            ChatModel.OpenAi.Gpt41.V41Mini,
            ChatModel.OpenAi.Gpt41.V41,
            ChatModel.OpenAi.Gpt41.V41Nano,
            ChatModel.OpenAi.O3.V3,
        ],
        LLmProviders.Anthropic =>
        [
            ChatModel.Anthropic.Claude37.Sonnet,
            ChatModel.Anthropic.Claude35.SonnetLatest,
        ],
        LLmProviders.Google =>
        [
            ChatModel.Google.Gemini.Gemini25Pro,
            ChatModel.Google.Gemini.Gemini25Flash,
        ],
        LLmProviders.XAi =>
        [
            ChatModel.XAi.Grok3.V3,
        ],
        LLmProviders.DeepSeek =>
        [
            ChatModel.DeepSeek.Models.Chat,
        ],
        LLmProviders.Groq =>
        [
            ChatModel.Groq.Meta.Llama370B,
        ],
        LLmProviders.Mistral =>
        [
            ChatModel.Mistral.Premier.MistralLarge,
        ],
        LLmProviders.Cohere =>
        [
            ChatModel.Cohere.Command.RPlus2408,
        ],
        LLmProviders.Perplexity =>
        [
            ChatModel.Perplexity.Sonar.Pro,
        ],
        _ => [],
    };
}
