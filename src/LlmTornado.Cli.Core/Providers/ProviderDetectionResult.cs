using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Providers;

/// <summary>
/// Detected provider with its API key and available models.
/// </summary>
public sealed class DetectedProvider
{
    public required LLmProviders Provider { get; init; }
    public required string ApiKey { get; init; }
    public required List<ChatModel> Models { get; init; }
    public required ChatModel DefaultModel { get; init; }

    /// <summary>
    /// Friendly endpoint label for Custom/OpenAI-compat providers (e.g. "ollama", "lmstudio").
    /// Null for cloud providers that share the multi-auth <see cref="ProviderDetectionResult.Api"/>.
    /// </summary>
    public string? EndpointName { get; init; }

    /// <summary>
    /// Dedicated <see cref="TornadoApi"/> for this endpoint. Required for Custom providers because
    /// a single TornadoApi can only hold one Custom BaseUrl. Null for cloud providers.
    /// </summary>
    public TornadoApi? DedicatedApi { get; init; }

    /// <summary>
    /// Optional default context window for models on this endpoint when the model card is silent.
    /// </summary>
    public int? DefaultContextTokens { get; init; }
}

/// <summary>
/// Result of provider detection.
/// </summary>
public sealed class ProviderDetectionResult
{
    public required TornadoApi Api { get; init; }
    public required List<DetectedProvider> Providers { get; init; }
    public required ChatModel ActiveModel { get; set; }

    public List<ChatModel> AllModels => Providers.SelectMany(p => p.Models).ToList();

    /// <summary>
    /// A cheap/fast model for internal tasks like tool optimization.
    /// Auto-selected from detected providers, preferring small models.
    /// </summary>
    public ChatModel? OptimizerModel { get; set; }

    /// <summary>
    /// Route a model to the TornadoApi that can actually call it. Cloud models use the shared
    /// multi-auth <see cref="Api"/>; Custom/OpenAI-compat models use their endpoint's
    /// <see cref="DetectedProvider.DedicatedApi"/>.
    /// </summary>
    public TornadoApi GetApiForModel(ChatModel model)
    {
        DetectedProvider? owner = FindOwner(model);
        return owner?.DedicatedApi ?? Api;
    }

    /// <summary>
    /// Find the detected provider that owns <paramref name="model"/> (by name + provider).
    /// When multiple Custom endpoints expose the same model name, prefer an exact EndpointName
    /// match if the caller qualified the name as <c>endpoint/model</c>.
    /// </summary>
    public DetectedProvider? FindOwner(ChatModel model)
    {
        // Prefer reference equality first (models from AllModels).
        DetectedProvider? byRef = Providers.FirstOrDefault(p => p.Models.Any(m => ReferenceEquals(m, model)));
        if (byRef is not null)
            return byRef;

        List<DetectedProvider> byName = Providers
            .Where(p => p.Provider == model.Provider &&
                        p.Models.Any(m => m.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return byName.Count == 1 ? byName[0] : byName.FirstOrDefault();
    }

    /// <summary>
    /// Resolve a model by bare name or <c>endpoint/model</c> qualification.
    /// Returns null when not found or when a bare name is ambiguous across endpoints.
    /// </summary>
    public ChatModel? ResolveModel(string nameOrQualified, out string? ambiguityError)
    {
        ambiguityError = null;
        if (string.IsNullOrWhiteSpace(nameOrQualified))
            return null;

        string input = nameOrQualified.Trim();

        // Qualified: endpoint/model
        int slash = input.IndexOf('/');
        if (slash > 0 && slash < input.Length - 1)
        {
            string endpoint = input[..slash];
            string modelName = input[(slash + 1)..];
            DetectedProvider? provider = Providers.FirstOrDefault(p =>
                p.EndpointName is not null &&
                p.EndpointName.Equals(endpoint, StringComparison.OrdinalIgnoreCase));
            if (provider is null)
                return null;

            return provider.Models.FirstOrDefault(m =>
                m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        }

        List<(DetectedProvider Provider, ChatModel Model)> matches = [];
        foreach (DetectedProvider provider in Providers)
        {
            foreach (ChatModel model in provider.Models)
            {
                if (model.Name.Equals(input, StringComparison.OrdinalIgnoreCase))
                    matches.Add((provider, model));
            }
        }

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0].Model;

        ambiguityError =
            $"Model '{input}' is available on multiple endpoints: " +
            string.Join(", ", matches.Select(m =>
                $"{m.Provider.EndpointName ?? m.Provider.Provider.ToString()}/{m.Model.Name}")) +
            ". Qualify as endpoint/model.";
        return null;
    }
}
