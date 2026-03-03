# Stage 5: ChatRuntimeController — Default Controller Implementation

## Goal

Implement `ChatRuntimeController`, the default `IChatUiController` that wires all `LlmTornado.Cli.Core` infrastructure to the UI. This is the "brains" — it takes user actions from the UI, drives the AI runtime, and pushes results back through `IChatUi`.

## Architecture Position

```
TornadoChatPanel (IChatUi)
        │
        ▼ user actions
ChatRuntimeController (IChatUiController)
        │
        ├── ProviderDetector          → API key detection, TornadoApi creation
        ├── AgentBuilder              → TornadoAgent + ChatRuntime assembly
        ├── AgentDefinitionManager    → Persona discovery and selection
        ├── SkillManager              → Skill discovery, enable/disable
        ├── McpConfigLoader           → MCP tool server connections
        ├── ConversationStore         → Session persistence (file-based)
        ├── FileAttachmentResolver    → File → ChatMessagePart mapping
        └── IToolApproval (self)      → Interactive approval via UI
```

## Configuration Options

**Path:** `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeControllerOptions.cs`

```csharp
namespace LlmTornado.Cli.Blazor.Controllers;

/// <summary>
/// Configuration for ChatRuntimeController.
/// All paths are optional — the controller uses sensible defaults
/// (environment variables, standard directories) when not specified.
/// </summary>
public sealed class ChatRuntimeControllerOptions
{
    /// <summary>
    /// Directory for conversation persistence files.
    /// Default: {AppData}/llmtornado/conversations/
    /// </summary>
    public string? ConversationsDirectory { get; set; }

    /// <summary>
    /// Directory for project-local skills.
    /// Default: ./skills/ (from CWD)
    /// </summary>
    public string? SkillsDirectory { get; set; }

    /// <summary>
    /// Directory for global skills.
    /// Default: TORNADO_SKILLS_DIR env var → {AppData}/llmtornado/skills/
    /// </summary>
    public string? GlobalSkillsDirectory { get; set; }

    /// <summary>
    /// Directory for custom agent persona .md files.
    /// Default: ./agents/ (from CWD)
    /// </summary>
    public string? AgentsDirectory { get; set; }

    /// <summary>
    /// Path to mcp.json config file.
    /// Default: TORNADO_MCP_CONFIG env var → ./mcp.json (from CWD)
    /// </summary>
    public string? McpConfigPath { get; set; }

    /// <summary>
    /// Override working directory for the agent's system prompt.
    /// Default: Environment.CurrentDirectory
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Optional API key overrides. Keys are environment variable names
    /// (e.g., "OPENAI_API_KEY"), values are the API keys.
    /// These are set as environment variables before provider detection,
    /// supplementing (not replacing) existing env vars.
    /// </summary>
    public Dictionary<string, string>? ApiKeyOverrides { get; set; }

    /// <summary>
    /// Path for persisting AgentSettings JSON.
    /// Default: {AppData}/llmtornado/settings.json
    /// </summary>
    public string? SettingsPath { get; set; }

    /// <summary>
    /// Additional tools to register beyond skills and MCP tools.
    /// Allows the host app to inject app-specific tools.
    /// </summary>
    public List<LlmTornado.Common.Tool>? AdditionalTools { get; set; }
}
```

## Full Controller Implementation

**Path:** `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.cs`

