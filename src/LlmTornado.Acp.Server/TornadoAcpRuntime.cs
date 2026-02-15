using LlmTornado.Acp;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Acp.Server;

/// <summary>
/// Concrete ACP runtime implementation backed by LlmTornado's ChatRuntime agent system.
/// Extends <see cref="BaseAcpTornadoRuntimeConfiguration"/> to leverage per-session ChatRuntime
/// with factory-created IRuntimeConfiguration instances.
/// </summary>
public class TornadoAcpRuntime : BaseAcpTornadoRuntimeConfiguration
{
    private readonly TornadoApi _api;
    private readonly string _defaultModel;

    private static readonly List<ModelOption> AvailableModels =
    [
        new("gpt-4.1-nano", "GPT-4.1 Nano", "Fast and cheap, good for simple tasks"),
        new("gpt-4.1-mini", "GPT-4.1 Mini", "Balanced speed and quality"),
        new("gpt-4.1", "GPT-4.1", "High quality, best for complex coding tasks"),
        new("o4-mini", "O4 Mini", "Reasoning model, good for hard problems"),
        new("o3", "O3", "Advanced reasoning model")
    ];

    private static readonly List<AcpSessionMode> AvailableModes =
    [
        new() { Id = "agent", Name = "Agent", Description = "Coding assistant — writes and explains code" },
        new() { Id = "chat", Name = "Chat", Description = "General-purpose conversational assistant" },
        new() { Id = "plan", Name = "Plan", Description = "High-level design and architecture guidance" },
        new() { Id = "refactor", Name = "Refactor", Description = "Automated file refactoring pipeline (Analyze → Plan → Edit → Verify)" }
    ];

    private static readonly Dictionary<string, string> ModeSystemPrompts = new()
    {
        ["agent"] = """
            You are a coding assistant integrated into JetBrains Rider via ACP.
            - Write clean, idiomatic code
            - Provide code in fenced markdown blocks with the language specified
            - Be concise and direct
            - When fixing bugs, explain the root cause briefly
            """,
        ["chat"] = """
            You are a helpful conversational assistant integrated into JetBrains Rider via ACP.
            - Answer questions clearly and concisely
            - Use markdown formatting where appropriate
            - You can discuss code, concepts, tooling, or anything the user asks
            """,
        ["plan"] = """
            You are a software architecture advisor integrated into JetBrains Rider via ACP.
            - Focus on high-level design, patterns, and trade-offs
            - Suggest project structure, abstractions, and technology choices
            - When providing code, keep it to key interfaces and contracts
            - Explain rationale behind architectural decisions
            """,
        ["refactor"] = """
            You are an automated file refactoring agent integrated into JetBrains Rider via ACP.
            - Analyze code structure and identify refactoring opportunities
            - Create detailed refactoring plans before making changes
            - Apply changes precisely using file tools
            - Verify changes compile and preserve behaviour
            """
    };

    public TornadoAcpRuntime(string openAiApiKey, string model = "gpt-4.1-nano")
        : base("LlmTornado", "1.0.0")
    {
        _api = new TornadoApi(openAiApiKey, LLmProviders.OpenAi);
        _defaultModel = model;
    }

    #region BaseAcpTornadoRuntimeConfiguration overrides

    /// <inheritdoc />
    protected override IRuntimeConfiguration CreateRuntimeConfiguration(AcpNewSessionRequest request, string modeId, string modelId)
    {
        if (modeId == "refactor")
        {
            return CreateRefactoringOrchestrationConfiguration(request, modelId);
        }

        return CreateSingletonConfiguration(request, modeId, modelId);
    }

    /// <inheritdoc />
    protected override string GetInitialMode(AcpNewSessionRequest request) => "agent";

