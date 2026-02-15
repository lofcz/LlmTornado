using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;

namespace LlmTornado.Acp;

/// <summary>
/// Per-session context holding an isolated ChatRuntime, its configuration, and session metadata.
/// </summary>
public class AcpSessionContext : IDisposable
{
    /// <summary>
    /// The ChatRuntime instance for this session, wrapping the session-scoped IRuntimeConfiguration.
    /// </summary>
    public ChatRuntime Agent { get; set; }

    /// <summary>
    /// The runtime configuration powering this session's ChatRuntime.
    /// </summary>
    public IRuntimeConfiguration RuntimeConfig { get; set; }

    /// <summary>
    /// Cancellation token source scoped to this session. Cancel to abort in-flight prompts.
    /// </summary>
    public CancellationTokenSource Cts { get; set; } = new();

    /// <summary>
    /// The working directory the IDE reported when creating this session.
    /// </summary>
    public string Cwd { get; set; } = string.Empty;

    /// <summary>
    /// The currently active mode for this session (e.g. "agent", "chat", "plan", "refactor").
    /// </summary>
    public string CurrentModeId { get; set; } = "agent";

    /// <summary>
    /// The currently selected model identifier for this session.
    /// </summary>
    public string CurrentModelId { get; set; } = string.Empty;

    /// <summary>
    /// Arbitrary session-scoped metadata that subclasses can use.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    public AcpSessionContext(IRuntimeConfiguration runtimeConfig, string cwd)
    {
        RuntimeConfig = runtimeConfig ?? throw new ArgumentNullException(nameof(runtimeConfig));
        Cwd = cwd;
        Agent = new ChatRuntime(RuntimeConfig);
    }

