namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// Type of event chip displayed inline in a message.
/// </summary>
public enum ChatUiChipType
{
    /// <summary>A tool was invoked (function call started).</summary>
    ToolInvoked,

    /// <summary>A tool completed execution with a result.</summary>
    ToolCompleted,

    /// <summary>A tool was denied by the user during approval.</summary>
    ToolDenied,

    /// <summary>An error occurred during tool execution or streaming.</summary>
    Error,

    /// <summary>Informational event (e.g., "Thinking...", "Searching files...").</summary>
    Info,

    /// <summary>AI reasoning / chain-of-thought block.</summary>
    Reasoning
}

/// <summary>
/// Status of an event chip's lifecycle.
/// </summary>
public enum ChatUiChipStatus
{
    /// <summary>Event is currently in progress (shows spinner).</summary>
    InProgress,

    /// <summary>Event completed successfully (shows checkmark).</summary>
    Completed,

    /// <summary>Event failed or was denied (shows X icon).</summary>
    Failed
}

/// <summary>
/// A compact inline event indicator within a chat message.
/// Used to show tool calls, reasoning, errors, etc.
/// </summary>
public sealed class ChatUiEventChip
{
    /// <summary>
    /// Unique identifier for this chip. Used by UpdateEventChip to target updates.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The category of event.
    /// </summary>
    public ChatUiChipType Type { get; set; }

    /// <summary>
    /// Short title displayed on the chip (e.g., "read_file", "Thinking").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Expandable detail content. For tools: the arguments JSON and result.
    /// For reasoning: the chain-of-thought text.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle status — drives the visual indicator (spinner/check/X).
    /// </summary>
    public ChatUiChipStatus Status { get; set; } = ChatUiChipStatus.InProgress;

    /// <summary>
    /// When this event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
