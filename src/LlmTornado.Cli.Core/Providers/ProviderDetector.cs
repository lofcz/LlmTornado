using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Providers;

/// <summary>
/// Auto-detect LLM providers from standard environment variables.
/// </summary>
public static class ProviderDetector
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

        ChatModel? optimizerModel = GetOptimizerModel(detected);

        return new ProviderDetectionResult
        {
            Api = api,
            Providers = detected,
            ActiveModel = activeModel,
            OptimizerModel = optimizerModel,
        };
    }

    /// <summary>
    /// Priority order of cheap/fast models for internal optimization tasks.
    /// </summary>
    private static readonly (LLmProviders Provider, Func<ChatModel> Model)[] OptimizerModelPriority =
    [
        (LLmProviders.Google, () => ChatModel.Google.Gemini.Gemini25Flash),
        (LLmProviders.OpenAi, () => ChatModel.OpenAi.O4.V4Mini),
        (LLmProviders.Anthropic, () => ChatModel.Anthropic.Claude4.Sonnet250514),
        (LLmProviders.Groq, () => ChatModel.Groq.Meta.Llama4Scout),
        (LLmProviders.DeepSeek, () => ChatModel.DeepSeek.Models.Chat),
        (LLmProviders.Mistral, () => ChatModel.Mistral.Free.MistralLarge2512),
        (LLmProviders.XAi, () => ChatModel.XAi.Grok41.V41FastReasoning),
    ];

    /// <summary>
    /// Select the cheapest/fastest available model for internal tasks like tool optimization.
    /// </summary>
    private static ChatModel? GetOptimizerModel(List<DetectedProvider> detected)
    {
        foreach ((LLmProviders provider, Func<ChatModel> modelFactory) in OptimizerModelPriority)
        {
            if (detected.Any(d => d.Provider == provider))
                return modelFactory();
        }

        // Fallback: use the first detected provider's default model
        return detected.Count > 0 ? detected[0].DefaultModel : null;
    }

    private static ChatModel? GetDefaultModel(LLmProviders provider) => provider switch
    {
        LLmProviders.Anthropic => ChatModel.Anthropic.Claude46.Opus,
        LLmProviders.OpenAi => ChatModel.OpenAi.Gpt52.V52,
        LLmProviders.Google => ChatModel.Google.GeminiPreview.Gemini3ProPreview,
        LLmProviders.XAi => ChatModel.XAi.Grok4.V4,
        LLmProviders.DeepSeek => ChatModel.DeepSeek.Models.Chat,
        LLmProviders.Groq => ChatModel.Groq.Meta.Llama4Maverick,
        LLmProviders.Mistral => ChatModel.Mistral.Premier.MistralMedium2508,
        LLmProviders.Cohere => ChatModel.Cohere.Command.A0325,
        LLmProviders.Perplexity => ChatModel.Perplexity.Sonar.Pro,
        _ => null,
    };

    private static List<ChatModel> GetModelsForProvider(LLmProviders provider) => provider switch
    {
        LLmProviders.OpenAi =>
        [
            ChatModel.OpenAi.Gpt52.V52,
            ChatModel.OpenAi.Gpt52.V52Pro,
            ChatModel.OpenAi.Gpt51.V51,
            ChatModel.OpenAi.Gpt51.V51CodexMax,
            ChatModel.OpenAi.O4.V4Mini,
            ChatModel.OpenAi.O3.V3,
        ],
        LLmProviders.Anthropic =>
        [
            ChatModel.Anthropic.Claude46.Opus,
            ChatModel.Anthropic.Claude45.Opus251101,
            ChatModel.Anthropic.Claude45.Sonnet250929,
            ChatModel.Anthropic.Claude4.Sonnet250514,
        ],
        LLmProviders.Google =>
        [
            ChatModel.Google.GeminiPreview.Gemini3ProPreview,
            ChatModel.Google.GeminiPreview.Gemini3FlashPreview,
            ChatModel.Google.Gemini.Gemini25Pro,
            ChatModel.Google.Gemini.Gemini25Flash,
        ],
        LLmProviders.XAi =>
        [
            ChatModel.XAi.Grok4.V4,
            ChatModel.XAi.Grok4.V4FastReasoning,
            ChatModel.XAi.Grok41.V41FastReasoning,
        ],
        LLmProviders.DeepSeek =>
        [
            ChatModel.DeepSeek.Models.Chat,
            ChatModel.DeepSeek.Models.Reasoner,
        ],
        LLmProviders.Groq =>
        [
            ChatModel.Groq.Meta.Llama4Maverick,
            ChatModel.Groq.Meta.Llama4Scout,
            ChatModel.Groq.Meta.Llama3370BVersatile,
        ],
        LLmProviders.Mistral =>
        [
            ChatModel.Mistral.Premier.MistralMedium2508,
            ChatModel.Mistral.Premier.MagistralMedium2509,
            ChatModel.Mistral.Free.MistralLarge2512,
        ],
        LLmProviders.Cohere =>
        [
            ChatModel.Cohere.Command.A0325,
            ChatModel.Cohere.Command.AReasoning2508,
            ChatModel.Cohere.Command.AVision2507,
        ],
        LLmProviders.Perplexity =>
        [
            ChatModel.Perplexity.Sonar.Pro,
            ChatModel.Perplexity.Sonar.Default,
            ChatModel.Perplexity.Sonar.DeepResearch,
        ],
        _ => [],
    };
}