```csharp
using System.Collections.Concurrent;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Input;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Cli.Blazor.Controllers;

/// <summary>
/// Default IChatUiController implementation that wires LlmTornado.Cli.Core
/// infrastructure to the Blazor chat UI via the IChatUi interface.
/// 
/// This controller runs the AI runtime in-process — suitable for Blazor Server.
/// For Blazor WASM, implement IChatUiController with an HTTP proxy instead.
/// </summary>
public sealed class ChatRuntimeController : IChatUiController, IToolApproval, ISettingsPersistence
{
    private readonly ChatRuntimeControllerOptions _options;

    // Cli.Core components
    private TornadoApi? _api;
    private AgentSettings _settings = new();
    private SkillManager? _skillManager;
    private McpConfigLoader? _mcpLoader;
    private AgentDefinitionManager? _agentManager;
    private AgentBuilder? _agentBuilder;
    private ChatRuntime? _runtime;
    private ConversationStore? _conversationStore;

    // Provider state
    private ProviderDetectionResult? _detectionResult;
    private List<ChatModel> _allModels = [];

    // Tool approval
    private readonly ConcurrentDictionary<string, ToolApprovalRequest> _pendingApprovals = new();
    private readonly HashSet<string> _preApprovedTools = new(StringComparer.OrdinalIgnoreCase);

    // Conversation state
    private string? _currentConversationId;
    private string? _currentStreamingId;

    // Settings persistence path
    private string _settingsPath = string.Empty;

    public IChatUi? Ui { get; set; }

    public ChatRuntimeController(ChatRuntimeControllerOptions? options = null)
    {
        _options = options ?? new();
    }

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (Ui is null) throw new InvalidOperationException("Ui must be set before initialization");

        Ui.SetLoading(true);

        try
        {
            // 1. Apply API key overrides to environment
            ApplyApiKeyOverrides();

            // 2. Resolve paths
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "llmtornado");
            string conversationsDir = _options.ConversationsDirectory
                ?? Path.Combine(appData, "conversations");
            string skillsDir = _options.SkillsDirectory ?? Path.GetFullPath("skills");
            string agentsDir = _options.AgentsDirectory ?? Path.GetFullPath("agents");
            _settingsPath = _options.SettingsPath
                ?? Path.Combine(appData, "settings.json");

            // 3. Load settings
            _settings = LoadSettings();

            // 4. Detect providers
            _detectionResult = ProviderDetector.Detect();
            if (_detectionResult is null)
            {
                Ui.SetModels([]);
                Ui.SetAgents([]);
                Ui.SetLoading(false);
                return; // No API keys configured — UI shows empty state
            }

            _api = _detectionResult.Api;
            _allModels = _detectionResult.Providers.SelectMany(p => p.Models).ToList();

            // 5. Populate model dropdown
            List<ChatUiModel> uiModels = _detectionResult.Providers.SelectMany(p =>
                p.Models.Select(m => new ChatUiModel
                {
                    Id = m.Name,
                    DisplayName = m.Name,
                    Provider = p.Provider.ToString(),
                    IsAvailable = true
                })).ToList();
            Ui.SetModels(uiModels);

            // Restore or use detected active model
            ChatModel activeModel = _detectionResult.ActiveModel;
            if (_settings.ActiveModel is not null)
            {
                ChatModel? saved = _allModels.FirstOrDefault(m => m.Name == _settings.ActiveModel);
                if (saved is not null) activeModel = saved;
            }
            Ui.SetSelectedModel(activeModel.Name);

            // 6. Initialize skills
            _skillManager = new SkillManager(_settings, this);
            _skillManager.LoadSkills(skillsDir, _options.GlobalSkillsDirectory);

            // 7. Initialize MCP
            _mcpLoader = new McpConfigLoader();
            string? mcpPath = McpConfigLoader.ResolveMcpConfigPath(_options.McpConfigPath);
            if (mcpPath is not null)
            {
                await _mcpLoader.LoadAsync(mcpPath);
            }

            // 8. Initialize agents
            _agentManager = new AgentDefinitionManager(_settings, this);
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string cwd = _options.WorkingDirectory ?? Environment.CurrentDirectory;
            _agentManager.LoadAll(builtInDir, agentsDir, cwd);

            List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
                .Where(a => a.IsPersona)
                .Select(MapAgent)
                .ToList();
            Ui.SetAgents(uiAgents);
            Ui.SetSelectedAgent(_agentManager.ActivePersonaName);

            // 9. Build the agent
            _agentBuilder = new AgentBuilder(
                _api, activeModel, _skillManager, _mcpLoader,
                this, _agentManager, _settings,
                _detectionResult.OptimizerModel,
                _options.AdditionalTools);
            if (_options.WorkingDirectory is not null)
                _agentBuilder.WorkingDirectory = _options.WorkingDirectory;

            _runtime = _agentBuilder.Build(HandleRuntimeEvent);

            // 10. Load conversations
            _conversationStore = new ConversationStore(conversationsDir);
            RefreshConversationList();
        }
        finally
        {
            Ui.SetLoading(false);
        }
    }

    // ─────────────────────────────────────────────
    // Chat actions
    // ─────────────────────────────────────────────

    public async Task SendMessageAsync(string text, List<ChatUiFile>? files)
    {
        if (_runtime is null || Ui is null) return;

        // 1. Display user message
        var userMsg = new ChatUiMessage
        {
            Role = ChatUiRole.User,
            Content = text,
            Files = files ?? []
        };
        Ui.AddMessage(userMsg);

        // 2. Build internal ChatMessage (with file attachments if any)
        ChatMessage chatMessage = BuildChatMessage(text, files);

        // 3. Start streaming response
        string streamingId = Guid.NewGuid().ToString();
        _currentStreamingId = streamingId;
        Ui.StartStreamingMessage(streamingId);

        try
        {
            // 4. Tool optimization if needed
            if (_agentBuilder!.NeedsOptimization)
            {
                await _agentBuilder.OptimizeToolsForTurn(text);
            }

            // 5. Invoke the runtime
            ChatMessage response = await _runtime.InvokeAsync(chatMessage);

            // 6. Restore full tools after optimization
            if (_agentBuilder.NeedsOptimization)
            {
                _agentBuilder.RestoreFullTools();
            }

            // 7. Finalize streaming
            Ui.CompleteStreamingMessage(streamingId);

            // 8. Ensure conversation ID exists
            _currentConversationId ??= Guid.NewGuid().ToString();

            // 9. Save/update conversation
            List<ChatMessage> allMessages = _runtime.RuntimeConfiguration.GetMessages();
            string? modelName = _agentBuilder.ActiveModel.Name;
            List<string> activeSkills = _skillManager?.GetEnabledSkills()
                .Select(s => s.Name).ToList() ?? [];

            if (_conversationStore!.Exists(_currentConversationId))
            {
                _conversationStore.Update(_currentConversationId, allMessages, modelName);
            }
            else
            {
                _conversationStore.Save(_currentConversationId, allMessages,
                    null, modelName, activeSkills);
            }

            RefreshConversationList();
        }
        catch (Exception ex)
        {
            Ui.CompleteStreamingMessage(streamingId);
            Ui.AddMessage(new ChatUiMessage
            {
                Role = ChatUiRole.Assistant,
                Content = $"**Error:** {ex.Message}",
                IsError = true
            });
        }
        finally
        {
            _currentStreamingId = null;
        }
    }

    public Task CancelAsync()
    {
        _runtime?.CancelExecution();

        if (_currentStreamingId is not null && Ui is not null)
        {
            Ui.CompleteStreamingMessage(_currentStreamingId);
            _currentStreamingId = null;
        }

        // Cancel any pending tool approvals
        foreach (var kvp in _pendingApprovals)
        {
            kvp.Value.Completion.TrySetResult(false);
        }
        _pendingApprovals.Clear();

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // Model / Agent selection
    // ─────────────────────────────────────────────

    public Task SelectModelAsync(string modelId)
    {
        ChatModel? model = _allModels.FirstOrDefault(m => m.Name == modelId);
        if (model is null || _agentBuilder is null) return Task.CompletedTask;

        _runtime = _agentBuilder.SetModel(model, HandleRuntimeEvent);
        _settings.ActiveModel = modelId;
        SaveSettings(_settings);
        Ui?.SetSelectedModel(modelId);

        return Task.CompletedTask;
    }

    public Task SelectAgentAsync(string? agentName)
    {
        if (_agentManager is null || _agentBuilder is null) return Task.CompletedTask;

        if (agentName is null)
            _agentManager.ClearActivePersona();
        else
            _agentManager.SetActivePersona(agentName);

        _runtime = _agentBuilder.RebuildForAgentChange(HandleRuntimeEvent);
        Ui?.SetSelectedAgent(agentName);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // Conversation management
    // ─────────────────────────────────────────────

    public Task LoadConversationAsync(string conversationId)
    {
        if (_conversationStore is null || Ui is null) return Task.CompletedTask;

        Ui.SetLoading(true);
        Ui.Clear();

        List<ChatMessage>? messages = _conversationStore.Load(conversationId);
        if (messages is null)
        {
            Ui.SetLoading(false);
            return Task.CompletedTask;
        }

        _currentConversationId = conversationId;

        // Restore runtime state
        _runtime?.Clear();

        // Map and display each message
        foreach (ChatMessage msg in messages)
        {
            Ui.AddMessage(MapToChatUiMessage(msg));
        }

        Ui.SetLoading(false);
        return Task.CompletedTask;
    }

    public Task NewConversationAsync()
    {
        _currentConversationId = null;
        _runtime?.Clear();
        Ui?.Clear();
        return Task.CompletedTask;
    }

    public Task DeleteConversationAsync(string conversationId)
    {
        _conversationStore?.Delete(conversationId);

        if (_currentConversationId == conversationId)
        {
            _currentConversationId = null;
            _runtime?.Clear();
            Ui?.Clear();
        }

        RefreshConversationList();
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // Tool approval
    // ─────────────────────────────────────────────

    public Task RespondToToolApprovalAsync(string requestId, bool approved)
    {
        if (_pendingApprovals.TryRemove(requestId, out ToolApprovalRequest? request))
        {
            request.Completion.SetResult(approved);
        }
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // IToolApproval implementation
    // ─────────────────────────────────────────────

    void IToolApproval.PreApproveSkillTools(IEnumerable<string> toolNames)
    {
        foreach (string name in toolNames)
            _preApprovedTools.Add(name);
    }

    bool IToolApproval.IsAutoApproved(string toolName)
    {
        return _preApprovedTools.Contains(toolName);
    }

    async ValueTask<bool> IToolApproval.HandleToolPermissionRequest(string requestMessage)
    {
        // If auto-approved, allow immediately
        // (The runtime calls this for every tool — we check our pre-approved set)

        if (Ui is null) return true; // No UI bound → auto-approve

        // Create an approval request with a TaskCompletionSource
        var request = new ToolApprovalRequest
        {
            ToolName = ExtractToolName(requestMessage),
            RequestMessage = requestMessage
        };

        _pendingApprovals[request.Id] = request;

        // Push to UI
        Ui.ShowToolApproval(request);

        // Await user's decision (blocks this async flow until approved/denied)
        return await request.Completion.Task;
    }

    // ─────────────────────────────────────────────
    // ISettingsPersistence implementation
    // ─────────────────────────────────────────────

    void ISettingsPersistence.SaveSettings(AgentSettings settings)
    {
        SaveSettings(settings);
    }

    // ─────────────────────────────────────────────
    // Runtime event handling
    // ─────────────────────────────────────────────

    private ValueTask HandleRuntimeEvent(ChatRuntimeEvents evt)
    {
        if (Ui is null || _currentStreamingId is null) return ValueTask.CompletedTask;

        if (evt is ChatRuntimeAgentRunnerEvents agentEvt)
        {
            switch (agentEvt.AgentRunnerEvent)
            {
                case AgentRunnerStreamingEvent streaming:
                    HandleStreamingEvent(streaming);
                    break;

                case AgentRunnerToolInvokedEvent toolInvoked:
                    Ui.AddEventChip(new ChatUiEventChip
                    {
                        Id = toolInvoked.ToolCalled.Id ?? Guid.NewGuid().ToString(),
                        Type = ChatUiChipType.ToolInvoked,
                        Title = toolInvoked.ToolCalled.Name,
                        Detail = toolInvoked.ToolCalled.Arguments ?? "",
                        Status = ChatUiChipStatus.InProgress
                    });
                    break;

                case AgentRunnerToolCompletedEvent toolCompleted:
                    string chipId = toolCompleted.ToolCall.Id ?? "";
                    string resultText = toolCompleted.ToolResult?.Output ?? "(no output)";
                    if (resultText.Length > 2000)
                        resultText = resultText[..2000] + "\n[TRUNCATED]";

                    Ui.UpdateEventChip(chipId, new ChatUiEventChip
                    {
                        Id = chipId,
                        Type = ChatUiChipType.ToolCompleted,
                        Title = toolCompleted.ToolCall.Name,
                        Detail = $"Arguments:\n{toolCompleted.ToolCall.Arguments}\n\nResult:\n{resultText}",
                        Status = ChatUiChipStatus.Completed
                    });
                    break;

                case AgentRunnerErrorEvent error:
                    Ui.AddEventChip(new ChatUiEventChip
                    {
                        Type = ChatUiChipType.Error,
                        Title = "Error",
                        Detail = error.ErrorMessage,
                        Status = ChatUiChipStatus.Failed
                    });
                    break;

                case AgentRunnerUsageReceivedEvent usage:
                    Ui.AddEventChip(new ChatUiEventChip
                    {
                        Type = ChatUiChipType.Info,
                        Title = "Usage",
                        Detail = $"Input: {usage.InputTokens} | Output: {usage.OutputTokens} | Total: {usage.TokenUsageAmount}",
                        Status = ChatUiChipStatus.Completed
                    });
                    break;
            }
        }
        else if (evt is ChatRuntimeErrorEvent runtimeError)
        {
            Ui.AddEventChip(new ChatUiEventChip
            {
                Type = ChatUiChipType.Error,
                Title = "Runtime Error",
                Detail = runtimeError.Exception.Message,
                Status = ChatUiChipStatus.Failed
            });
        }

        return ValueTask.CompletedTask;
    }

    private void HandleStreamingEvent(AgentRunnerStreamingEvent streaming)
    {
        switch (streaming.ModelStreamingEvent)
        {
            case ModelStreamingOutputTextDeltaEvent delta:
                Ui!.AppendStreamingToken(_currentStreamingId!, delta.DeltaText ?? "");
                break;

            case ModelStreamingReasoningPartAddedEvent:
                Ui!.AddEventChip(new ChatUiEventChip
                {
                    Type = ChatUiChipType.Reasoning,
                    Title = "Thinking",
                    Status = ChatUiChipStatus.InProgress
                });
                break;

            case ModelStreamingFailedEvent failed:
                Ui!.AddEventChip(new ChatUiEventChip
                {
                    Type = ChatUiChipType.Error,
                    Title = "Stream Failed",
                    Detail = failed.ErrorMessage ?? "Unknown streaming error",
                    Status = ChatUiChipStatus.Failed
                });
                break;
        }
    }

    // ─────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────

    private ChatMessage BuildChatMessage(string text, List<ChatUiFile>? files)
    {
        if (files is null || files.Count == 0)
        {
            return new ChatMessage(ChatMessageRoles.User, text);
        }

        // Build multipart message with file attachments
        List<ChatMessagePart> parts = [];

        foreach (ChatUiFile file in files)
        {
            string base64 = file.Base64;

            if (file.IsImage)
            {
                string dataUri = $"data:{file.MimeType};base64,{base64}";
                parts.Add(new ChatMessagePart(dataUri, LlmTornado.Images.ImageDetail.Auto, file.MimeType));
            }
            else if (file.IsDocument)
            {
                parts.Add(new ChatMessagePart(new ChatDocument(base64)));
            }
            else if (file.IsAudio)
            {
                ChatAudioFormats format = file.MimeType switch
                {
                    "audio/wav" => ChatAudioFormats.Wav,
                    "audio/mpeg" or "audio/mp3" => ChatAudioFormats.Mp3,
                    _ => ChatAudioFormats.Wav,
                };
                parts.Add(new ChatMessagePart(file.Content, format));
            }
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(new ChatMessagePart(text));
        }

        return new ChatMessage(ChatMessageRoles.User, parts);
    }

    private ChatUiMessage MapToChatUiMessage(ChatMessage msg)
    {
        ChatUiRole role = msg.Role switch
        {
            ChatMessageRoles.User => ChatUiRole.User,
            ChatMessageRoles.System => ChatUiRole.System,
            _ => ChatUiRole.Assistant
        };

        return new ChatUiMessage
        {
            Role = role,
            Content = msg.Content ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };
    }

    private static ChatUiAgent MapAgent(AgentDefinition def) => new()
    {
        Name = def.Name,
        Description = def.Description,
        Source = def.Source switch
        {
            AgentSource.BuiltIn => ChatUiAgentSource.BuiltIn,
            AgentSource.Global => ChatUiAgentSource.Global,
            AgentSource.Custom => ChatUiAgentSource.Custom,
            AgentSource.Project => ChatUiAgentSource.Project,
            _ => ChatUiAgentSource.Custom
        },
        HasCapabilityCuration = def.HasCapabilityCuration
    };

    private void RefreshConversationList()
    {
        if (_conversationStore is null || Ui is null) return;

        List<ChatUiConversation> convos = _conversationStore.List()
            .Select(meta => new ChatUiConversation
            {
                Id = meta.Id,
                Label = meta.Label,
                Preview = meta.FirstMessagePreview,
                UpdatedAt = meta.UpdatedAt,
                MessageCount = meta.MessageCount
            })
            .ToList();

        Ui.SetConversations(convos);
    }

    private void ApplyApiKeyOverrides()
    {
        if (_options.ApiKeyOverrides is null) return;

        foreach (var (envVar, apiKey) in _options.ApiKeyOverrides)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                Environment.SetEnvironmentVariable(envVar, apiKey);
            }
        }
    }

    private AgentSettings LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                string json = File.ReadAllText(_settingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AgentSettings>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }
        return new();
    }

    private void SaveSettings(AgentSettings settings)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsPath);
            if (dir is not null) Directory.CreateDirectory(dir);

            string json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Settings persistence failure is non-fatal
        }
    }

    private static string ExtractToolName(string requestMessage)
    {
        // Request messages follow the pattern "Tool 'name' wants to..."
        int start = requestMessage.IndexOf('\'');
        int end = requestMessage.IndexOf('\'', start + 1);
        if (start >= 0 && end > start)
            return requestMessage[(start + 1)..end];
        return "unknown-tool";
    }

    // ─────────────────────────────────────────────
    // Disposal
    // ─────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_mcpLoader is not null)
            await _mcpLoader.DisposeAsync();
    }
}
```

