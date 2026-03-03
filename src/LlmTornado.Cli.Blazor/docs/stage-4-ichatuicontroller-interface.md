# Stage 4: IChatUiController — User Action Handler Interface

## Goal

Define `IChatUiController` — the interface that the **UI calls** when the user performs an action. This is the "pull" API: the UI forwards user intent to the controller, and the controller decides what to do with it.

## Relationship to IChatUi

The two interfaces form a bidirectional pair:

```
┌─────────────────────────────────┐             ┌────────────────────────────────┐
│   TornadoChatPanel              │   user      │   ChatRuntimeController        │
│   (implements IChatUi)          │   actions   │   (implements IChatUiController)│
│                                 │ ──────────► │                                │
│   "Controller, the user         │             │   "Ui, show this streaming     │
│    pressed Send with this text" │             │    token / this tool chip"     │
│                                 │ ◄────────── │                                │
│                                 │   UI cmds   │                                │
└─────────────────────────────────┘             └────────────────────────────────┘
```

- **IChatUi** (Stage 3): Controller → UI. Push commands. "Display this."
- **IChatUiController** (this stage): UI → Controller. User actions. "The user did this."

## Full Interface

**Path:** `src/LlmTornado.Cli.Blazor/IChatUiController.cs`

```csharp
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
    Task RespondToToolApprovalAsync(string requestId, bool approved);
}
```

## Method-by-Method Explanation

### `Ui` Property — The Binding Point

```csharp
IChatUi? Ui { get; set; }
```

The `TornadoChatPanel` component sets this during initialization:

```razor
@* TornadoChatPanel.razor *@
@code {
    [Parameter]
    public IChatUiController Controller { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Bind the UI reference so the controller can push updates
        Controller.Ui = this;
        
        // Let the controller initialize (detect providers, load models, etc.)
        await Controller.InitializeAsync();
    }
}
```

This bidirectional binding is the key to the event-driven architecture. The controller doesn't need to know about Blazor or components — it just calls `IChatUi` methods.

---

### `InitializeAsync()` — Startup Sequence

Called once when the component initializes. The default `ChatRuntimeController` implementation will:

```
InitializeAsync()
├── ProviderDetector.Detect()     → builds TornadoApi with available API keys
├── Ui.SetModels(...)             → populates model dropdown
├── Ui.SetSelectedModel(...)      → selects the default/active model
│
├── AgentDefinitionManager.LoadAll()  → discovers personas
├── Ui.SetAgents(...)             → populates agent dropdown
├── Ui.SetSelectedAgent(...)      → selects active persona (or null)
│
├── SkillManager.LoadSkills()     → discovers skills
├── McpConfigLoader.LoadAsync()   → connects MCP servers
│
├── AgentBuilder.Build()          → creates TornadoAgent + ChatRuntime
│
├── ConversationStore.List()      → lists saved conversations
└── Ui.SetConversations(...)      → populates sidebar
```

---

### `SendMessageAsync()` — The Core Chat Flow

This is the most complex method. Here's the full sequence:

```csharp
async Task SendMessageAsync(string text, List<ChatUiFile>? files)
{
    // 1. Display user message immediately
    var userMsg = new ChatUiMessage
    {
        Role = ChatUiRole.User,
        Content = text,
        Files = files ?? []
    };
    Ui.AddMessage(userMsg);

    // 2. Build the ChatMessage for the runtime
    //    (map ChatUiFile → ChatMessagePart using FileAttachmentResolver patterns)
    ChatMessage chatMessage = BuildChatMessage(text, files);

    // 3. Start streaming assistant message
    string streamingId = Guid.NewGuid().ToString();
    Ui.StartStreamingMessage(streamingId);

    try
    {
        // 4. Invoke the runtime (tokens + events arrive via OnRuntimeEvent callback)
        _currentStreamingId = streamingId;
        ChatMessage response = await _runtime.InvokeAsync(chatMessage);

        // 5. Finalize
        Ui.CompleteStreamingMessage(streamingId);

        // 6. Save conversation
        SaveConversation();
    }
    catch (Exception ex)
    {
        Ui.CompleteStreamingMessage(streamingId);
        Ui.AddMessage(new ChatUiMessage
        {
            Role = ChatUiRole.Assistant,
            Content = $"Error: {ex.Message}",
            IsError = true
        });
    }
}
```

