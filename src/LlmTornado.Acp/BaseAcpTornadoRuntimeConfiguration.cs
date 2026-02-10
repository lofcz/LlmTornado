using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;

namespace LlmTornado.Acp;

/// <summary>
/// Base implementation of ACP runtime configuration that integrates with the
/// LlmTornado ChatRuntime agent system. Subclass this to create ACP-compatible agents.
/// </summary>
public abstract class BaseAcpTornadoRuntimeConfiguration : IAcpRuntimeConfiguration, IDisposable
{
    protected readonly ChatRuntime Agent;
    protected readonly IRuntimeConfiguration RuntimeConfig;
    private readonly Dictionary<string, CancellationTokenSource> _sessionCancellations = new();
    private readonly Dictionary<string, string> _sessions = new();
    private volatile string? _activeSessionId;

    protected string AgentName { get; set; }
    protected string AgentVersion { get; set; }

    /// <inheritdoc />
    public event Func<AcpSessionNotification, Task>? OnSessionUpdate;

    /// <summary>
    /// Initializes a new instance of the BaseAcpTornadoRuntimeConfiguration.
    /// </summary>
    public BaseAcpTornadoRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version = "1.0.0")
    {
        RuntimeConfig = runtimeConfig ?? throw new ArgumentNullException(nameof(runtimeConfig));
        AgentName = name;
        AgentVersion = version;

        Agent = new ChatRuntime(RuntimeConfig);
        Agent.RuntimeConfiguration.OnRuntimeEvent += RuntimeEventHandler;
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
        string sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = request.Cwd;
        _sessionCancellations[sessionId] = new CancellationTokenSource();
        _activeSessionId = sessionId;

        AcpNewSessionResponse response = new()
        {
            SessionId = sessionId
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public virtual async Task<AcpPromptResponse> PromptAsync(AcpPromptRequest request, CancellationToken cancellationToken)
    {
        if (!_sessions.ContainsKey(request.SessionId))
        {
            throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        }

        CancellationTokenSource sessionCts = _sessionCancellations[request.SessionId];
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sessionCts.Token);

        _activeSessionId = request.SessionId;

        ChatMessage userMessage = request.Prompt.ToTornadoMessage();
        ChatMessage response = await Agent.InvokeAsync(userMessage);

        // Stream the agent's response back as a session update
        if (OnSessionUpdate is not null)
        {
            AcpSessionNotification notification = new()
            {
                SessionId = request.SessionId,
                Update = new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = response.Content ?? string.Empty
                    }
                }
            };

            await OnSessionUpdate.Invoke(notification);
        }

        return new AcpPromptResponse
        {
            StopReason = AcpStopReasons.EndTurn
        };
    }

    /// <inheritdoc />
    public virtual Task CancelAsync(AcpCancelNotification notification, CancellationToken cancellationToken)
    {
        if (_sessionCancellations.TryGetValue(notification.SessionId, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            _sessionCancellations[notification.SessionId] = new CancellationTokenSource();
        }

        return Task.CompletedTask;
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
    /// Handles runtime events and forwards them as ACP session updates.
    /// </summary>
    private async ValueTask RuntimeEventHandler(ChatRuntimeEvents evt)
    {
        if (OnSessionUpdate is null)
        {
            return;
        }

        // Use the explicitly tracked active session
        string? activeSessionId = _activeSessionId;

        if (activeSessionId is null)
        {
            return;
        }

        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        await OnSessionUpdate.Invoke(new AcpSessionNotification
        {
            SessionId = activeSessionId,
            Update = update
        });
    }

    /// <summary>
    /// Releases resources used by this configuration.
    /// </summary>
    public void Dispose()
    {
        foreach (CancellationTokenSource cts in _sessionCancellations.Values)
        {
            cts.Dispose();
        }

        _sessionCancellations.Clear();
        GC.SuppressFinalize(this);
    }
}
