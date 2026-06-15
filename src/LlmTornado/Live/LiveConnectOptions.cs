using System;
using System.Collections.Generic;
using System.Threading;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;

namespace LlmTornado.Live;

/// <summary>
/// Options for opening a Gemini Live API WebSocket session.
/// </summary>
public class LiveConnectOptions
{
    /// <summary>
    /// Session configuration sent in the initial <c>setup</c> message.
    /// </summary>
    public LiveSessionConfig Config { get; set; } = new LiveSessionConfig();

    /// <summary>
    /// API version for the WebSocket endpoint. Use <c>v1alpha</c> only when required by a specific preview feature.
    /// </summary>
    public string ApiVersion { get; set; } = "v1beta";

    /// <summary>
    /// Optional ephemeral access token for client-to-server connections.
    /// When set, the token is sent as <c>access_token</c> instead of the API key.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Invoked for each server message after JSON parsing.
    /// </summary>
    public Action<LiveServerMessage>? OnMessage { get; set; }

    /// <summary>
    /// Invoked when the WebSocket connection opens.
    /// </summary>
    public Action? OnOpen { get; set; }

    /// <summary>
    /// Invoked on WebSocket errors.
    /// </summary>
    public Action<Exception>? OnError { get; set; }

    /// <summary>
    /// Invoked when the WebSocket closes.
    /// </summary>
    public Action<string?>? OnClose { get; set; }

    /// <summary>
    /// Cancellation token for the connection and receive loop.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}

/// <summary>
/// Realtime multimodal input for <see cref="LiveSession.SendRealtimeInputAsync"/>.
/// </summary>
public class LiveRealtimeInput
{
    /// <summary>
    /// Raw PCM audio bytes (16-bit, little-endian). Include sample rate in <see cref="AudioMimeType"/>.
    /// </summary>
    public byte[]? Audio { get; set; }

    /// <summary>
    /// MIME type for audio, e.g. <c>audio/pcm;rate=16000</c>.
    /// </summary>
    public string AudioMimeType { get; set; } = "audio/pcm;rate=16000";

    /// <summary>
    /// JPEG/PNG video frame bytes (max ~1 FPS).
    /// </summary>
    public byte[]? Video { get; set; }

    /// <summary>
    /// MIME type for video frame, e.g. <c>image/jpeg</c>.
    /// </summary>
    public string VideoMimeType { get; set; } = "image/jpeg";

    /// <summary>
    /// Text input during an active realtime conversation.
    /// Preferred over client content on Gemini 3.1 after the first model turn.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Marks manual activity start (requires automatic VAD disabled).
    /// </summary>
    public bool ActivityStart { get; set; }

    /// <summary>
    /// Marks manual activity end (requires automatic VAD disabled).
    /// </summary>
    public bool ActivityEnd { get; set; }

    /// <summary>
    /// Signals the input audio stream paused (automatic VAD only).
    /// </summary>
    public bool AudioStreamEnd { get; set; }
}

/// <summary>
/// Incremental conversation update for <see cref="LiveSession.SendClientContentAsync"/>.
/// On Gemini 3.1 Flash Live, use only to seed initial history when
/// <see cref="LiveHistoryConfig.InitialHistoryInClientContent"/> is true.
/// </summary>
public class LiveClientContent
{
    /// <summary>
    /// Conversation turns to append.
    /// </summary>
    public List<ChatMessage>? Turns { get; set; }

    /// <summary>
    /// When true, the server may begin generation from accumulated content.
    /// </summary>
    public bool TurnComplete { get; set; } = true;
}

/// <summary>
/// Function call response for <see cref="LiveSession.SendToolResponseAsync"/>.
/// </summary>
public class LiveToolResponse
{
    /// <summary>
    /// Function responses matched to server tool calls by id.
    /// </summary>
    public List<LiveFunctionResponseItem>? FunctionResponses { get; set; }
}

/// <summary>
/// A single function response in a Live API tool response message.
/// </summary>
public class LiveFunctionResponseItem
{
    /// <summary>
    /// Tool call id from the server <see cref="LiveToolCall"/>.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Function name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// JSON-serializable response payload.
    /// </summary>
    public object? Response { get; set; }
}
