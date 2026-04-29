using LlmTornado.Acp;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Common;

namespace LlmTornado.Acp.Server;

/// <summary>
/// Concrete ACP runtime implementation powered by the shared Cli.Core agent infrastructure.
/// Uses <see cref="ProviderDetectionResult"/> for multi-provider support.
/// CLI agent personas (default, architect, code-reviewer, debugger, docs-writer) become ACP modes.
/// Filesystem tools are injected as <c>additionalTools</c> into the Core <see cref="AgentBuilder"/>.
/// </summary>
public class TornadoAcpRuntime : BaseAcpTornadoRuntimeConfiguration
{
    private readonly ProviderDetectionResult _detection;

    /// <summary>
    /// All agent persona definitions loaded at startup (used as ACP modes).
    /// </summary>
    private readonly List<AgentDefinition> _personas;

    /// <summary>
    /// Session metadata key for the stored <see cref="AgentBuilder"/>.
    /// </summary>
    private const string MetaAgentBuilder = "agentBuilder";

    /// <summary>
    /// Session metadata key for the stored <see cref="AgentDefinitionManager"/>.
    /// </summary>
    private const string MetaAgentManager = "agentManager";

    /// <summary>
    /// Creates a new ACP runtime backed by Core agent infrastructure.
    /// </summary>
    internal TornadoAcpRuntime(ProviderDetectionResult detection)
        : base("LlmTornado", "1.0.0")
    {
        _detection = detection;

        // Load persona definitions once at startup for mode discovery
        string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string customDir = AgentDefinitionLoader.ResolveAgentsDirectory(null);
        string globalAgentsDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();

        _personas = AgentDefinitionLoader.DiscoverPersonaAgents(builtInDir, globalAgentsDir, customDir);
        Console.Error.WriteLine($"[ACP] Loaded {_personas.Count} agent persona(s): {string.Join(", ", _personas.Select(p => p.Name))}");
    }

    #region BaseAcpTornadoRuntimeConfiguration overrides

    /// <inheritdoc />
    protected override IRuntimeConfiguration CreateRuntimeConfiguration(AcpNewSessionRequest request, string modeId, string modelId)
    {
        return BuildSessionRuntime(request.Cwd, modeId, modelId, out _, out _);
    }

    /// <inheritdoc />
    protected override string GetInitialMode(AcpNewSessionRequest request) => "default";

    /// <inheritdoc />
    protected override string GetInitialModel(AcpNewSessionRequest request)
    {
        return _detection.ActiveModel.Name ?? _detection.AllModels.FirstOrDefault()?.Name ?? "unknown";
    }

    /// <inheritdoc />
    protected override AcpAgentCapabilities DescribeCapabilities()
    {
        return new AcpAgentCapabilities
        {
            LoadSession = false,
            SessionCapabilities = new AcpSessionCapabilities
            {
                SetMode = true,
                SetConfigOption = true
            },
            PromptCapabilities = new AcpPromptCapabilities
            {
                Image = false,
                Audio = false,
                EmbeddedContext = true
            }
        };
    }

    /// <inheritdoc />
    public override Task<AcpNewSessionResponse> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        string initialMode = GetInitialMode(request);
        string initialModel = GetInitialModel(request);

        IRuntimeConfiguration config = BuildSessionRuntime(
            request.Cwd, initialMode, initialModel,
            out AgentBuilder builder, out AgentDefinitionManager agentManager);

        AcpSessionContext ctx = new(config, request.Cwd)
        {
            CurrentModeId = initialMode,
            CurrentModelId = initialModel
        };

        // Store Core components in session metadata for mode/model changes
        ctx.Metadata[MetaAgentBuilder] = builder;
        ctx.Metadata[MetaAgentManager] = agentManager;

        // Wire runtime events
        string capturedSessionId = sessionId;
        ctx.Agent.RuntimeConfiguration.OnRuntimeEvent += async (evt) =>
        {
            await HandleRuntimeEvent(capturedSessionId, evt);
        };

