using LlmTornado.Cli.Blazor.Models;

namespace LlmTornado.Cli.Blazor;

/// <summary>
/// Interface for controlling the chat UI. Implemented by the chat panel component.
/// The controller calls these methods to drive what the UI displays.
/// 
/// All methods are safe to call from non-UI threads — implementations
/// use InvokeAsync internally for Blazor synchronization context.
/// </summary>
public interface IChatUi
{
    // ─────────────────────────────────────────────
    // Message management
    // ─────────────────────────────────────────────

    /// <summary>
    /// Add a complete message to the chat display.
    /// Used for user messages and for non-streaming assistant responses.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void AddMessage(ChatUiMessage message);

    /// <summary>
    /// Begin a new streaming assistant message. Creates an empty message bubble
    /// with the typing indicator. Call <see cref="AppendStreamingToken"/> to fill it with content.
    /// </summary>
    /// <param name="messageId">
    /// Unique ID for the streaming message. Use this same ID with
    /// <see cref="AppendStreamingToken"/> and <see cref="CompleteStreamingMessage"/>.
    /// </param>
    void StartStreamingMessage(string messageId);

    /// <summary>
    /// Append a text delta token to an active streaming message.
    /// Called many times during streaming — typically once per token.
    /// </summary>
    /// <param name="messageId">The ID of the streaming message (from <see cref="StartStreamingMessage"/>).</param>
    /// <param name="token">The text delta to append.</param>
    void AppendStreamingToken(string messageId, string token);

    /// <summary>
    /// Finalize a streaming message. Removes the typing indicator
    /// and marks the message as complete.
    /// </summary>
    /// <param name="messageId">The ID of the streaming message.</param>
    void CompleteStreamingMessage(string messageId);

    // ─────────────────────────────────────────────
    // Event chips (tool calls, reasoning, etc.)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Add an event chip to the current streaming message.
    /// If no streaming message is active, the chip is added to the last assistant message.
    /// </summary>
    /// <param name="chip">The event chip to display.</param>
    void AddEventChip(ChatUiEventChip chip);

    /// <summary>
    /// Update an existing event chip's properties (status, detail, etc.).
    /// Typically used to transition a tool from InProgress → Completed/Failed.
    /// </summary>
    /// <param name="chipId">The ID of the chip to update.</param>
    /// <param name="updated">The updated chip data. The ID must match <paramref name="chipId"/>.</param>
    void UpdateEventChip(string chipId, ChatUiEventChip updated);

    /// <summary>
    /// Updates token telemetry for a specific message.
    /// </summary>
    /// <param name="messageId">The target assistant message ID.</param>
    /// <param name="telemetry">The latest merged token telemetry.</param>
    void UpdateMessageTokenTelemetry(string messageId, ChatUiTokenTelemetry telemetry);

    /// <summary>
    /// Updates the persistent context window footer for the active model.
    /// </summary>
    /// <param name="status">The latest context window status.</param>
    void SetContextWindowStatus(ChatUiContextWindowStatus status);

    // ─────────────────────────────────────────────
    // Tool approval
    // ─────────────────────────────────────────────

    /// <summary>
    /// Display a tool approval prompt to the user.
    /// The controller awaits <see cref="ToolApprovalRequest.Completion"/> for the user's decision.
    /// </summary>
    /// <param name="request">The approval request containing tool details and the completion source.</param>
    void ShowToolApproval(ToolApprovalRequest request);

    // ─────────────────────────────────────────────
    // Configuration dropdowns
    // ─────────────────────────────────────────────

    /// <summary>
    /// Populate the model selector dropdown with available models.
    /// Models are grouped by provider in the dropdown.
    /// </summary>
    /// <param name="models">All available models.</param>
    void SetModels(List<ChatUiModel> models);

    /// <summary>
    /// Set the currently selected model in the dropdown.
    /// </summary>
    /// <param name="modelId">The model ID to select.</param>
    void SetSelectedModel(string modelId);

    /// <summary>
    /// Populate the agent selector dropdown with available personas.
    /// </summary>
    /// <param name="agents">All available agent personas.</param>
    void SetAgents(List<ChatUiAgent> agents);

    /// <summary>
    /// Set the currently selected agent in the dropdown.
    /// Null means "default" (no persona).
    /// </summary>
    /// <param name="agentName">The agent name to select, or null for default.</param>
    void SetSelectedAgent(string? agentName);

    // ─────────────────────────────────────────────
    // Conversation sidebar
    // ─────────────────────────────────────────────

    /// <summary>
    /// Populate the conversation sidebar with saved sessions.
    /// </summary>
    /// <param name="conversations">The conversation list, ordered by most recent first.</param>
    void SetConversations(List<ChatUiConversation> conversations);

    // ─────────────────────────────────────────────
    // State indicators
    // ─────────────────────────────────────────────

    /// <summary>
    /// Show or hide a global loading/processing indicator.
    /// Displayed while the controller is initializing or performing background work.
    /// </summary>
    /// <param name="loading">True to show the indicator, false to hide.</param>
    void SetLoading(bool loading);

    /// <summary>
    /// Clear all messages, event chips, and state from the UI.
    /// Used when starting a new conversation or loading a different one.
    /// </summary>
    void Clear();
}
