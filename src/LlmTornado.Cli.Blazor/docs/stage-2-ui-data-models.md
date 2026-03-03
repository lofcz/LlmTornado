# Stage 2: UI Data Models

## Goal

Define lightweight, UI-only data transfer objects (DTOs) in `LlmTornado.Cli.Blazor.Models`. These are **deliberately separate** from the internal `LlmTornado` types (`ChatMessage`, `FunctionCall`, `Conversation`, etc.) to ensure:

1. **UI components never depend on internal LLM shapes** — if the Agents library changes its types, only the controller mapping layer needs updating
2. **Serialization-friendly** — simple POCOs that can be JSON-serialized for persistence, logging, or transport over HTTP/SignalR
3. **Blazor-optimized** — minimal footprint, no circular references, designed for rendering

## Architecture Principle

```
┌──────────────────────────────────┐     ┌──────────────────────────────────┐
│  LlmTornado Internal Types       │     │  ChatUI View Models              │
│  (in LlmTornado.Agents)         │ ──► │  (in LlmTornado.Cli.Blazor)     │
│                                  │     │                                  │
│  ChatMessage                     │     │  ChatUiMessage                   │
│  FunctionCall / FunctionResult   │     │  ChatUiEventChip                 │
│  ChatModel                       │     │  ChatUiModel                     │
│  AgentDefinition                 │     │  ChatUiAgent                     │
│  ConversationMetadata            │     │  ChatUiConversation              │
└──────────────────────────────────┘     └──────────────────────────────────┘
           ▲                                         │
           │         The Controller maps             │
           └─────────   between them   ──────────────┘
```

The **controller** (`ChatRuntimeController`) handles the mapping. UI components only ever see the view models.

---

## Model Definitions

### 2.1 `ChatUiMessage`

Represents a single message bubble in the chat. Supports user messages, assistant messages, and system messages.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ChatUiMessage.cs`

```csharp
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
    /// Inline event chips (tool calls, reasoning, etc.) that belong to this message.
    /// Displayed inline within the assistant message bubble.
    /// </summary>
    public List<ChatUiEventChip> EventChips { get; set; } = [];
}
```

**Why `EventChips` is inside `ChatUiMessage`:** Tool calls and reasoning events happen *during* an assistant turn. Displaying them inline within the message bubble (rather than as standalone items in the message list) matches the UX pattern of Claude, ChatGPT, and similar products. The event chips appear between text content as the assistant works.

---

### 2.2 `ChatUiEventChip`

Represents a compact inline indicator for tool invocations, reasoning blocks, errors, or informational events.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ChatUiEventChip.cs`

```csharp
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
```

**Lifecycle example:**

```
1. Tool invoked → AddEventChip(Type=ToolInvoked, Title="read_file", Status=InProgress)
   UI shows: [🔄 read_file]
   
2. Tool completed → UpdateEventChip(id, Status=Completed, Detail="<file contents>")
   UI shows: [✓ read_file] (click to expand results)

3. Tool denied → UpdateEventChip(id, Status=Failed, Type=ToolDenied)
   UI shows: [✗ read_file — denied]
```

---

### 2.3 `ChatUiFile`

Represents a file attachment on a message — either user-uploaded or referenced by the assistant.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ChatUiFile.cs`

```csharp
namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// A file attachment on a chat message.
/// </summary>
public sealed class ChatUiFile
{
    /// <summary>
    /// Display name of the file (e.g., "screenshot.png").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (e.g., "image/png", "application/pdf").
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Raw file content bytes.
    /// </summary>
    public byte[] Content { get; set; } = [];

    /// <summary>
    /// Base64-encoded content for display or transport.
    /// Computed lazily from Content.
    /// </summary>
    public string Base64 => _base64 ??= Convert.ToBase64String(Content);
    private string? _base64;

    /// <summary>
    /// Human-readable file size.
    /// </summary>
    public string FormattedSize => Content.Length switch
    {
        < 1024 => $"{Content.Length} B",
        < 1024 * 1024 => $"{Content.Length / 1024.0:F1} KB",
        _ => $"{Content.Length / (1024.0 * 1024.0):F1} MB"
    };

    /// <summary>
    /// Whether this is an image file (for inline preview rendering).
    /// </summary>
    public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether this is a PDF document.
    /// </summary>
    public bool IsDocument => MimeType == "application/pdf";

    /// <summary>
    /// Whether this is an audio file.
    /// </summary>
    public bool IsAudio => MimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
}
```

**Why bytes + lazy Base64:** In Blazor Server, components share memory with the server process. Storing `byte[]` is memory-efficient, and `Base64` is only computed when the template needs it (e.g., for `<img src="data:...">`). 

---

### 2.4 `ChatUiModel`

Represents a selectable LLM model in the model dropdown.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ChatUiModel.cs`

```csharp
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
```

