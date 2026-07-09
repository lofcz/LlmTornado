using System.Text.Json;
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

    /// <summary>
    /// Default Ollama endpoint when <c>OLLAMA_HOST</c> is not set.
    /// </summary>
    private const string DefaultOllamaHost = "http://localhost:11434";

    /// <summary>
    /// Detect cloud providers from env vars, Ollama from <c>OLLAMA_HOST</c>, and any configured
    /// OpenAI-compatible endpoints (LM Studio / llama.cpp / vLLM).
    /// </summary>
    /// <param name="openAiCompatEndpoints">
    /// Merged settings+env OpenAI-compat endpoints. Each reachable endpoint becomes its own
    /// Custom provider with a dedicated <see cref="TornadoApi"/> (one BaseUrl per api).
    /// </param>
    /// <param name="warnings">Optional sink for probe warnings (unreachable endpoints, empty lists).</param>
    public static ProviderDetectionResult? Detect(
        IReadOnlyList<OpenAiCompatEndpoint>? openAiCompatEndpoints = null,
        Action<string>? warnings = null)
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

        // Local (self-hosted) Ollama, via LLmProviders.Custom. Models are user-installed, so
        // discover them from the running server rather than a hardcoded list. Ollama is the first
        // auto-registered Custom endpoint and keeps its native /api/tags probe + context inspector.
        string host = NormalizeHost(Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? DefaultOllamaHost);
        List<ChatModel> ollamaModels = GetOllamaModels(host);
        if (ollamaModels.Count > 0)
        {
            TornadoApi ollamaApi = new(new Uri(host.TrimEnd('/') + "/"), string.Empty, LLmProviders.Custom);
            detected.Add(new DetectedProvider
            {
                Provider = LLmProviders.Custom,
                ApiKey = string.Empty,
                Models = ollamaModels,
                DefaultModel = ollamaModels[0],
                EndpointName = "ollama",
                DedicatedApi = ollamaApi,
            });
        }

        // User-configured OpenAI-compatible endpoints (LM Studio, llama.cpp server, vLLM, …).
        if (openAiCompatEndpoints is { Count: > 0 })
        {
            foreach (OpenAiCompatEndpoint endpoint in openAiCompatEndpoints)
            {
                // Don't double-register if the user named something "ollama" while Ollama is already up.
                if (detected.Any(d =>
                        d.EndpointName is not null &&
                        d.EndpointName.Equals(endpoint.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    warnings?.Invoke($"Skipping OpenAI-compat endpoint '{endpoint.Name}' — name already registered.");
                    continue;
                }

                List<ChatModel> models = OpenAiCompatProber.ProbeModels(endpoint, out string? warning);
                if (warning is not null)
                    warnings?.Invoke(warning);

                if (models.Count == 0)
                    continue;

                TornadoApi dedicated = OpenAiCompatProber.CreateApi(endpoint);
                detected.Add(new DetectedProvider
                {
                    Provider = LLmProviders.Custom,
                    ApiKey = endpoint.ApiKey ?? string.Empty,
                    Models = models,
                    DefaultModel = models[0],
                    EndpointName = endpoint.Name,
                    DedicatedApi = dedicated,
                    DefaultContextTokens = endpoint.ContextTokens is > 0 ? endpoint.ContextTokens : null,
                });
            }
        }

        if (detected.Count == 0)
            return null;

        // Shared multi-auth api for cloud providers only. Custom endpoints each have DedicatedApi
        // because TornadoApi can only hold one Custom BaseUrl.
        List<ProviderAuthentication> cloudAuths = detected
            .Where(p => p.Provider != LLmProviders.Custom)
            .Select(p => new ProviderAuthentication(p.Provider, p.ApiKey))
            .ToList();

        // Prefer a cloud api as the "primary" Api; if only Custom endpoints exist, use the first
        // dedicated one so callers that still read result.Api keep working.
        TornadoApi api = cloudAuths.Count > 0
            ? new TornadoApi(cloudAuths)
            : detected.First(p => p.DedicatedApi is not null).DedicatedApi!;

        // Pick active model based on priority (cloud first, then first Custom endpoint).
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
        (LLmProviders.Google, () => ChatModel.Google.Gemini.Gemini31FlashLite),
        (LLmProviders.OpenAi, () => ChatModel.OpenAi.Gpt54.V54Mini),
        (LLmProviders.Anthropic, () => ChatModel.Anthropic.Claude46.Sonnet),
        (LLmProviders.Groq, () => ChatModel.Groq.OpenAi.GptOss120B),
        (LLmProviders.DeepSeek, () => ChatModel.DeepSeek.Models.Chat),
        (LLmProviders.Mistral, () => ChatModel.Mistral.Free.Ministral14b2512),
        (LLmProviders.XAi, () => ChatModel.XAi.Grok41.V41FastNonReasoning),
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
        LLmProviders.Anthropic => ChatModel.Anthropic.Claude48.Opus,
        LLmProviders.OpenAi => ChatModel.OpenAi.Gpt55.V55,
        LLmProviders.Google => ChatModel.Google.Gemini.GeminiProLatest,
        LLmProviders.XAi => ChatModel.XAi.Grok41.V41FastReasoning,
        LLmProviders.DeepSeek => ChatModel.DeepSeek.Models.Reasoner,
        LLmProviders.Groq => ChatModel.Groq.Meta.Llama3370BVersatile,
        LLmProviders.Mistral => ChatModel.Mistral.Premier.MistralSaba,
        LLmProviders.Cohere => ChatModel.Cohere.Aya.Expanse32B,
        LLmProviders.Perplexity => ChatModel.Perplexity.Sonar.Reasoning,
        _ => null,
    };

    public static List<ChatModel> GetModelsForProvider(LLmProviders provider) => provider switch
    {
        LLmProviders.OpenAi =>
        [
            ChatModel.OpenAi.Gpt54.V54,
            ChatModel.OpenAi.Gpt55.V55,
            ChatModel.OpenAi.Codex.Gpt53Codex,
        ],
        LLmProviders.Anthropic =>
        [
            ChatModel.Anthropic.Claude48.Opus,
            ChatModel.Anthropic.Claude46.Sonnet,
            ChatModel.Anthropic.Claude45.Haiku251001
        ],
        LLmProviders.Google =>
        [
            ChatModel.Google.Gemini.Gemini35Flash,
            ChatModel.Google.Gemini.Gemini31FlashLite,
            ChatModel.Google.Gemini.GeminiProLatest,
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

    /// <summary>
    /// Normalizes an <c>OLLAMA_HOST</c> value into a connectable client base URL: supplies a
    /// scheme and the default port when missing, and rewrites bind-all addresses (0.0.0.0, ::)
    /// to loopback — those are valid for the server to listen on but cannot be connected to.
    /// </summary>
    private static string NormalizeHost(string host)
    {
        host = host.Trim();
        if (host.Length == 0)
            return DefaultOllamaHost;

        string scheme = "http";
        if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            scheme = "https";
            host = host["https://".Length..];
        }
        else if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            host = host["http://".Length..];
        }

        host = host.TrimEnd('/');

        // IPv6 literal in bracket form — assume the user gave a complete authority.
        if (host.Contains(']'))
            return $"{scheme}://{host}";

        string hostPart = host;
        string? portPart = null;
        int colon = host.LastIndexOf(':');
        if (colon > -1)
        {
            hostPart = host[..colon];
            portPart = host[(colon + 1)..];
        }

        if (hostPart is "0.0.0.0" or "::" or "")
            hostPart = "127.0.0.1";

        if (string.IsNullOrEmpty(portPart))
            portPart = "11434";

        return $"{scheme}://{hostPart}:{portPart}";
    }

    /// <summary>
    /// Discovers installed Ollama models by probing the native <c>/api/tags</c> endpoint.
    /// Falls back to the <c>OLLAMA_MODELS</c> / <c>OLLAMA_MODEL</c> environment variables when the
    /// server is unreachable, so an absent local server simply yields no Ollama provider.
    /// </summary>
    private static List<ChatModel> GetOllamaModels(string host)
    {
        List<ChatModel> models = [];

        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(2) };
            string json = client.GetStringAsync($"{host}/api/tags").GetAwaiter().GetResult();

            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement) &&
                modelsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement model in modelsElement.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out JsonElement nameElement) &&
                        nameElement.GetString() is { Length: > 0 } name)
                    {
                        models.Add(new ChatModel(name, LLmProviders.Custom));
                    }
                }
            }
        }
        catch
        {
            // Server down / unreachable - fall through to env-var fallback.
        }

        if (models.Count > 0)
            return models;

        string? envModels = Environment.GetEnvironmentVariable("OLLAMA_MODELS")
                            ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL");
        if (!string.IsNullOrWhiteSpace(envModels))
        {
            foreach (string name in envModels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                models.Add(new ChatModel(name, LLmProviders.Custom));
            }
        }

        return models;
    }
}
