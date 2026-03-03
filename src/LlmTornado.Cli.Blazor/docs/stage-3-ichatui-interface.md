# Stage 3: IChatUi Interface — UI Manipulation API

## Goal

Define `IChatUi` — the interface that the **controller calls** to drive the UI. This is the "push" API: the controller tells the UI what to display, when to start streaming, when to add events, etc. The UI component (`TornadoChatPanel`) implements this interface.

## Design Philosophy

The UI is a **dumb renderer**. It has no AI logic — it just displays what it's told. All intelligence lives in the controller. This means:

- You can unit test the UI by creating a mock controller that calls `IChatUi` methods
- You can unit test the controller by creating a mock `IChatUi` that records method calls
- The same UI can be driven by completely different backends (local `ChatRuntime`, HTTP API, mock data)

## Full Interface

**Path:** `src/LlmTornado.Cli.Blazor/IChatUi.cs`

```csharp
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
```

## Method-by-Method Explanation

### Message Lifecycle

These three methods handle the streaming message flow:

```
SendMessageAsync()
    │
    ├── Ui.AddMessage(userMessage)         ← User's message appears instantly
    │
    ├── Ui.StartStreamingMessage("msg-1")  ← Empty bubble with typing cursor
    │
    │   ┌── Loop: for each token from LLM ──────────────────┐
    │   │                                                     │
    │   │   Ui.AppendStreamingToken("msg-1", "Hello")        │
    │   │   Ui.AppendStreamingToken("msg-1", " world")       │
    │   │   Ui.AppendStreamingToken("msg-1", "!")             │
    │   │                                                     │
    │   └── (tokens keep arriving) ──────────────────────────┘
    │
    └── Ui.CompleteStreamingMessage("msg-1") ← Cursor removed, message finalized
```

**Implementation in the component:**

```csharp
// Inside TornadoChatPanel.razor.cs (implements IChatUi)

private readonly List<ChatUiMessage> _messages = [];
private readonly Dictionary<string, ChatUiMessage> _streamingMessages = [];

public void AddMessage(ChatUiMessage message)
{
    InvokeAsync(() =>
    {
        _messages.Add(message);
        StateHasChanged();
    });
}

public void StartStreamingMessage(string messageId)
{
    InvokeAsync(() =>
    {
        var msg = new ChatUiMessage
        {
            Id = messageId,
            Role = ChatUiRole.Assistant,
            IsStreaming = true
        };
        _messages.Add(msg);
        _streamingMessages[messageId] = msg;
        StateHasChanged();
    });
}

public void AppendStreamingToken(string messageId, string token)
{
    InvokeAsync(() =>
    {
        if (_streamingMessages.TryGetValue(messageId, out var msg))
        {
            msg.Content += token;
            StateHasChanged();
        }
    });
}

public void CompleteStreamingMessage(string messageId)
{
    InvokeAsync(() =>
    {
        if (_streamingMessages.Remove(messageId, out var msg))
        {
            msg.IsStreaming = false;
            StateHasChanged();
        }
    });
}
```

**Performance note on `StateHasChanged`:** Calling `StateHasChanged()` on every token is acceptable in Blazor Server because it's a diff-based renderer — only the changed DOM elements are sent over SignalR. For WASM, the component should debounce (e.g., batch tokens and re-render every 50ms). We'll add a configurable `StreamingRenderInterval` parameter.

---

### Event Chip Flow

Event chips are added to the **last assistant message** (or the active streaming message):

```csharp
public void AddEventChip(ChatUiEventChip chip)
{
    InvokeAsync(() =>
    {
        // Find the target message: active streaming message, or last assistant message
        ChatUiMessage? target = _streamingMessages.Values.LastOrDefault()
            ?? _messages.LastOrDefault(m => m.Role == ChatUiRole.Assistant);

        if (target is not null)
        {
            target.EventChips.Add(chip);
            StateHasChanged();
        }
    });
}

public void UpdateEventChip(string chipId, ChatUiEventChip updated)
{
    InvokeAsync(() =>
    {
        foreach (var msg in _messages)
        {
            int idx = msg.EventChips.FindIndex(c => c.Id == chipId);
            if (idx >= 0)
            {
                msg.EventChips[idx] = updated;
                StateHasChanged();
                return;
            }
        }
    });
}
```

**Example sequence from controller:**

