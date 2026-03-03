# Stage 6: Blazor UI Components

## Goal

Build the Blazor `.razor` components that make up the chat UI. All components use **plain Blazor** with scoped CSS — no MudBlazor dependency. Each component is a focused, single-responsibility piece that the main `TornadoChatPanel` composes together.

## Component Hierarchy

```
TornadoChatPanel (main container, implements IChatUi)
├── Header bar
│   ├── <select> — Model dropdown
│   └── <select> — Agent dropdown
├── ConversationSidebar (optional, toggle-able)
│   ├── "New Chat" button
│   └── Conversation list items
├── Message area (scrollable)
│   └── foreach message:
│       ├── ChatMessageBubble
│       │   ├── User content + file chips
│       │   └── Assistant content (markdown rendered)
│       ├── foreach chip in message.EventChips:
│       │   └── ChatEventChip
│       └── ToolApprovalBanner (if pending)
├── FileAttachmentBar (staged files above input)
│   └── File chips with remove button
└── Input area
    ├── <textarea> — Message input
    ├── File attach <button> + hidden <InputFile>
    ├── Send <button>
    └── Cancel <button> (visible during streaming)
```

---

## 6.1: `TornadoChatPanel.razor` — Main Container

This is the primary component consumers embed in their pages. It implements `IChatUi` and delegates user actions to the `IChatUiController`.

**Path:** `src/LlmTornado.Cli.Blazor/Components/TornadoChatPanel.razor`

```razor
@using Markdig
@using LlmTornado.Cli.Blazor.Models
@implements IChatUi
@implements IAsyncDisposable

<div class="tornado-chat @(_showSidebar ? "tornado-chat--with-sidebar" : "")">
    @* ── Sidebar ── *@
    @if (ShowSidebar)
    {
        <ConversationSidebar Conversations="_conversations"
                             ActiveConversationId="_currentConversationId"
                             OnNewChat="HandleNewChat"
                             OnSelectConversation="HandleSelectConversation"
                             OnDeleteConversation="HandleDeleteConversation"
                             Visible="_showSidebar" />
    }

    <div class="tornado-chat__main">
        @* ── Header bar with dropdowns ── *@
        <div class="tornado-chat__header">
            <div class="tornado-chat__header-left">
                <select class="tornado-chat__select"
                        value="@_selectedModelId"
                        @onchange="HandleModelChange">
                    <option value="" disabled>Select model...</option>
                    @foreach (var group in _models.GroupBy(m => m.Provider))
                    {
                        <optgroup label="@group.Key">
                            @foreach (var model in group)
                            {
                                <option value="@model.Id"
                                        disabled="@(!model.IsAvailable)">
                                    @model.DisplayName
                                </option>
                            }
                        </optgroup>
                    }
                </select>
            </div>

            <div class="tornado-chat__header-right">
                <select class="tornado-chat__select"
                        value="@_selectedAgentName"
                        @onchange="HandleAgentChange">
                    <option value="">Default (no persona)</option>
                    @foreach (var agent in _agents)
                    {
                        <option value="@agent.Name">
                            @agent.Name @(agent.HasCapabilityCuration ? "⚙" : "")
                        </option>
                    }
                </select>

                @if (ShowSidebar)
                {
                    <button class="tornado-chat__btn tornado-chat__btn--icon"
                            @onclick="() => _showSidebar = !_showSidebar"
                            title="Toggle sidebar">
                        ☰
                    </button>
                }
            </div>
        </div>

        @* ── Message area ── *@
        <div class="tornado-chat__messages" @ref="_messageContainer">
            @if (_isLoading)
            {
                <div class="tornado-chat__loading">
                    <div class="tornado-chat__spinner"></div>
                    <span>Loading...</span>
                </div>
            }
            else if (_messages.Count == 0)
            {
                <div class="tornado-chat__empty">
                    <p>Start a conversation by typing a message below.</p>
                </div>
            }
            else
            {
                @foreach (var msg in _messages)
                {
                    <ChatMessageBubble Message="msg" />

                    @foreach (var chip in msg.EventChips)
                    {
                        <ChatEventChip Chip="chip" />
                    }
                }
            }

            @if (_pendingApproval is not null)
            {
                <ToolApprovalBanner Request="_pendingApproval"
                                    OnRespond="HandleToolApprovalResponse" />
            }
        </div>

        @* ── File attachment bar ── *@
        @if (_stagedFiles.Count > 0)
        {
            <FileAttachmentBar Files="_stagedFiles"
                               OnRemoveFile="HandleRemoveFile" />
        }

        @* ── Input area ── *@
        <div class="tornado-chat__input-area">
            <label class="tornado-chat__btn tornado-chat__btn--icon"
                   title="Attach files">
                📎
                <InputFile OnChange="HandleFileUpload"
                           multiple
                           accept=".png,.jpg,.jpeg,.gif,.webp,.pdf,.wav,.mp3"
                           style="display:none" />
            </label>

            <textarea class="tornado-chat__textarea"
                      @bind="_inputText"
                      @bind:event="oninput"
                      @onkeydown="HandleKeyDown"
                      placeholder="Type a message... (Enter to send, Shift+Enter for newline)"
                      rows="2"
                      disabled="@_isSending"></textarea>

            @if (_isSending)
            {
                <button class="tornado-chat__btn tornado-chat__btn--cancel"
                        @onclick="HandleCancel"
                        title="Cancel">
                    ⬜
                </button>
            }
            else
            {
                <button class="tornado-chat__btn tornado-chat__btn--send"
                        @onclick="HandleSend"
                        disabled="@(string.IsNullOrWhiteSpace(_inputText) && _stagedFiles.Count == 0)"
                        title="Send message">
                    ➤
                </button>
            }
        </div>
    </div>
</div>
```

