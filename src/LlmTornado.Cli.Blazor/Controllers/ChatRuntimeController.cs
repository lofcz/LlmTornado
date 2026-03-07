using System.Collections.Concurrent;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Blazor.Controllers;

/// <summary>
/// Default IChatUiController implementation that wires LlmTornado.Cli.Core
/// infrastructure to the Blazor chat UI via the IChatUi interface.
/// 
/// This controller runs the AI runtime in-process — suitable for Blazor Server.
/// For Blazor WASM, implement IChatUiController with an HTTP proxy instead.
/// 
/// Split across partial files:
///   - ChatRuntimeController.cs          → fields, ctor, lifecycle, model/agent selection
///   - ChatRuntimeController.Chat.cs     → SendMessageAsync, CancelAsync, message building
///   - ChatRuntimeController.Events.cs   → runtime event handling
///   - ChatRuntimeController.Conversations.cs → conversation CRUD
///   - ChatRuntimeController.ToolApproval.cs  → IToolApproval + approval UI flow
///   - ChatRuntimeController.Helpers.cs  → settings, mapping, utilities
/// </summary>
public sealed partial class ChatRuntimeController : IChatUiController, ISettingsController, IToolApproval, ISettingsPersistence
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

    // Track whether paths were explicitly set (vs. defaulting to CWD-relative).
    // When changing working directory, only re-resolve paths that were not explicit.
    private bool _skillsDirExplicit;
    private bool _agentsDirExplicit;
    private bool _mcpPathExplicit;

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

            // Track which paths were explicitly configured (not defaulting to CWD-relative)
            _skillsDirExplicit = _options.SkillsDirectory is not null;
            _agentsDirExplicit = _options.AgentsDirectory is not null;
            _mcpPathExplicit = _options.McpConfigPath is not null;

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
            string? globalSkillsDir = _options.GlobalSkillsDirectory
                ?? SkillLoader.ResolveGlobalSkillsDirectory();
            _skillManager.LoadSkills(skillsDir, globalSkillsDir);

            // 7. Initialize MCP (global + local)
            _mcpLoader = new McpConfigLoader();
            string? mcpPath = McpConfigLoader.ResolveMcpConfigPath(_options.McpConfigPath);
            string? globalMcpPath = _options.GlobalMcpConfigPath is not null
                ? (File.Exists(_options.GlobalMcpConfigPath) ? _options.GlobalMcpConfigPath : null)
                : McpConfigLoader.ResolveGlobalMcpConfigPath();
            await _mcpLoader.LoadAsync(mcpPath, globalMcpPath);

            // 8. Initialize agents (built-in + global + local)
            _agentManager = new AgentDefinitionManager(_settings, this);
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string cwd = _options.WorkingDirectory ?? Environment.CurrentDirectory;
            string? globalAgentsDir = _options.GlobalAgentsDirectory
                ?? AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
            _agentManager.LoadAll(builtInDir, globalAgentsDir, agentsDir, cwd);

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
    // Disposal
    // ─────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_mcpLoader is not null)
            await _mcpLoader.DisposeAsync();
    }
}