```csharp
// Controller receives AgentRunnerToolInvokedEvent
var chip = new ChatUiEventChip
{
    Id = toolCallId,
    Type = ChatUiChipType.ToolInvoked,
    Title = functionCall.Name,          // e.g., "read_file"
    Detail = functionCall.Arguments,    // e.g., {"path": "./README.md"}
    Status = ChatUiChipStatus.InProgress
};
Ui.AddEventChip(chip);

// ... tool executes ...

// Controller receives AgentRunnerToolCompletedEvent
var updated = new ChatUiEventChip
{
    Id = toolCallId,
    Type = ChatUiChipType.ToolCompleted,
    Title = functionCall.Name,
    Detail = $"Arguments: {functionCall.Arguments}\n\nResult: {toolResult.Output}",
    Status = ChatUiChipStatus.Completed
};
Ui.UpdateEventChip(toolCallId, updated);
```

---

### Tool Approval

```csharp
private ToolApprovalRequest? _pendingApproval;

public void ShowToolApproval(ToolApprovalRequest request)
{
    InvokeAsync(() =>
    {
        _pendingApproval = request;
        StateHasChanged();
    });
}

// In the template:
// @if (_pendingApproval is not null)
// {
//     <ToolApprovalBanner Request="_pendingApproval" 
//                         OnRespond="HandleToolApprovalResponse" />
// }

private void HandleToolApprovalResponse(bool approved)
{
    if (_pendingApproval is not null)
    {
        _pendingApproval.Completion.SetResult(approved);
        _pendingApproval = null;
        StateHasChanged();
    }
}
```

---

### Configuration Dropdowns

```csharp
private List<ChatUiModel> _models = [];
private string? _selectedModelId;
private List<ChatUiAgent> _agents = [];
private string? _selectedAgentName;

public void SetModels(List<ChatUiModel> models)
{
    InvokeAsync(() =>
    {
        _models = models;
        StateHasChanged();
    });
}

public void SetSelectedModel(string modelId)
{
    InvokeAsync(() =>
    {
        _selectedModelId = modelId;
        StateHasChanged();
    });
}

public void SetAgents(List<ChatUiAgent> agents)
{
    InvokeAsync(() =>
    {
        _agents = agents;
        StateHasChanged();
    });
}

public void SetSelectedAgent(string? agentName)
{
    InvokeAsync(() =>
    {
        _selectedAgentName = agentName;
        StateHasChanged();
    });
}
```

---

### Conversation Management and State

```csharp
private List<ChatUiConversation> _conversations = [];

public void SetConversations(List<ChatUiConversation> conversations)
{
    InvokeAsync(() =>
    {
        _conversations = conversations;
        StateHasChanged();
    });
}

public void SetLoading(bool loading)
{
    InvokeAsync(() =>
    {
        _isLoading = loading;
        StateHasChanged();
    });
}

public void Clear()
{
    InvokeAsync(() =>
    {
        _messages.Clear();
        _streamingMessages.Clear();
        _pendingApproval = null;
        StateHasChanged();
    });
}
```

---

## Why `InvokeAsync` Everywhere?

In Blazor Server, the AI runtime events arrive on background threads (Task continuations from HTTP calls, MCP stdio reads, etc.). Blazor's rendering pipeline requires all UI mutations to happen on the synchronization context. `InvokeAsync` marshals the call to the renderer's context.

In Blazor WASM, everything is single-threaded, so `InvokeAsync` is essentially a no-op — but it's still correct to use it for compatibility.

## Thread Safety Contract

The `IChatUi` contract guarantees:
1. **All methods are safe to call from any thread.** Implementations use `InvokeAsync`.
2. **Methods are fire-and-forget from the caller's perspective.** They don't return `Task` — the `InvokeAsync` call is queued internally.
3. **Ordering is preserved.** Blazor's `InvokeAsync` processes calls in FIFO order.

This means the controller can freely call `AppendStreamingToken` in a tight loop from a streaming callback without worrying about thread safety.

## Testing Strategy

**Unit testing the UI (without a real controller):**

```csharp
// Arrange
var ui = new TornadoChatPanel(); // or a test double
var controller = new MockController { Ui = ui };

// Act — simulate controller behavior
ui.StartStreamingMessage("test-1");
ui.AppendStreamingToken("test-1", "Hello ");
ui.AppendStreamingToken("test-1", "world!");
ui.CompleteStreamingMessage("test-1");

// Assert
Assert.Single(ui.Messages);
Assert.Equal("Hello world!", ui.Messages[0].Content);
Assert.False(ui.Messages[0].IsStreaming);
```

**Unit testing the controller (with a mock UI):**

```csharp
// Arrange
var mockUi = new RecordingChatUi(); // Records all method calls
var controller = new ChatRuntimeController(options) { Ui = mockUi };

// Act
await controller.SendMessageAsync("What is 2+2?", null);

// Assert
Assert.Contains(mockUi.Calls, c => c.Method == "StartStreamingMessage");
Assert.Contains(mockUi.Calls, c => c.Method == "AppendStreamingToken");
Assert.Contains(mockUi.Calls, c => c.Method == "CompleteStreamingMessage");
```