        // Register the session
        RegisterSession(sessionId, ctx);

        AcpNewSessionResponse response = new()
        {
            SessionId = sessionId,
            Modes = new AcpSessionModeState
            {
                CurrentModeId = initialMode,
                AvailableModes = BuildAvailableModes()
            },
            ConfigOptions = BuildConfigOptions(ctx)
        };

        Console.Error.WriteLine($"[ACP] New session: {sessionId} (cwd: {request.Cwd}, model: {initialModel}, mode: {initialMode})");

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public override Task<AcpSetSessionModeResponse> SetModeAsync(AcpSetSessionModeRequest request, CancellationToken cancellationToken)
    {
        AcpSessionContext? ctx = GetSessionContext(request.SessionId);
        if (ctx is null)
            return Task.FromResult(new AcpSetSessionModeResponse());

        // Validate mode exists
        if (!_personas.Exists(p => string.Equals(p.Name, request.ModeId, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(new AcpSetSessionModeResponse());

        // Get stored components
        if (ctx.Metadata.TryGetValue(MetaAgentManager, out object? managerObj) && managerObj is AgentDefinitionManager agentManager
            && ctx.Metadata.TryGetValue(MetaAgentBuilder, out object? builderObj) && builderObj is AgentBuilder builder)
        {
            // Switch persona
            if (string.Equals(request.ModeId, "default", StringComparison.OrdinalIgnoreCase))
                agentManager.ClearActivePersona();
            else
                agentManager.SetActivePersona(request.ModeId);

            // Rebuild with new persona
            ChatRuntime newRuntime = builder.RebuildForAgentChange(async evt =>
            {
                await HandleRuntimeEvent(request.SessionId, evt);
            });

            ctx.CurrentModeId = request.ModeId;
            ctx.RuntimeConfig = newRuntime.RuntimeConfiguration;
            ctx.Agent = newRuntime;
        }

        Console.Error.WriteLine($"[ACP] Session {request.SessionId} mode changed to: {request.ModeId}");
        return Task.FromResult(new AcpSetSessionModeResponse());
    }

    /// <inheritdoc />
    public override Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken)
    {
        AcpSessionContext? ctx = GetSessionContext(request.SessionId);
        if (ctx is null)
            return Task.FromResult(new AcpSetSessionConfigOptionResponse());

        if (request.ConfigId == "model")
        {
            // Find the model in available models
            ChatModel? targetModel = _detection.AllModels.FirstOrDefault(m => m.Name == request.Value);
            if (targetModel is not null && ctx.Metadata.TryGetValue(MetaAgentBuilder, out object? builderObj) && builderObj is AgentBuilder builder)
            {
                ChatRuntime newRuntime = builder.SetModel(targetModel, async evt =>
                {
                    await HandleRuntimeEvent(request.SessionId, evt);
                });

                ctx.CurrentModelId = request.Value;
                ctx.RuntimeConfig = newRuntime.RuntimeConfiguration;
                ctx.Agent = newRuntime;

                Console.Error.WriteLine($"[ACP] Session {request.SessionId} model changed to: {request.Value}");
            }
        }

        return Task.FromResult(new AcpSetSessionConfigOptionResponse
        {
            ConfigOptions = BuildConfigOptions(ctx)
        });
    }

    #endregion

    #region Session runtime factory

    /// <summary>
    /// Build a complete session runtime from Core infrastructure.
    /// </summary>
    private IRuntimeConfiguration BuildSessionRuntime(
        string cwd, string modeId, string modelId,
        out AgentBuilder builder, out AgentDefinitionManager agentManager)
    {
        // Per-session settings (no persistence for ACP)
        AgentSettings sessionSettings = new();
        AcpSettingsPersistence persistence = new();
        AcpToolApproval toolApproval = new();

        // Initialize skill manager (skills from global + CWD/skills/)
        SkillManager skillManager = new(sessionSettings, persistence);
        string skillsDir = SkillLoader.ResolveSkillsDirectory(null);
        string globalSkillsDir = SkillLoader.ResolveGlobalSkillsDirectory();
        skillManager.LoadSkills(skillsDir, globalSkillsDir);

        // Initialize agent definition manager
        agentManager = new AgentDefinitionManager(sessionSettings, persistence);
        string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string customDir = AgentDefinitionLoader.ResolveAgentsDirectory(null);
        string globalAgentsDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
        agentManager.LoadAll(builtInDir, globalAgentsDir, customDir, cwd);

        // Apply mode as active persona
        if (!string.Equals(modeId, "default", StringComparison.OrdinalIgnoreCase))
            agentManager.SetActivePersona(modeId);

        agentManager.ApplyCapabilityBaseline(skillManager, toolApproval);

        // Resolve the model
        ChatModel model = ResolveModel(modelId);

        // MCP config loader (empty — ACP doesn't use local MCP config)
        McpConfigLoader mcpLoader = new();

        // Build filesystem tools scoped to CWD
        List<Tool> fsTools = BuildAcpLocalTools(cwd);

        // Create the builder
        builder = new AgentBuilder(
            _detection.Api,
            model,
            skillManager,
            mcpLoader,
            toolApproval,
            null,
            agentManager,
            sessionSettings,
            _detection.OptimizerModel,
            fsTools);

        builder.WorkingDirectory = cwd;

        // Build and extract the runtime configuration
        ChatRuntime runtime = builder.Build();
        return runtime.RuntimeConfiguration;
    }

    /// <summary>
    /// Resolve a model ID (name string) to a <see cref="ChatModel"/> from detected providers.
    /// Falls back to the detection's active model.
    /// </summary>
    private ChatModel ResolveModel(string modelId)
    {
        ChatModel? match = _detection.AllModels.FirstOrDefault(m => m.Name == modelId);
        return match ?? _detection.ActiveModel;
    }

    #endregion

    #region Mode / model discovery

    /// <summary>
    /// Builds the ACP mode list from loaded agent personas.
    /// </summary>
    private List<AcpSessionMode> BuildAvailableModes()
    {
        // Stable ordering: default first, then alphabetical
        List<AcpSessionMode> modes =
        [
            new AcpSessionMode
            {
                Id = "default",
                Name = "Agent",
                Description = "General-purpose coding assistant with all capabilities"
            }
        ];

        foreach (AgentDefinition persona in _personas.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(persona.Name, "default", StringComparison.OrdinalIgnoreCase))
                continue; // Already added as the first mode

            modes.Add(new AcpSessionMode
            {
                Id = persona.Name,
                Name = FormatDisplayName(persona.Name),
                Description = persona.Description.Length > 0 ? persona.Description : $"Agent persona: {persona.Name}"
            });
        }

        return modes;
    }

    /// <summary>
    /// Build model config options from detected providers, grouped by provider.
    /// </summary>
    private List<AcpSessionConfigOption> BuildConfigOptions(AcpSessionContext ctx)
    {
        List<AcpSessionConfigSelectGroup> groups = [];

        foreach (DetectedProvider provider in _detection.Providers)
        {
            List<AcpSessionConfigSelectOption> options = provider.Models
                .Select(m => new AcpSessionConfigSelectOption
                {
                    Value = m.Name ?? m.ToString() ?? "unknown",
                    Name = m.Name ?? m.ToString() ?? "unknown",
                    Description = $"{provider.Provider} model"
                })
                .ToList();

            if (options.Count > 0)
            {
                groups.Add(new AcpSessionConfigSelectGroup
                {
                    Group = provider.Provider.ToString().ToLowerInvariant(),
                    Name = provider.Provider.ToString(),
                    Options = options
                });
            }
        }

        return
        [
            new AcpSessionConfigOption
            {
                Id = "model",
                Name = "Model",
                Description = "The LLM model to use for completions",
                Type = "select",
                Category = "model",
                CurrentValue = ctx.CurrentModelId,
                Options = groups
            }
        ];
    }

    #endregion

    #region Filesystem tools

    internal static List<Tool> BuildAcpLocalTools(string cwd)
    {
        string acpRoot = ResolveAcpRootPath(cwd);

        return
        [
            new Tool(
                (string relativePath) => ListDirectory(acpRoot, relativePath),
                "list_dir",
                "Lists files and folders under the working directory for a relative path."
            ),
            new Tool(
                (string query, string includePattern, int maxResults) => SearchFiles(acpRoot, query, includePattern, maxResults),
                "search_files",
                "Searches for text in files under the working directory. includePattern accepts globs like *.cs or *.*."
            ),
            new Tool(
                (string relativePath, int startLine, int endLine) => ReadFileRange(acpRoot, relativePath, startLine, endLine),
                "read_file",
                "Reads a range of lines from a file in the working directory."
            ),
            new Tool(
                (string relativePath, string content) => WriteFile(acpRoot, relativePath, content),
                "write_file",
                "Writes full file content to a file in the working directory. Creates folders as needed."
            ),
            new Tool(
                (string relativePath, string oldText, string newText) => ReplaceInFile(acpRoot, relativePath, oldText, newText),
                "replace_in_file",
                "Replaces exact text in a file in the working directory."
            )
        ];
    }

    internal static string ResolveAcpRootPath(string cwd)
    {
        string current = Path.GetFullPath(cwd);
        string nested = Path.GetFullPath(Path.Combine(current, "src", "LlmTornado.Acp"));

        if (Directory.Exists(nested))
        {
            return nested;
        }

        if (string.Equals(Path.GetFileName(current), "LlmTornado.Acp", StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        string sibling = Path.GetFullPath(Path.Combine(current, "LlmTornado.Acp"));

        return Directory.Exists(sibling) ? sibling : current;
    }

    private static string ResolveFilePath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Path must be relative to the working directory.");
        }

        string full = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path escapes the working directory.");
        }

        return full;
    }

