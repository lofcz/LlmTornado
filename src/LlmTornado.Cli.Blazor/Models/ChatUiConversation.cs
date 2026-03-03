namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// A conversation entry for the sidebar conversation list.
/// </summary>
public sealed class ChatUiConversation
{
    /// <summary>
    /// Unique conversation identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User-set or auto-generated label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Preview of the first user message (truncated).
    /// </summary>
    public string? Preview { get; set; }

    /// <summary>
    /// When the conversation was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Number of messages in the conversation.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Display title — uses Label if set, or Preview, or a fallback.
    /// </summary>
    public string DisplayTitle => Label ?? Preview ?? $"Conversation {Id[..8]}...";
}