**Code-behind:** `TornadoChatPanel.razor` `@code` block:

```csharp
@code {
    // ── Parameters ──
    [Parameter] public IChatUiController Controller { get; set; } = default!;
    [Parameter] public bool ShowSidebar { get; set; } = true;
    [Parameter] public int StreamingRenderIntervalMs { get; set; } = 50;

    // ── State ──
    private readonly List<ChatUiMessage> _messages = [];
    private readonly Dictionary<string, ChatUiMessage> _streamingMessages = new();
    private List<ChatUiModel> _models = [];
    private List<ChatUiAgent> _agents = [];
    private List<ChatUiConversation> _conversations = [];
    private List<ChatUiFile> _stagedFiles = [];
    private string? _selectedModelId;
    private string? _selectedAgentName;
    private string? _currentConversationId;
    private string _inputText = string.Empty;
    private bool _isLoading;
    private bool _isSending;
    private bool _showSidebar = true;
    private ToolApprovalRequest? _pendingApproval;
    private ElementReference _messageContainer;

    // Streaming debounce
    private DateTime _lastRender = DateTime.MinValue;
    private bool _renderPending;

    // ── Lifecycle ──

    protected override async Task OnInitializedAsync()
    {
        Controller.Ui = this;
        await Controller.InitializeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Auto-scroll to bottom after messages change
        // (requires a small JS interop for scrolling)
    }

    // ── IChatUi Implementation ──

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
            _isSending = true;
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

                // Debounce renders for performance
                if ((DateTime.UtcNow - _lastRender).TotalMilliseconds >= StreamingRenderIntervalMs)
                {
                    _lastRender = DateTime.UtcNow;
                    StateHasChanged();
                }
                else if (!_renderPending)
                {
                    _renderPending = true;
                    _ = Task.Delay(StreamingRenderIntervalMs).ContinueWith(_ =>
                    {
                        InvokeAsync(() =>
                        {
                            _renderPending = false;
                            _lastRender = DateTime.UtcNow;
                            StateHasChanged();
                        });
                    });
                }
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
            }
            _isSending = false;
            StateHasChanged();
        });
    }

    public void AddEventChip(ChatUiEventChip chip)
    {
        InvokeAsync(() =>
        {
            var target = _streamingMessages.Values.LastOrDefault()
                ?? _messages.LastOrDefault(m => m.Role == ChatUiRole.Assistant);
            target?.EventChips.Add(chip);
            StateHasChanged();
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

    public void ShowToolApproval(ToolApprovalRequest request)
    {
        InvokeAsync(() =>
        {
            _pendingApproval = request;
            StateHasChanged();
        });
    }

    public void SetModels(List<ChatUiModel> models)
    {
        InvokeAsync(() => { _models = models; StateHasChanged(); });
    }

    public void SetSelectedModel(string modelId)
    {
        InvokeAsync(() => { _selectedModelId = modelId; StateHasChanged(); });
    }

    public void SetAgents(List<ChatUiAgent> agents)
    {
        InvokeAsync(() => { _agents = agents; StateHasChanged(); });
    }

    public void SetSelectedAgent(string? agentName)
    {
        InvokeAsync(() => { _selectedAgentName = agentName; StateHasChanged(); });
    }

    public void SetConversations(List<ChatUiConversation> conversations)
    {
        InvokeAsync(() => { _conversations = conversations; StateHasChanged(); });
    }

    public void SetLoading(bool loading)
    {
        InvokeAsync(() => { _isLoading = loading; StateHasChanged(); });
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

    // ── Event handlers (UI → Controller) ──

    private async Task HandleSend()
    {
        if (string.IsNullOrWhiteSpace(_inputText) && _stagedFiles.Count == 0) return;

        string text = _inputText;
        List<ChatUiFile> files = [.. _stagedFiles];
        _inputText = string.Empty;
        _stagedFiles.Clear();

        await Controller.SendMessageAsync(text, files.Count > 0 ? files : null);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await HandleSend();
        }
    }

    private async Task HandleCancel()
    {
        await Controller.CancelAsync();
    }

    private async Task HandleModelChange(ChangeEventArgs e)
    {
        string? modelId = e.Value?.ToString();
        if (!string.IsNullOrEmpty(modelId))
            await Controller.SelectModelAsync(modelId);
    }

    private async Task HandleAgentChange(ChangeEventArgs e)
    {
        string? agentName = e.Value?.ToString();
        await Controller.SelectAgentAsync(
            string.IsNullOrEmpty(agentName) ? null : agentName);
    }

    private async Task HandleNewChat()
    {
        _currentConversationId = null;
        await Controller.NewConversationAsync();
    }

    private async Task HandleSelectConversation(string conversationId)
    {
        _currentConversationId = conversationId;
        await Controller.LoadConversationAsync(conversationId);
    }

    private async Task HandleDeleteConversation(string conversationId)
    {
        await Controller.DeleteConversationAsync(conversationId);
    }

    private void HandleToolApprovalResponse(bool approved)
    {
        if (_pendingApproval is not null)
        {
            _ = Controller.RespondToToolApprovalAsync(_pendingApproval.Id, approved);
            _pendingApproval = null;
            StateHasChanged();
        }
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(10))
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            _stagedFiles.Add(new ChatUiFile
            {
                FileName = file.Name,
                MimeType = file.ContentType,
                Content = ms.ToArray()
            });
        }
        StateHasChanged();
    }

    private void HandleRemoveFile(ChatUiFile file)
    {
        _stagedFiles.Remove(file);
        StateHasChanged();
    }

    // ── Disposal ──

    public async ValueTask DisposeAsync()
    {
        await Controller.DisposeAsync();
    }
}
```