    private static object SearchFiles(string root, string query, string includePattern, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new { error = "query is required" };
        }

        string pattern = string.IsNullOrWhiteSpace(includePattern) ? "*.*" : includePattern;
        int take = Math.Clamp(maxResults <= 0 ? 20 : maxResults, 1, 200);
        List<object> results = [];

        foreach (string file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            string[] lines;

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(new
                {
                    path = Path.GetRelativePath(root, file).Replace('\\', '/'),
                    line = i + 1,
                    text = lines[i]
                });

                if (results.Count >= take)
                {
                    return new { root, count = results.Count, results };
                }
            }
        }

        return new { root, count = results.Count, results };
    }

    private static object ListDirectory(string root, string relativePath)
    {
        string normalizedRelativePath = string.IsNullOrWhiteSpace(relativePath) ? "." : relativePath;
        string targetPath = normalizedRelativePath is "."
            ? root
            : ResolveFilePath(root, normalizedRelativePath);

        if (!Directory.Exists(targetPath))
        {
            return new
            {
                ok = false,
                error = "directory not found",
                path = normalizedRelativePath
            };
        }

        List<string> entries = [];

        foreach (string directory in Directory.EnumerateDirectories(targetPath))
        {
            entries.Add(Path.GetFileName(directory) + "/");
        }

        foreach (string file in Directory.EnumerateFiles(targetPath))
        {
            entries.Add(Path.GetFileName(file));
        }

        return new
        {
            ok = true,
            path = Path.GetRelativePath(root, targetPath).Replace('\\', '/'),
            count = entries.Count,
            entries
        };
    }

    private static object ReadFileRange(string root, string relativePath, int startLine, int endLine)
    {
        string path = ResolveFilePath(root, relativePath);

        if (!File.Exists(path))
        {
            return new { error = "file not found", path = relativePath };
        }

        string[] lines = File.ReadAllLines(path);
        int from = Math.Max(1, startLine);
        int to = Math.Min(lines.Length, endLine <= 0 ? lines.Length : endLine);

        if (from > to)
        {
            return new { error = "invalid line range", from, to, totalLines = lines.Length };
        }

        List<object> result = [];

        for (int i = from; i <= to; i++)
        {
            result.Add(new { line = i, text = lines[i - 1] });
        }

        return new
        {
            path = Path.GetRelativePath(root, path).Replace('\\', '/'),
            from,
            to,
            totalLines = lines.Length,
            lines = result
        };
    }

    private static object WriteFile(string root, string relativePath, string content)
    {
        string path = ResolveFilePath(root, relativePath);
        string? dir = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, content ?? string.Empty);

        return new
        {
            ok = true,
            path = Path.GetRelativePath(root, path).Replace('\\', '/'),
            bytesWritten = content?.Length ?? 0
        };
    }

    private static object ReplaceInFile(string root, string relativePath, string oldText, string newText)
    {
        string path = ResolveFilePath(root, relativePath);

        if (!File.Exists(path))
        {
            return new { ok = false, error = "file not found", path = relativePath };
        }

        if (string.IsNullOrEmpty(oldText))
        {
            return new { ok = false, error = "oldText must not be empty" };
        }

        string original = File.ReadAllText(path);
        int count = 0;
        int index = 0;

        while ((index = original.IndexOf(oldText, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += oldText.Length;
        }

        if (count == 0)
        {
            return new { ok = false, error = "text not found", path = relativePath };
        }

        string updated = original.Replace(oldText, newText, StringComparison.Ordinal);
        File.WriteAllText(path, updated);

        return new
        {
            ok = true,
            path = Path.GetRelativePath(root, path).Replace('\\', '/'),
            replacements = count
        };
    }

    #endregion

    #region Event forwarding

    /// <summary>
    /// Handles runtime events and forwards them as ACP session updates.
    /// </summary>
    private async ValueTask HandleRuntimeEvent(string sessionId, ChatRuntimeEvents evt)
    {
        List<AcpSessionUpdate>? updates = evt.ToAcpSessionUpdates();

        if (updates is null || updates.Count == 0)
            return;

        foreach (AcpSessionUpdate update in updates)
        {
            await RaiseSessionUpdate(new AcpSessionNotification
            {
                SessionId = sessionId,
                Update = update
            });
        }
    }

    #endregion

    #region Logging helpers

    /// <summary>
    /// Formats a kebab-case agent name into a title-case display name.
    /// e.g. "code-reviewer" → "Code Reviewer"
    /// </summary>
    private static string FormatDisplayName(string name)
    {
        return string.Join(' ', name.Split('-').Select(word =>
            word.Length > 0 ? char.ToUpperInvariant(word[0]) + word[1..] : word));
    }

    internal static string ExtractTextForLogging(List<AcpContentBlock> blocks)
    {
        List<string> parts = [];

        foreach (AcpContentBlock block in blocks)
        {
            switch (block.Type)
            {
                case AcpContentBlockTypes.Text when block.Text is not null:
                    parts.Add(block.Text);
                    break;
                case AcpContentBlockTypes.Resource when block.Resource?.Text is not null:
                    parts.Add(block.Resource.Text);
                    break;
                case AcpContentBlockTypes.ResourceLink:
                    parts.Add($"[Resource: {block.Name}]({block.Uri})");
                    break;
            }
        }

        return string.Join("\n\n", parts);
    }

    internal static string Truncate(string s, int maxLen)
    {
        string oneLine = s.ReplaceLineEndings(" ");
        return oneLine.Length <= maxLen ? oneLine : string.Concat(oneLine.AsSpan(0, maxLen), "...");
    }

    #endregion
}
