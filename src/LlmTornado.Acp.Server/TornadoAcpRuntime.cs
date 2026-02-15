using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Acp.Server;

/// <summary>
/// ACP runtime backed by LlmTornado's Chat API.
/// Each session maintains its own conversation history, mode, and model selection.
/// </summary>
public class TornadoAcpRuntime : IAcpRuntimeConfiguration
{
    private readonly TornadoApi _api;
    private readonly string _defaultModel;
    private readonly Dictionary<string, SessionState> _sessions = new();
    private readonly object _sessionsLock = new();

    public event Func<AcpSessionNotification, Task>? OnSessionUpdate;

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
        new() { Id = "plan", Name = "Plan", Description = "High-level design and architecture guidance" }
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
            """
    };

    public TornadoAcpRuntime(string openAiApiKey, string model = "gpt-4.1-nano")
    {
        _api = new TornadoApi(openAiApiKey, LLmProviders.OpenAi);
        _defaultModel = model;
    }

    public Task<AcpInitializeResponse> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AcpInitializeResponse
        {
            ProtocolVersion = request.ProtocolVersion,
            AgentInfo = new AcpImplementation
            {
                Name = "LlmTornado",
                Version = "1.0.0",
                Title = "LlmTornado ACP Agent"
            },
            AgentCapabilities = new AcpAgentCapabilities
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
            }
        });
    }

    public async Task<AcpNewSessionResponse> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        string initialModel = _defaultModel;

        // Ensure the default model is in our list, otherwise pick the first
        if (!AvailableModels.Exists(m => m.Id == initialModel))
        {
            initialModel = AvailableModels[0].Id;
        }

        List<Tool> localTools = BuildAcpLocalTools(request.Cwd);

        SessionState state = new()
        {
            Cwd = request.Cwd,
            CurrentModeId = "agent",
            CurrentModelId = initialModel,
            LocalTools = localTools,
            Conversation = CreateConversation(initialModel, "agent", request.Cwd, localTools)
        };

        lock (_sessionsLock)
        {
            _sessions[sessionId] = state;
        }

        Console.Error.WriteLine($"[ACP] New session: {sessionId} (cwd: {request.Cwd}, model: {initialModel})");

        return new AcpNewSessionResponse
        {
            SessionId = sessionId,
            Modes = new AcpSessionModeState
            {
                CurrentModeId = state.CurrentModeId,
                AvailableModes = AvailableModes
            }
            // configOptions omitted — Rider 2025.3 has a bug where its Kotlin deserializer
            // wraps SessionConfigSelectOptions in a sealed class requiring JsonObject,
            // but the ACP spec defines it as a plain JsonArray. Both Flat and Grouped
            // variants crash. Track: https://youtrack.jetbrains.com (LLM project, ACP tag)
        };
    }

    public Task<AcpSetSessionModeResponse> SetModeAsync(AcpSetSessionModeRequest request, CancellationToken cancellationToken)
    {
        SessionState? session = GetSession(request.SessionId);

        if (session is null)
        {
            return Task.FromResult(new AcpSetSessionModeResponse());
        }

        if (AvailableModes.Exists(m => m.Id == request.ModeId))
        {
            session.CurrentModeId = request.ModeId;
            session.Conversation = CreateConversation(session.CurrentModelId, session.CurrentModeId, session.Cwd, session.LocalTools);
            Console.Error.WriteLine($"[ACP] Session {request.SessionId} mode changed to: {request.ModeId}");
        }

        return Task.FromResult(new AcpSetSessionModeResponse());
    }

    public Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken)
    {
        SessionState? session = GetSession(request.SessionId);

        if (session is null)
        {
            return Task.FromResult(new AcpSetSessionConfigOptionResponse());
        }

        if (request.ConfigId == "model" && AvailableModels.Exists(m => m.Id == request.Value))
        {
            session.CurrentModelId = request.Value;
            session.Conversation = CreateConversation(session.CurrentModelId, session.CurrentModeId, session.Cwd, session.LocalTools);
            Console.Error.WriteLine($"[ACP] Session {request.SessionId} model changed to: {request.Value}");
        }

        return Task.FromResult(new AcpSetSessionConfigOptionResponse
        {
            ConfigOptions = BuildConfigOptions(session)
        });
    }

    public async Task<AcpPromptResponse> PromptAsync(AcpPromptRequest request, CancellationToken cancellationToken)
    {
        SessionState? session = GetSession(request.SessionId);

        if (session is null)
        {
            AcpNewSessionResponse newSession = await NewSessionAsync(new AcpNewSessionRequest { Cwd = Directory.GetCurrentDirectory() }, cancellationToken);
            session = GetSession(newSession.SessionId);
            request.SessionId = newSession.SessionId;
        }

        string userText = ExtractText(request.Prompt);
        session!.Conversation.AppendUserInput(userText);

        Console.Error.WriteLine($"[ACP] Prompt ({request.SessionId}, {session.CurrentModelId}, {session.CurrentModeId}): {Truncate(userText, 120)}");

        string stopReason = AcpStopReasons.EndTurn;

        try
        {
            await session.Conversation.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = async (token) =>
                {
                    if (OnSessionUpdate is not null)
                    {
                        await OnSessionUpdate.Invoke(new AcpSessionNotification
                        {
                            SessionId = request.SessionId,
                            Update = new AcpSessionUpdate
                            {
                                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                                Content = new AcpContentBlock
                                {
                                    Type = AcpContentBlockTypes.Text,
                                    Text = token
                                }
                            }
                        });
                    }
                },
                FunctionCallHandler = async (functionCalls) =>
                {
                    foreach (LlmTornado.ChatFunctions.FunctionCall call in functionCalls)
                    {
                        try
                        {
                            if (call.Tool?.Delegate is not null)
                            {
                                await call.Invoke(call.Arguments ?? "{}");
                            }
                            else
                            {
                                call.Resolve(new
                                {
                                    error = $"Tool '{call.Name}' is not executable in this ACP runtime."
                                }, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            call.Resolve(new
                            {
                                error = ex.Message
                            }, false);
                        }
                    }
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            stopReason = AcpStopReasons.Cancelled;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ACP] Error during completion: {ex.Message}");

            if (OnSessionUpdate is not null)
            {
                await OnSessionUpdate.Invoke(new AcpSessionNotification
                {
                    SessionId = request.SessionId,
                    Update = new AcpSessionUpdate
                    {
                        SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                        Content = new AcpContentBlock
                        {
                            Type = AcpContentBlockTypes.Text,
                            Text = $"\n\n[Error: {ex.Message}]"
                        }
                    }
                });
            }
        }

        return new AcpPromptResponse
        {
            StopReason = stopReason
        };
    }

    public Task CancelAsync(AcpCancelNotification notification, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"[ACP] Cancel requested for session: {notification.SessionId}");
        return Task.CompletedTask;
    }

    private Conversation CreateConversation(string modelId, string modeId, string cwd, List<Tool>? localTools = null)
    {
        string acpRoot = ResolveAcpRootPath(cwd);

        Conversation conversation = _api.Chat.CreateConversation(new ChatRequest
        {
            Model = modelId,
            Tools = (modeId == "agent" || modeId == "plan") ? localTools : null
        });

        string systemPrompt = ModeSystemPrompts.GetValueOrDefault(modeId, ModeSystemPrompts["agent"]);
        conversation.AppendSystemMessage($"{systemPrompt}\n\nThe user's current working directory is: {cwd}\nTool access is restricted to: {acpRoot}");

        return conversation;
    }

    private static List<Tool> BuildAcpLocalTools(string cwd)
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

    private static string ResolveAcpRootPath(string cwd)
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

    private static List<AcpSessionConfigOption> BuildConfigOptions(SessionState session)
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
                CurrentValue = session.CurrentModelId,
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

    private SessionState? GetSession(string sessionId)
    {
        lock (_sessionsLock)
        {
            _sessions.TryGetValue(sessionId, out SessionState? session);
            return session;
        }
    }

    private static string ExtractText(List<AcpContentBlock> blocks)
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

    private static string Truncate(string s, int maxLen)
    {
        string oneLine = s.ReplaceLineEndings(" ");
        return oneLine.Length <= maxLen ? oneLine : string.Concat(oneLine.AsSpan(0, maxLen), "...");
    }

    private class SessionState
    {
        public required string Cwd { get; init; }
        public required string CurrentModeId { get; set; }
        public required string CurrentModelId { get; set; }
        public required Conversation Conversation { get; set; }
        public List<Tool>? LocalTools { get; set; }
    }

    private record ModelOption(string Id, string Name, string Description);
}
