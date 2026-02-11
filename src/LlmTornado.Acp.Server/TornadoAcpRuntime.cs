using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

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
        new() { Id = "code", Name = "Code", Description = "Coding assistant — writes and explains code" },
        new() { Id = "chat", Name = "Chat", Description = "General-purpose conversational assistant" },
        new() { Id = "architect", Name = "Architect", Description = "High-level design and architecture guidance" }
    ];

    private static readonly Dictionary<string, string> ModeSystemPrompts = new()
    {
        ["code"] = """
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
        ["architect"] = """
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

    public Task<AcpNewSessionResponse> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        string initialModel = _defaultModel;

        // Ensure the default model is in our list, otherwise pick the first
        if (!AvailableModels.Exists(m => m.Id == initialModel))
        {
            initialModel = AvailableModels[0].Id;
        }

        SessionState state = new()
        {
            Cwd = request.Cwd,
            CurrentModeId = "code",
            CurrentModelId = initialModel,
            Conversation = CreateConversation(initialModel, "code", request.Cwd)
        };

        lock (_sessionsLock)
        {
            _sessions[sessionId] = state;
        }

        Console.Error.WriteLine($"[ACP] New session: {sessionId} (cwd: {request.Cwd}, model: {initialModel})");

        return Task.FromResult(new AcpNewSessionResponse
        {
            SessionId = sessionId,
            Modes = new AcpSessionModeState
            {
                CurrentModeId = state.CurrentModeId,
                AvailableModes = AvailableModes
            }
        });
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
            session.Conversation = CreateConversation(session.CurrentModelId, session.CurrentModeId, session.Cwd);
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
            session.Conversation = CreateConversation(session.CurrentModelId, session.CurrentModeId, session.Cwd);
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

    private Conversation CreateConversation(string modelId, string modeId, string cwd)
    {
        Conversation conversation = _api.Chat.CreateConversation(new ChatRequest
        {
            Model = modelId
        });

        string systemPrompt = ModeSystemPrompts.GetValueOrDefault(modeId, ModeSystemPrompts["code"]);
        conversation.AppendSystemMessage($"{systemPrompt}\n\nThe user's current working directory is: {cwd}");

        return conversation;
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
                Category = "Model",
                CurrentValue = session.CurrentModelId,
                Options = AvailableModels.ConvertAll(m => new AcpSessionConfigSelectOption
                {
                    Value = m.Id,
                    Name = m.Name,
                    Description = m.Description
                })
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
    }

    private record ModelOption(string Id, string Name, string Description);
}