**Mapping from `ChatModel`:** The controller maps `ChatModel` instances from `ProviderDetector.GetModelsForProvider()` like this:

```csharp
// Inside ChatRuntimeController
ChatUiModel MapModel(ChatModel model, LLmProviders provider) => new()
{
    Id = model.Name,            // e.g., "claude-4.6-opus"
    DisplayName = model.Name,   // ChatModel.Name is already human-readable
    Provider = provider.ToString(),
    IsAvailable = true
};
```

---

### 2.5 `ChatUiAgent`

Represents a selectable agent persona in the agent dropdown.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ChatUiAgent.cs`

```csharp
namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// Source origin of an agent definition, mirroring AgentSource from Cli.Core.
/// </summary>
public enum ChatUiAgentSource
{
    BuiltIn,
    Global,
    Custom,
    Project
}

/// <summary>
/// A selectable agent persona for the agent dropdown.
/// </summary>
public sealed class ChatUiAgent
{
    /// <summary>
    /// Unique name identifier (e.g., "code-reviewer", "debugger").
    /// Used as the value when selecting.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Where this agent was loaded from.
    /// </summary>
    public ChatUiAgentSource Source { get; set; }

    /// <summary>
    /// Whether this agent has custom capability curation
    /// (skill/tool whitelists or blacklists).
    /// </summary>
    public bool HasCapabilityCuration { get; set; }
}
```

**Mapping from `AgentDefinition`:**

```csharp
ChatUiAgent MapAgent(AgentDefinition def) => new()
{
    Name = def.Name,
    Description = def.Description,
    Source = def.Source switch
    {
        AgentSource.BuiltIn => ChatUiAgentSource.BuiltIn,
        AgentSource.Global  => ChatUiAgentSource.Global,
        AgentSource.Custom  => ChatUiAgentSource.Custom,
        AgentSource.Project => ChatUiAgentSource.Project,
        _ => ChatUiAgentSource.Custom
    },
    HasCapabilityCuration = def.HasCapabilityCuration
};
```

---

### 2.6 `ChatUiConversation`

Represents a saved conversation in the sidebar list.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ChatUiConversation.cs`

```csharp
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
```

**Mapping from `ConversationMetadata`:**

```csharp
ChatUiConversation MapConversation(ConversationMetadata meta) => new()
{
    Id = meta.Id,
    Label = meta.Label,
    Preview = meta.FirstMessagePreview,
    UpdatedAt = meta.UpdatedAt,
    MessageCount = meta.MessageCount
};
```

---

### 2.7 `ToolApprovalRequest`

Represents a pending tool approval that the UI must display to the user. Uses `TaskCompletionSource<bool>` for async resolution.

**Path:** `src/LlmTornado.Cli.Blazor/Models/ToolApprovalRequest.cs`

```csharp
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
```

**Flow diagram:**

```
Controller                          UI Component
    │                                    │
    │  tool needs permission             │
    │  ──────────────────────────►       │
    │  ShowToolApproval(request)         │
    │                                    │
    │  await request.Completion.Task     │  Shows approve/deny banner
    │  ◄─────── (blocked) ──────►       │
    │                                    │  User clicks "Approve"
    │                                    │
    │  Completion.SetResult(true)        │
    │  ◄─────────────────────────        │
    │                                    │
    │  continues tool execution          │
    ▼                                    ▼
```

This pattern avoids polling — the controller's async method simply `await`s the `TaskCompletionSource`, and the Blazor component resolves it when the user interacts. It works identically on Server and WASM.

---

## Type Relationship Diagram

```
ChatUiMessage
├── Id: string
├── Role: ChatUiRole (User | Assistant | System)
├── Content: string
├── Files: List<ChatUiFile>
│   └── ChatUiFile
│       ├── FileName, MimeType, Content: byte[]
│       └── Base64 (lazy), IsImage, IsDocument, IsAudio
├── EventChips: List<ChatUiEventChip>
│   └── ChatUiEventChip
│       ├── Id, Type: ChatUiChipType, Title, Detail
│       └── Status: ChatUiChipStatus (InProgress | Completed | Failed)
├── IsStreaming: bool
└── IsError: bool

ChatUiModel (for model dropdown)
├── Id, DisplayName, Provider
└── IsAvailable: bool

ChatUiAgent (for agent dropdown)
├── Name, Description
├── Source: ChatUiAgentSource
└── HasCapabilityCuration: bool

ChatUiConversation (for sidebar)
├── Id, Label, Preview
├── UpdatedAt, MessageCount
└── DisplayTitle (computed)

ToolApprovalRequest (for approval banners)
├── Id, ToolName, RequestMessage, Arguments
└── Completion: TaskCompletionSource<bool>
```

## Verification

After creating all 7 model files:

```bash
cd src
dotnet build LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj
```

All models are simple POCOs with no dependencies on `LlmTornado.*` types — they will compile even if the project reference isn't resolved yet. The controller (Stage 5) handles all mapping.
