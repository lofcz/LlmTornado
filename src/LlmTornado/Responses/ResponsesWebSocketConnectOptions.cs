using System;
using System.Threading;

namespace LlmTornado.Responses;

/// <summary>
/// Options for opening a Responses API WebSocket connection.
/// </summary>
public class ResponsesWebSocketConnectOptions
{
    /// <summary>
    /// Optional override for the WebSocket URL. When null, derived from the provider's Responses HTTP URL.
    /// </summary>
    public string? UrlOverride { get; set; }

    /// <summary>
    /// Invoked when the WebSocket connection opens.
    /// </summary>
    public Action? OnOpen { get; set; }

    /// <summary>
    /// Invoked when the WebSocket connection closes.
    /// </summary>
    public Action? OnClose { get; set; }

    /// <summary>
    /// Invoked on WebSocket errors.
    /// </summary>
    public Action<Exception>? OnError { get; set; }

    /// <summary>
    /// Cancellation token used while establishing the connection.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
