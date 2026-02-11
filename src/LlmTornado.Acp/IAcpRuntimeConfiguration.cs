namespace LlmTornado.Acp;

/// <summary>
/// Interface for ACP runtime configuration that bridges the ACP protocol
/// with the LlmTornado Agent Runtime.
/// </summary>
public interface IAcpRuntimeConfiguration
{
    /// <summary>
    /// Handles an ACP initialize request and returns agent capabilities.
    /// </summary>
    Task<AcpInitializeResponse> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new session and returns its ID.
    /// </summary>
    Task<AcpNewSessionResponse> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a user prompt within a session and returns the response.
    /// </summary>
    Task<AcpPromptResponse> PromptAsync(AcpPromptRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels ongoing operations for a session.
    /// </summary>
    Task CancelAsync(AcpCancelNotification notification, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the active mode for a session.
    /// </summary>
    Task<AcpSetSessionModeResponse> SetModeAsync(AcpSetSessionModeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes a configuration option for a session.
    /// </summary>
    Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Event handler for streaming session updates to the client.
    /// </summary>
    event Func<AcpSessionNotification, Task>? OnSessionUpdate;
}