    /// <inheritdoc />
    protected override string GetInitialModel(AcpNewSessionRequest request)
    {
        if (AvailableModels.Exists(m => m.Id == _defaultModel))
        {
            return _defaultModel;
        }

        return AvailableModels[0].Id;
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
        // Let the base class create the session with ChatRuntime
        Task<AcpNewSessionResponse> responseTask = base.NewSessionAsync(request, cancellationToken);
        AcpNewSessionResponse response = responseTask.Result;

        // Attach mode and model metadata
        response.Modes = new AcpSessionModeState
        {
            CurrentModeId = "agent",
            AvailableModes = AvailableModes
        };

        Console.Error.WriteLine($"[ACP] New session: {response.SessionId} (cwd: {request.Cwd}, model: {GetInitialModel(request)})");

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public override Task<AcpSetSessionModeResponse> SetModeAsync(AcpSetSessionModeRequest request, CancellationToken cancellationToken)
    {
        if (!AvailableModes.Exists(m => m.Id == request.ModeId))
        {
            return Task.FromResult(new AcpSetSessionModeResponse());
        }

        Console.Error.WriteLine($"[ACP] Session {request.SessionId} mode changed to: {request.ModeId}");
        return base.SetModeAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken)
    {
        AcpSessionContext? ctx = GetSessionContext(request.SessionId);

        if (ctx is null)
        {
            return Task.FromResult(new AcpSetSessionConfigOptionResponse());
        }

        if (request.ConfigId == "model" && AvailableModels.Exists(m => m.Id == request.Value))
        {
            RebuildSessionRuntime(ctx, ctx.CurrentModeId, request.Value);
            Console.Error.WriteLine($"[ACP] Session {request.SessionId} model changed to: {request.Value}");
        }

        return Task.FromResult(new AcpSetSessionConfigOptionResponse
        {
            ConfigOptions = BuildConfigOptions(ctx)
        });
    }

    #endregion

    #region Configuration factories

    private SingletonRuntimeConfiguration CreateSingletonConfiguration(AcpNewSessionRequest request, string modeId, string modelId)
    {
        List<Tool> localTools = BuildAcpLocalTools(request.Cwd);
        string acpRoot = ResolveAcpRootPath(request.Cwd);
        string systemPrompt = ModeSystemPrompts.GetValueOrDefault(modeId, ModeSystemPrompts["agent"]);
        string fullSystemPrompt = $"{systemPrompt}\n\nThe user's current working directory is: {request.Cwd}\nTool access is restricted to: {acpRoot}";

        bool useTools = modeId is "agent" or "plan" or "refactor";

        ChatModel resolvedModel = ResolveModel(modelId);

        TornadoAgent agent = new(
            client: _api,
            model: resolvedModel,
            name: $"ACP-{modeId}",
            instructions: fullSystemPrompt,
            tools: useTools ? localTools.ConvertAll<Delegate>(t => t.Delegate!) : null,
            streaming: true
        );

        return new SingletonRuntimeConfiguration(agent);
    }

    private IRuntimeConfiguration CreateRefactoringOrchestrationConfiguration(AcpNewSessionRequest request, string modelId)
    {
        return new FileRefactoringOrchestrationConfiguration(
            _api,
            ResolveModel(modelId),
            request.Cwd,
            BuildAcpLocalTools(request.Cwd));
    }

    private static ChatModel ResolveModel(string modelId)
    {
        return modelId switch
        {
            "gpt-4.1" => ChatModel.OpenAi.Gpt41.V41,
            "gpt-4.1-mini" => ChatModel.OpenAi.Gpt41.V41Mini,
            "o4-mini" => ChatModel.OpenAi.O4.V4Mini,
            "o3" => ChatModel.OpenAi.O3.V3,
            _ => ChatModel.OpenAi.Gpt41.V41Nano
        };
    }

    #endregion

    #region Config options

    private static List<AcpSessionConfigOption> BuildConfigOptions(AcpSessionContext ctx)
    {
        return
        [
            new AcpSessionConfigOption
            {
                Id = "model",
                Name = "Model",
                Description = "The OpenAI model to use for completions",
                Type = "select",
                Category = "model",
                CurrentValue = ctx.CurrentModelId,
                Options =
                [
                    new AcpSessionConfigSelectGroup
                    {
                        Group = "models",
                        Name = "Models",
                        Options = AvailableModels.ConvertAll(m => new AcpSessionConfigSelectOption
                        {
                            Value = m.Id,
                            Name = m.Name,
                            Description = m.Description
                        })
                    }
                ]
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
                "Lists files and folders under the ACP directory for a relative path."
            ),
            new Tool(
                (string query, string includePattern, int maxResults) => SearchFiles(acpRoot, query, includePattern, maxResults),
                "search_files",
                "Searches for text in files under the ACP directory. includePattern accepts globs like *.cs or *.*."
            ),
            new Tool(
                (string relativePath, int startLine, int endLine) => ReadFileRange(acpRoot, relativePath, startLine, endLine),
                "read_file",
                "Reads a range of lines from a file in the ACP directory."
            ),
            new Tool(
                (string relativePath, string content) => WriteFile(acpRoot, relativePath, content),
                "write_file",
                "Writes full file content to a file in the ACP directory. Creates folders as needed."
            ),
            new Tool(
                (string relativePath, string oldText, string newText) => ReplaceInFile(acpRoot, relativePath, oldText, newText),
                "replace_in_file",
                "Replaces exact text in a file in the ACP directory."
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
            throw new InvalidOperationException("Path must be relative to the ACP directory.");
        }

        string full = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path escapes the ACP directory.");
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

    #region Logging helpers

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

    private record ModelOption(string Id, string Name, string Description);
}