---

## 6.2: `ChatMessageBubble.razor` — Message Bubble

Renders a single message with role-based styling.

**Path:** `src/LlmTornado.Cli.Blazor/Components/ChatMessageBubble.razor`

```razor
@using Markdig

<div class="chat-bubble @RoleClass @(Message.IsError ? "chat-bubble--error" : "")">
    <div class="chat-bubble__content">
        @if (Message.Role == ChatUiRole.User)
        {
            <p>@Message.Content</p>

            @if (Message.Files.Count > 0)
            {
                <div class="chat-bubble__files">
                    @foreach (var file in Message.Files)
                    {
                        <span class="chat-bubble__file-chip">
                            @FileIcon(file) @file.FileName
                            <span class="chat-bubble__file-size">(@file.FormattedSize)</span>
                        </span>
                    }
                </div>
            }
        }
        else
        {
            @* Assistant messages rendered as markdown *@
            <div class="chat-bubble__markdown">
                @((MarkupString)Markdown.ToHtml(Message.Content, Pipeline))
            </div>

            @if (Message.IsStreaming)
            {
                <span class="chat-bubble__cursor">▊</span>
            }
        }
    </div>

    <div class="chat-bubble__meta">
        <span class="chat-bubble__time">
            @Message.Timestamp.ToLocalTime().ToString("HH:mm")
        </span>
    </div>
</div>

@code {
    [Parameter] public ChatUiMessage Message { get; set; } = default!;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private string RoleClass => Message.Role switch
    {
        ChatUiRole.User => "chat-bubble--user",
        ChatUiRole.System => "chat-bubble--system",
        _ => "chat-bubble--assistant"
    };

    private static string FileIcon(ChatUiFile file) => file switch
    {
        { IsImage: true } => "🖼",
        { IsDocument: true } => "📄",
        { IsAudio: true } => "🎵",
        _ => "📎"
    };
}
```

