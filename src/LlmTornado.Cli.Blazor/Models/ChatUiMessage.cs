namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// Role of the message author in the chat UI.
/// </summary>
public enum ChatUiRole
{
    User,
    Assistant,
    System
}

/// <summary>
/// A single message displayed in the chat panel.
/// This is a UI view model — not the internal LlmTornado ChatMessage.
/// </summary>
public sealed class ChatUiMessage
{
    /// <summary>
    /// Unique identifier for this message. Used by streaming methods
    /// to target a specific message bubble for token appending.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Who sent this message.
    /// </summary>
    public ChatUiRole Role { get; set; }

    /// <summary>
    /// The text content (plain text or markdown).
    /// For streaming messages, this grows as tokens arrive.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Files attached to this message (images, PDFs, audio).
    /// Typically only present on User messages.
    /// </summary>
    public List<ChatUiFile> Files { get; set; } = [];

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True while the assistant is still streaming tokens into this message.
    /// The UI shows a typing cursor when this is true.
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    /// True if this message represents an error (e.g., API failure).
    /// The UI renders it with error styling.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Accumulated reasoning/thinking text from the model's chain-of-thought.
    /// Grows during streaming as thinking tokens arrive.
    /// </summary>
    public string ThinkingContent { get; set; } = string.Empty;

    /// <summary>
    /// True while the model is actively streaming thinking/reasoning tokens.
    /// The UI shows a pulsing "Thinking..." block when this is true.
    /// </summary>
    public bool IsThinking { get; set; }

    /// <summary>
    /// When the thinking phase started (first thinking token received).
    /// Used to compute thinking duration for the collapsed pill.
    /// </summary>
    public DateTime? ThinkingStartedAt { get; set; }

    /// <summary>
    /// When the thinking phase completed (thinking done event received or first text token after thinking).
    /// Used to compute thinking duration for the collapsed pill.
    /// </summary>
    public DateTime? ThinkingCompletedAt { get; set; }

    /// <summary>
    /// Inline event chips (tool calls, reasoning, etc.) that belong to this message.
    /// Displayed inline within the assistant message bubble.
    /// </summary>
    public List<ChatUiEventChip> EventChips { get; set; } = [];

    /// <summary>
    /// Token telemetry for this assistant turn.
    /// Includes preflight context/request counts and accumulated actual usage.
    /// </summary>
    public ChatUiTokenTelemetry? TokenTelemetry { get; set; }
}
