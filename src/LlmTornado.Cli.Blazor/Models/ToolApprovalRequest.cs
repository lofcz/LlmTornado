namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// A pending tool approval request displayed to the user.
/// The controller creates this when a tool needs permission,
/// and the UI resolves it when the user approves or denies.
/// </summary>
public sealed class ToolApprovalRequest
{
    /// <summary>
    /// Unique identifier for this approval request.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The name of the tool requesting approval (e.g., "read_file").
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// The full request message from the runtime (e.g., "Tool 'read_file' wants to read ./config.json").
    /// </summary>
    public string RequestMessage { get; set; } = string.Empty;

    /// <summary>
    /// The function call arguments as a JSON string (for display).
    /// Empty if not applicable.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// The async completion source. The controller awaits this.
    /// The UI sets the result when the user clicks Approve or Deny.
    /// </summary>
    public TaskCompletionSource<bool> Completion { get; } = new();

    /// <summary>
    /// When this request was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
