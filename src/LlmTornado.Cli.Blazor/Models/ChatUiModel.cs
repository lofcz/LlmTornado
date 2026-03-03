namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// A selectable LLM model for the model dropdown.
/// </summary>
public sealed class ChatUiModel
{
    /// <summary>
    /// The model identifier string (e.g., "claude-4.6-opus").
    /// Used as the value when selecting.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Claude 4.6 Opus").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Provider name for grouping (e.g., "Anthropic", "OpenAI").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider/model is currently available (API key configured).
    /// Unavailable models are shown greyed out in the dropdown.
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}