The runtime events are handled by a callback registered during `Build()`:

```csharp
// Registered as: runtimeConfig.OnRuntimeEvent = HandleRuntimeEvent;
async ValueTask HandleRuntimeEvent(ChatRuntimeEvents evt)
{
    if (evt is ChatRuntimeAgentRunnerEvents agentEvt)
    {
        switch (agentEvt.AgentRunnerEvent)
        {
            case AgentRunnerStreamingEvent streaming:
                if (streaming.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta)
                {
                    Ui?.AppendStreamingToken(_currentStreamingId!, delta.DeltaText ?? "");
                }
                break;

            case AgentRunnerToolInvokedEvent toolInvoked:
                Ui?.AddEventChip(new ChatUiEventChip
                {
                    Id = toolInvoked.ToolCalled.Id ?? Guid.NewGuid().ToString(),
                    Type = ChatUiChipType.ToolInvoked,
                    Title = toolInvoked.ToolCalled.Name,
                    Detail = toolInvoked.ToolCalled.Arguments,
                    Status = ChatUiChipStatus.InProgress
                });
                break;

            case AgentRunnerToolCompletedEvent toolCompleted:
                Ui?.UpdateEventChip(
                    toolCompleted.ToolCall.Id ?? "",
                    new ChatUiEventChip
                    {
                        Id = toolCompleted.ToolCall.Id ?? "",
                        Type = ChatUiChipType.ToolCompleted,
                        Title = toolCompleted.ToolCall.Name,
                        Detail = toolCompleted.ToolResult?.Output ?? "",
                        Status = ChatUiChipStatus.Completed
                    });
                break;

            case AgentRunnerErrorEvent error:
                Ui?.AddEventChip(new ChatUiEventChip
                {
                    Type = ChatUiChipType.Error,
                    Title = "Error",
                    Detail = error.ErrorMessage,
                    Status = ChatUiChipStatus.Failed
                });
                break;
        }
    }
}
```

---

### `SelectModelAsync()` — Model Switching

```csharp
async Task SelectModelAsync(string modelId)
{
    // Find the ChatModel by name
    ChatModel? model = _allModels.FirstOrDefault(m => m.Name == modelId);
    if (model is null) return;

    // Rebuild the agent with the new model
    _agentBuilder.SetModel(model, HandleRuntimeEvent);

    // Update settings
    _settings.ActiveModel = modelId;
    _persistence.SaveSettings(_settings);

    // Update UI
    Ui?.SetSelectedModel(modelId);
}
```

This calls `AgentBuilder.SetModel()` which internally rebuilds the `TornadoAgent` and `ChatRuntime` with the new model while preserving the conversation history.

---

### `SelectAgentAsync()` — Agent/Persona Switching

```csharp
async Task SelectAgentAsync(string? agentName)
{
    if (agentName is null)
    {
        _agentManager.ClearActivePersona();
    }
    else
    {
        _agentManager.SetActivePersona(agentName);
    }

    // Rebuild with updated capability baseline
    _agentBuilder.RebuildForAgentChange(HandleRuntimeEvent);

    Ui?.SetSelectedAgent(agentName);
}
```

`RebuildForAgentChange()` calls `ApplyCapabilityBaseline()` which:
1. Resets all skills to enabled
2. Applies the persona's skill whitelist/blacklist
3. Applies the persona's tool whitelist/blacklist
4. Pre-approves the persona's auto-approve tools
5. Rebuilds the agent with updated system prompt and tools

---

### `LoadConversationAsync()` — Loading Saved Sessions

```csharp
async Task LoadConversationAsync(string conversationId)
{
    Ui?.SetLoading(true);
    Ui?.Clear();

    List<ChatMessage>? messages = _conversationStore.Load(conversationId);
    if (messages is null)
    {
        Ui?.SetLoading(false);
        return;
    }

    _currentConversationId = conversationId;

    // Map internal ChatMessages → ChatUiMessages
    foreach (ChatMessage msg in messages)
    {
        Ui?.AddMessage(MapToChatUiMessage(msg));
    }

    // Restore the runtime's conversation state
    _runtime.Clear();
    // Re-inject messages into the runtime...

    Ui?.SetLoading(false);
}
```