---

## 6.3: `ChatEventChip.razor` — Inline Event Chip

Compact pill showing tool name + status, expandable to show details.

**Path:** `src/LlmTornado.Cli.Blazor/Components/ChatEventChip.razor`

```razor
<div class="event-chip @StatusClass @TypeClass"
     @onclick="() => _expanded = !_expanded">

    <span class="event-chip__icon">@StatusIcon</span>
    <span class="event-chip__title">@Chip.Title</span>

    @if (Chip.Status == ChatUiChipStatus.InProgress)
    {
        <span class="event-chip__spinner"></span>
    }
</div>

@if (_expanded && !string.IsNullOrEmpty(Chip.Detail))
{
    <div class="event-chip__detail">
        <pre>@Chip.Detail</pre>
    </div>
}

@code {
    [Parameter] public ChatUiEventChip Chip { get; set; } = default!;

    private bool _expanded;

    private string StatusClass => Chip.Status switch
    {
        ChatUiChipStatus.InProgress => "event-chip--in-progress",
        ChatUiChipStatus.Completed => "event-chip--completed",
        ChatUiChipStatus.Failed => "event-chip--failed",
        _ => ""
    };

    private string TypeClass => Chip.Type switch
    {
        ChatUiChipType.ToolInvoked or ChatUiChipType.ToolCompleted => "event-chip--tool",
        ChatUiChipType.ToolDenied => "event-chip--denied",
        ChatUiChipType.Error => "event-chip--error",
        ChatUiChipType.Reasoning => "event-chip--reasoning",
        ChatUiChipType.Info => "event-chip--info",
        _ => ""
    };

    private string StatusIcon => Chip.Status switch
    {
        ChatUiChipStatus.InProgress => "⟳",
        ChatUiChipStatus.Completed => "✓",
        ChatUiChipStatus.Failed => "✗",
        _ => "•"
    };
}
```

---

## 6.4: `ToolApprovalBanner.razor` — Approval Prompt

Inline banner that appears when a tool needs user permission.

**Path:** `src/LlmTornado.Cli.Blazor/Components/ToolApprovalBanner.razor`

```razor
<div class="tool-approval">
    <div class="tool-approval__header">
        <span class="tool-approval__icon">🔒</span>
        <span class="tool-approval__title">
            Tool <strong>@Request.ToolName</strong> requires permission
        </span>
    </div>

    <p class="tool-approval__message">@Request.RequestMessage</p>

    @if (!string.IsNullOrEmpty(Request.Arguments))
    {
        <details class="tool-approval__args">
            <summary>Arguments</summary>
            <pre>@Request.Arguments</pre>
        </details>
    }

    <div class="tool-approval__actions">
        <button class="tornado-chat__btn tornado-chat__btn--approve"
                @onclick="() => OnRespond.InvokeAsync(true)">
            ✓ Allow
        </button>
        <button class="tornado-chat__btn tornado-chat__btn--deny"
                @onclick="() => OnRespond.InvokeAsync(false)">
            ✗ Deny
        </button>
    </div>
</div>

@code {
    [Parameter] public ToolApprovalRequest Request { get; set; } = default!;
    [Parameter] public EventCallback<bool> OnRespond { get; set; }
}
```

---

## 6.5: `FileAttachmentBar.razor` — Staged Files

Shows files staged for upload above the input area.

**Path:** `src/LlmTornado.Cli.Blazor/Components/FileAttachmentBar.razor`

```razor
<div class="file-bar">
    @foreach (var file in Files)
    {
        <span class="file-bar__chip">
            @if (file.IsImage)
            {
                <img class="file-bar__preview"
                     src="data:@file.MimeType;base64,@file.Base64"
                     alt="@file.FileName" />
            }
            else
            {
                <span class="file-bar__icon">
                    @(file.IsDocument ? "📄" : file.IsAudio ? "🎵" : "📎")
                </span>
            }
            <span class="file-bar__name">@file.FileName</span>
            <span class="file-bar__size">@file.FormattedSize</span>
            <button class="file-bar__remove"
                    @onclick="() => OnRemoveFile.InvokeAsync(file)"
                    title="Remove">
                ✗
            </button>
        </span>
    }
</div>

@code {
    [Parameter] public List<ChatUiFile> Files { get; set; } = [];
    [Parameter] public EventCallback<ChatUiFile> OnRemoveFile { get; set; }
}
```