    public void Dispose()
    {
        Cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Base implementation of ACP runtime configuration that integrates with the
/// LlmTornado ChatRuntime agent system. Each session gets its own isolated ChatRuntime
/// created via the abstract <see cref="CreateRuntimeConfiguration"/> factory method.
/// Subclass this to create ACP-compatible agents backed by any <see cref="IRuntimeConfiguration"/>.
/// </summary>
public abstract class BaseAcpTornadoRuntimeConfiguration : IAcpRuntimeConfiguration, IDisposable
{
    private readonly Dictionary<string, AcpSessionContext> _sessions = new();
    private readonly object _sessionsLock = new();

    protected string AgentName { get; set; }
    protected string AgentVersion { get; set; }

    /// <inheritdoc />
    public event Func<AcpSessionNotification, Task>? OnSessionUpdate;

    /// <summary>
    /// Initializes a new instance of the BaseAcpTornadoRuntimeConfiguration.
    /// </summary>
    protected BaseAcpTornadoRuntimeConfiguration(string name, string version = "1.0.0")
    {
        AgentName = name;
        AgentVersion = version;
    }

    /// <summary>
    /// Factory method: create an <see cref="IRuntimeConfiguration"/> for a new session.
    /// Override this to return any configuration — SingletonRuntimeConfiguration, OrchestrationRuntimeConfiguration, etc.
    /// </summary>
    /// <param name="request">The new-session request containing cwd and other metadata.</param>
    /// <param name="modeId">The initial mode for the session.</param>
    /// <param name="modelId">The initial model for the session.</param>
    /// <returns>An IRuntimeConfiguration that will be wrapped in a per-session ChatRuntime.</returns>
    protected abstract IRuntimeConfiguration CreateRuntimeConfiguration(AcpNewSessionRequest request, string modeId, string modelId);

    /// <summary>
    /// Called when the runtime configuration for a session needs to be recreated
    /// (e.g. mode or model change). Override to customise rebuild behaviour.
    /// The default implementation calls <see cref="CreateRuntimeConfiguration"/>.
    /// </summary>
    protected virtual IRuntimeConfiguration RecreateRuntimeConfiguration(AcpSessionContext session, string modeId, string modelId)
    {
        AcpNewSessionRequest syntheticRequest = new() { Cwd = session.Cwd };
        return CreateRuntimeConfiguration(syntheticRequest, modeId, modelId);
    }

    /// <summary>
    /// Retrieves the session context for the given session ID, or null if not found.
    /// </summary>
    protected AcpSessionContext? GetSessionContext(string sessionId)
    {
        lock (_sessionsLock)
        {
            _sessions.TryGetValue(sessionId, out AcpSessionContext? ctx);
            return ctx;
        }
    }

    /// <inheritdoc />
    public virtual Task<AcpInitializeResponse> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken)
    {
        AcpInitializeResponse response = new()
        {
            ProtocolVersion = request.ProtocolVersion,
            AgentInfo = new AcpImplementation
            {
                Name = AgentName,
                Version = AgentVersion
            },
            AgentCapabilities = DescribeCapabilities()
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public virtual Task<AcpNewSessionResponse> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        string initialMode = GetInitialMode(request);
        string initialModel = GetInitialModel(request);

        IRuntimeConfiguration config = CreateRuntimeConfiguration(request, initialMode, initialModel);
        AcpSessionContext ctx = new(config, request.Cwd)
        {
            CurrentModeId = initialMode,
            CurrentModelId = initialModel
        };

        // Wire runtime events with session ID captured in closure — no race condition
        string capturedSessionId = sessionId;
        ctx.Agent.RuntimeConfiguration.OnRuntimeEvent += async (evt) =>
        {
            await HandleRuntimeEvent(capturedSessionId, evt);
        };

        lock (_sessionsLock)
        {
            _sessions[sessionId] = ctx;
        }

        AcpNewSessionResponse response = new()
        {
            SessionId = sessionId
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public virtual async Task<AcpPromptResponse> PromptAsync(AcpPromptRequest request, CancellationToken cancellationToken)
    {
        AcpSessionContext? ctx = GetSessionContext(request.SessionId);

        if (ctx is null)
        {
            throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        }

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctx.Cts.Token);

        ChatMessage userMessage = request.Prompt.ToTornadoMessage();

        string stopReason = AcpStopReasons.EndTurn;

        try
        {
            ChatMessage response = await ctx.Agent.RuntimeConfiguration.AddToChatAsync(userMessage, linkedCts.Token);

            // If the runtime did not stream, send the complete response as a final chunk
            if (OnSessionUpdate is not null && !string.IsNullOrEmpty(response.Content))
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
                            Text = response.Content
                        }
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            stopReason = AcpStopReasons.Cancelled;
        }
        catch (Exception ex)
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

    /// <inheritdoc />
    public virtual Task CancelAsync(AcpCancelNotification notification, CancellationToken cancellationToken)
    {
        AcpSessionContext? ctx = GetSessionContext(notification.SessionId);

        if (ctx is not null)
        {
            ctx.Cts.Cancel();
            ctx.Agent.CancelExecution();
            // Replace the CTS so the session can accept new prompts
            ctx.Cts = new CancellationTokenSource();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task<AcpSetSessionModeResponse> SetModeAsync(AcpSetSessionModeRequest request, CancellationToken cancellationToken)
    {
        AcpSessionContext? ctx = GetSessionContext(request.SessionId);

        if (ctx is not null)
        {
            RebuildSessionRuntime(ctx, request.ModeId, ctx.CurrentModelId);
        }

        return Task.FromResult(new AcpSetSessionModeResponse());
    }

    /// <inheritdoc />
    public virtual Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AcpSetSessionConfigOptionResponse());
    }

    /// <summary>
    /// Override to describe the agent's capabilities.
    /// </summary>
    protected virtual AcpAgentCapabilities DescribeCapabilities()
    {
        return new AcpAgentCapabilities
        {
            PromptCapabilities = new AcpPromptCapabilities
            {
                Image = false,
                Audio = false,
                EmbeddedContext = false
            }
        };
    }

    /// <summary>
    /// Override to control the initial mode for a new session.
    /// </summary>
    protected virtual string GetInitialMode(AcpNewSessionRequest request) => "agent";

    /// <summary>
    /// Override to control the initial model for a new session.
    /// </summary>
    protected virtual string GetInitialModel(AcpNewSessionRequest request) => "gpt-4.1-nano";

    /// <summary>
    /// Rebuilds the session's ChatRuntime while preserving conversation history.
    /// </summary>
    protected virtual void RebuildSessionRuntime(AcpSessionContext ctx, string newModeId, string newModelId)
    {
        // Snapshot existing messages
        List<ChatMessage> existingMessages = ctx.RuntimeConfig.GetMessages();

        ctx.CurrentModeId = newModeId;
        ctx.CurrentModelId = newModelId;

        // Create fresh runtime config
        IRuntimeConfiguration newConfig = RecreateRuntimeConfiguration(ctx, newModeId, newModelId);
        ctx.RuntimeConfig = newConfig;
        ctx.Agent = new ChatRuntime(newConfig);

        // Re-wire events — session ID is already captured via the dictionary key
        // We need to find the session ID for this context
        string? sessionId = null;
        lock (_sessionsLock)
        {
            foreach (KeyValuePair<string, AcpSessionContext> kvp in _sessions)
            {
                if (ReferenceEquals(kvp.Value, ctx))
                {
                    sessionId = kvp.Key;
                    break;
                }
            }
        }

        if (sessionId is not null)
        {
            string capturedSessionId = sessionId;
            ctx.Agent.RuntimeConfiguration.OnRuntimeEvent += async (evt) =>
            {
                await HandleRuntimeEvent(capturedSessionId, evt);
            };
        }

        // Replay conversation history (skip system messages — they're set by the new config)
        foreach (ChatMessage msg in existingMessages)
        {
            if (msg.Role is not Code.ChatMessageRoles.System)
            {
                ctx.RuntimeConfig.GetMessages().Add(msg);
            }
        }
    }

    /// <summary>
    /// Handles runtime events and forwards them as ACP session updates, scoped to the correct session.
    /// </summary>
    private async ValueTask HandleRuntimeEvent(string sessionId, ChatRuntimeEvents evt)
    {
        if (OnSessionUpdate is null)
        {
            return;
        }

        List<AcpSessionUpdate>? updates = evt.ToAcpSessionUpdates();

        if (updates is null || updates.Count == 0)
        {
            return;
        }

        foreach (AcpSessionUpdate update in updates)
        {
            await OnSessionUpdate.Invoke(new AcpSessionNotification
            {
                SessionId = sessionId,
                Update = update
            });
        }
    }

    /// <summary>
    /// Releases resources used by this configuration.
    /// </summary>
    public void Dispose()
    {
        lock (_sessionsLock)
        {
            foreach (AcpSessionContext ctx in _sessions.Values)
            {
                ctx.Dispose();
            }

            _sessions.Clear();
        }

        GC.SuppressFinalize(this);
    }
}
