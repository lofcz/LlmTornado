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
}
