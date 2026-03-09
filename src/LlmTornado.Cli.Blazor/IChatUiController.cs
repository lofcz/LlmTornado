using LlmTornado.Cli.Blazor.Models;

namespace LlmTornado.Cli.Blazor;

/// <summary>
/// Interface for handling user actions from the chat UI.
/// Implemented by the controller (e.g., ChatRuntimeController).
/// 
/// The UI component holds a reference to this interface and calls methods
/// when the user interacts with the chat panel.
/// </summary>
public interface IChatUiController : IAsyncDisposable
{
    /// <summary>
    /// Reference to the UI component. Set by the component on initialization.
    /// The controller uses this to push updates back to the UI.
    /// </summary>
    IChatUi? Ui { get; set; }

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    /// <summary>
    /// Initialize the controller. Called from the component's OnInitializedAsync.
    /// Performs provider detection, loads models, agents, skills, MCP tools,
    /// and populates the UI dropdowns and conversation list.
    /// </summary>
    Task InitializeAsync();

    // ─────────────────────────────────────────────
    // Chat actions
    // ─────────────────────────────────────────────

    /// <summary>
    /// Send a user message (with optional file attachments) to the AI.
    /// The controller will:
    /// 1. Display the user message via Ui.AddMessage
    /// 2. Start a streaming assistant message via Ui.StartStreamingMessage
    /// 3. Forward streaming tokens via Ui.AppendStreamingToken
    /// 4. Display tool calls/results via Ui.AddEventChip/UpdateEventChip
    /// 5. Finalize via Ui.CompleteStreamingMessage
    /// </summary>
    /// <param name="text">The user's text message.</param>
    /// <param name="files">Optional file attachments.</param>
    Task SendMessageAsync(string text, List<ChatUiFile>? files);

    /// <summary>
    /// Cancel the currently running AI request.
    /// Stops streaming and marks the current message as incomplete.
    /// </summary>
    Task CancelAsync();

    // ─────────────────────────────────────────────
    // Model / Agent selection
    // ─────────────────────────────────────────────

    /// <summary>
    /// Change the active LLM model.
    /// Rebuilds the agent with the new model and updates the UI dropdown.
    /// </summary>
    /// <param name="modelId">The model identifier string.</param>
    Task SelectModelAsync(string modelId);

    /// <summary>
    /// Change the active agent persona.
    /// Rebuilds the agent with the new persona's capability curation and instructions.
    /// Pass null to clear the persona (revert to default).
    /// </summary>
    /// <param name="agentName">The agent name, or null for default.</param>
    Task SelectAgentAsync(string? agentName);

    /// <summary>
    /// Change the reasoning effort level for models that support extended thinking.
    /// Pass null to revert to the provider/model default.
    /// </summary>
    /// <param name="effort">The reasoning effort string ("none", "low", "medium", "high", etc.), or null for auto.</param>
    Task SelectReasoningEffortAsync(string? effort);

    // ─────────────────────────────────────────────
    // Conversation management
    // ─────────────────────────────────────────────

    /// <summary>
    /// Load a saved conversation by ID.
    /// Clears the current UI and populates it with the saved messages.
    /// </summary>
    /// <param name="conversationId">The conversation ID to load.</param>
    Task LoadConversationAsync(string conversationId);

    /// <summary>
    /// Start a new empty conversation.
    /// Clears the current UI and resets the runtime state.
    /// </summary>
    Task NewConversationAsync();

    /// <summary>
    /// Delete a saved conversation.
    /// Removes it from the sidebar and disk.
    /// </summary>
    /// <param name="conversationId">The conversation ID to delete.</param>
    Task DeleteConversationAsync(string conversationId);

    // ─────────────────────────────────────────────
    // Tool approval response
    // ─────────────────────────────────────────────

    /// <summary>
    /// Respond to a pending tool approval request.
    /// Called by the UI when the user clicks Approve or Deny.
    /// </summary>
    /// <param name="requestId">The approval request ID.</param>
    /// <param name="approved">True if approved, false if denied.</param>
    /// <param name="alwaysAllow">True if the tool should be auto-approved in the future.</param>
    Task RespondToToolApprovalAsync(string requestId, bool approved, bool alwaysAllow = false);
}