---

### `RespondToToolApprovalAsync()` — Approval Resolution

```csharp
async Task RespondToToolApprovalAsync(string requestId, bool approved)
{
    if (_pendingApprovals.TryRemove(requestId, out ToolApprovalRequest? request))
    {
        request.Completion.SetResult(approved);
    }
}
```

The `TaskCompletionSource` pattern means the controller's tool execution flow is paused until the user responds. When `SetResult(true)` is called, the awaiting code in the runtime continues and executes the tool. When `SetResult(false)`, the tool is skipped.

---

## Custom Controller Implementations 

The `IChatUiController` interface is **not tied to local `ChatRuntime` usage**. Consumers can implement their own controllers for different scenarios:

### Example: HTTP Proxy Controller

A WASM app can't run `ChatRuntime` in-process. Instead, it proxies to a remote API:

```csharp
public class HttpChatController : IChatUiController
{
    private readonly HttpClient _http;

    public IChatUi? Ui { get; set; }

    public HttpChatController(HttpClient http) => _http = http;

    public async Task InitializeAsync()
    {
        // Fetch models + agents from API
        var models = await _http.GetFromJsonAsync<List<ChatUiModel>>("/api/models");
        var agents = await _http.GetFromJsonAsync<List<ChatUiAgent>>("/api/agents");

        Ui?.SetModels(models ?? []);
        Ui?.SetAgents(agents ?? []);
    }

    public async Task SendMessageAsync(string text, List<ChatUiFile>? files)
    {
        Ui?.AddMessage(new ChatUiMessage { Role = ChatUiRole.User, Content = text });

        string streamingId = Guid.NewGuid().ToString();
        Ui?.StartStreamingMessage(streamingId);

        // Use SSE or SignalR for streaming from the API
        await foreach (var chunk in StreamFromApi(text, files))
        {
            if (chunk.Type == "token")
                Ui?.AppendStreamingToken(streamingId, chunk.Content);
            else if (chunk.Type == "tool_invoked")
                Ui?.AddEventChip(/* ... */);
        }

        Ui?.CompleteStreamingMessage(streamingId);
    }

    // ... other methods similarly proxy to HTTP ...

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

### Example: Mock Controller for Testing

```csharp
public class MockChatController : IChatUiController
{
    public IChatUi? Ui { get; set; }

    public async Task InitializeAsync()
    {
        Ui?.SetModels([
            new() { Id = "mock-model", DisplayName = "Mock Model", Provider = "Test" }
        ]);
        Ui?.SetAgents([
            new() { Name = "default", Description = "Test agent" }
        ]);
    }

    public async Task SendMessageAsync(string text, List<ChatUiFile>? files)
    {
        Ui?.AddMessage(new ChatUiMessage { Role = ChatUiRole.User, Content = text });

        string id = "mock-stream";
        Ui?.StartStreamingMessage(id);

        // Simulate streaming with delays
        foreach (string word in $"Echo: {text}".Split(' '))
        {
            await Task.Delay(100);
            Ui?.AppendStreamingToken(id, word + " ");
        }

        Ui?.CompleteStreamingMessage(id);
    }

    public Task CancelAsync() => Task.CompletedTask;
    public Task SelectModelAsync(string modelId) => Task.CompletedTask;
    public Task SelectAgentAsync(string? agentName) => Task.CompletedTask;
    public Task LoadConversationAsync(string id) => Task.CompletedTask;
    public Task NewConversationAsync() { Ui?.Clear(); return Task.CompletedTask; }
    public Task DeleteConversationAsync(string id) => Task.CompletedTask;
    public Task RespondToToolApprovalAsync(string id, bool approved) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

---

## Usage in a Blazor Page

```razor
@page "/chat"
@inject IChatUiController ChatController
@implements IAsyncDisposable

<TornadoChatPanel Controller="ChatController" ShowSidebar="true" />

@code {
    public async ValueTask DisposeAsync()
    {
        await ChatController.DisposeAsync();
    }
}
```

That's it. The page is a one-liner. All complexity lives in the controller and components.