---

## 6.6: `ConversationSidebar.razor` — Session List

**Path:** `src/LlmTornado.Cli.Blazor/Components/ConversationSidebar.razor`

```razor
<aside class="conversation-sidebar @(Visible ? "" : "conversation-sidebar--hidden")">
    <button class="tornado-chat__btn tornado-chat__btn--primary conversation-sidebar__new-btn"
            @onclick="OnNewChat">
        + New Chat
    </button>

    <div class="conversation-sidebar__list">
        @foreach (var convo in Conversations)
        {
            <div class="conversation-sidebar__item @(convo.Id == ActiveConversationId ? "conversation-sidebar__item--active" : "")"
                 @onclick="() => OnSelectConversation.InvokeAsync(convo.Id)">
                <div class="conversation-sidebar__title">@convo.DisplayTitle</div>
                <div class="conversation-sidebar__meta">
                    @convo.UpdatedAt.ToLocalTime().ToString("MMM d, HH:mm")
                    · @convo.MessageCount msgs
                </div>
                <button class="conversation-sidebar__delete"
                        @onclick:stopPropagation="true"
                        @onclick="() => OnDeleteConversation.InvokeAsync(convo.Id)"
                        title="Delete conversation">
                    🗑
                </button>
            </div>
        }

        @if (Conversations.Count == 0)
        {
            <div class="conversation-sidebar__empty">
                <p>No conversations yet</p>
            </div>
        }
    </div>
</aside>

@code {
    [Parameter] public List<ChatUiConversation> Conversations { get; set; } = [];
    [Parameter] public string? ActiveConversationId { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public EventCallback OnNewChat { get; set; }
    [Parameter] public EventCallback<string> OnSelectConversation { get; set; }
    [Parameter] public EventCallback<string> OnDeleteConversation { get; set; }
}
```

---

## Rendering Flow Example

Here's what happens visually when the user sends "What tools do you have?":

```
┌─ TornadoChatPanel ──────────────────────────────────────┐
│                                                          │
│  ┌─ Header ────────────────────────────────────────────┐ │
│  │ [Claude 4.6 Opus ▾]              [default ▾]  [☰]  │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                          │
│  ┌─ Messages ──────────────────────────────────────────┐ │
│  │                                                      │ │
│  │  ┌──────────────────────────── User ┐                │ │
│  │  │ What tools do you have?          │                │ │
│  │  └──────────────────────────────────┘                │ │
│  │                                                      │ │
│  │  ┌─ Assistant ──────────────────────────────────┐    │ │
│  │  │ I have access to several tools:              │    │ │
│  │  │                                              │    │ │
│  │  │  [✓ list_skills]  ← click to expand results │    │ │
│  │  │                                              │    │ │
│  │  │ Here are the skills and tools available...   │    │ │
│  │  └──────────────────────────────────────────────┘    │ │
│  │                                                      │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                          │
│  ┌─ Input ─────────────────────────────────────────────┐ │
│  │ [📎] [Type a message...                      ] [➤]  │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### Why native `<select>` instead of a custom dropdown?

- **Zero JavaScript**: native selects work everywhere without JS
- **Accessibility**: built-in keyboard navigation, screen reader support
- **Simplicity**: `<optgroup>` natively groups models by provider
- The demo app (Stage 8) can style these with more flair; the library keeps it functional

### Why `InvokeAsync` wrapper on every IChatUi method?

Blazor Server dispatches events on the circuit's synchronization context, but the AI runtime fires callbacks on background threads (from HTTP response streaming, MCP stdio, etc.). `InvokeAsync` marshals the call to the render context. It's a no-op on WASM (single-threaded) but essential for Server.

### Why streaming render debounce?

Fast models can emit 50+ tokens/second. Calling `StateHasChanged()` on every token would thrash the Blazor diff engine (on Server, each diff is a SignalR message). The `StreamingRenderIntervalMs` parameter (default 50ms = ~20fps) batches token appends between renders, providing smooth streaming without excessive network traffic.
