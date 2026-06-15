using System;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Realtime.Vendors.OpenAi;

namespace LlmTornado.Realtime.Translation;

/// <summary>
/// Options for opening an OpenAI Realtime translation WebSocket session.
/// </summary>
public class RealtimeTranslationConnectOptions
{
    /// <summary>
    /// Translation model. Defaults to <see cref="ChatModelOpenAiRealtime.ModelRealtimeTranslate"/>.
    /// </summary>
    public ChatModel Model { get; set; } = ChatModelOpenAiRealtime.ModelRealtimeTranslate;

    /// <summary>
    /// Session configuration applied via <c>session.update</c> after the socket opens.
    /// </summary>
    public RealtimeTranslationSessionConfig Config { get; set; } = new RealtimeTranslationSessionConfig();

    /// <summary>
    /// API key or ephemeral client secret. When null, uses authenticated OpenAI provider from <see cref="TornadoApi"/>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional safety identifier (hashed user ID) sent as <c>OpenAI-Safety-Identifier</c>.
    /// </summary>
    public string? SafetyIdentifier { get; set; }

    /// <summary>
    /// Optional event handlers invoked while the receive loop runs.
    /// </summary>
    public RealtimeTranslationEventHandler? EventHandler { get; set; }

    /// <summary>
    /// Cancellation token for the connection and receive loop.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;
}