## How Each Cli.Core Component Is Used

### ProviderDetector → Model Population

```
ProviderDetector.Detect()
    ├── Scans 12 env vars: OPENAI_API_KEY, ANTHROPIC_API_KEY, ...
    ├── Builds TornadoApi with all ProviderAuthentication entries
    ├── Selects ActiveModel by priority: Anthropic → OpenAi → Google → ...
    └── Selects OptimizerModel (cheap/fast): Google Flash → OpenAi Mini → ...

Controller maps DetectedProvider.Models → List<ChatUiModel> for the dropdown
```

### AgentBuilder → Runtime Assembly

The controller creates one `AgentBuilder` during init and calls `Build()`, `SetModel()`, or `RebuildForAgentChange()` as the user changes settings. The builder handles all the complexity:

1. Builds system prompt from persona instructions + skill catalog + CWD
2. Collects tools from built-in + scripts + MCP + additional
3. Filters tools through persona capability curation
4. Sets up tool permission requirements
5. Creates `SingletonRuntimeConfiguration` → `ChatRuntime`

### IToolApproval — The Bridge to UI

The controller implements `IToolApproval` itself. When the runtime asks for permission to use a tool:

```
Runtime: "May I use read_file?"
    │
    ▼
IToolApproval.HandleToolPermissionRequest("Tool 'read_file' wants to...")
    │
    ├── Is 'read_file' in _preApprovedTools? → return true immediately
    │
    ├── Otherwise:
    │   ├── Create ToolApprovalRequest with TaskCompletionSource
    │   ├── _pendingApprovals[id] = request
    │   ├── Ui.ShowToolApproval(request)    → UI shows approve/deny banner
    │   └── await request.Completion.Task   → BLOCKS here until user responds
    │
    ▼
User clicks "Approve" → UI calls Controller.RespondToToolApprovalAsync(id, true)
    │
    ├── _pendingApprovals.TryRemove(id, out request)
    └── request.Completion.SetResult(true)  → unblocks the await above
```

### ConversationStore — File Persistence

```
Conversations directory: %APPDATA%/llmtornado/conversations/
    ├── {id}.jsonl          ← Messages in JSONL format (one JSON per line)
    └── {id}.meta.json      ← Metadata: id, label, userId, timestamps, preview

Controller calls:
    _conversationStore.Save(id, messages, userId, model, skills)    ← new conversation
    _conversationStore.Update(id, messages, model)                   ← append messages
    _conversationStore.List()                                        ← sidebar population
    _conversationStore.Load(id)                                      ← restore session
    _conversationStore.Delete(id)                                    ← delete session
```

## DI Registration Pattern

In the consumer's `Program.cs`:

```csharp
// Option 1: Register as scoped (per-circuit in Blazor Server)
builder.Services.AddScoped<IChatUiController>(sp =>
    new ChatRuntimeController(new ChatRuntimeControllerOptions
    {
        WorkingDirectory = "D:\\MyProject",
        ApiKeyOverrides = new()
        {
            ["ANTHROPIC_API_KEY"] = builder.Configuration["Anthropic:ApiKey"]!
        }
    }));

// Option 2: Simple defaults (everything from env vars)
builder.Services.AddScoped<IChatUiController, ChatRuntimeController>();
```

## Error Handling Strategy

| Scenario | Behavior |
|---|---|
| No API keys configured | `InitializeAsync` sets empty models/agents, UI shows "No providers configured" |
| MCP server connection fails | `McpConfigLoader` records error in `ServerStatuses`, tools from that server are skipped |
| Tool execution fails | `AgentRunnerErrorEvent` → error chip displayed inline |
| Streaming fails mid-response | `CompleteStreamingMessage` called, error message added |
| Conversation file corrupt | `ConversationStore.Load` returns null, UI shows "Failed to load" |
| Cancel during streaming | `CancelExecution()` → pending approvals auto-denied, streaming message finalized |
