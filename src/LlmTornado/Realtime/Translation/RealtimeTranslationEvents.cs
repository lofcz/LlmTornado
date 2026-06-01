using System.Collections.Generic;

namespace LlmTornado.Realtime.Translation;

/// <summary>
/// Known Realtime translation server event types.
/// </summary>
public enum RealtimeTranslationEventTypes
{
    /// <summary>
    /// Unrecognized event type.
    /// </summary>
    Unknown,

    /// <summary>
    /// <c>session.created</c>
    /// </summary>
    SessionCreated,

    /// <summary>
    /// <c>session.updated</c>
    /// </summary>
    SessionUpdated,

    /// <summary>
    /// <c>session.closed</c>
    /// </summary>
    SessionClosed,

    /// <summary>
    /// <c>session.input_transcript.delta</c>
    /// </summary>
    InputTranscriptDelta,

    /// <summary>
    /// <c>session.output_transcript.delta</c>
    /// </summary>
    OutputTranscriptDelta,

    /// <summary>
    /// <c>session.output_audio.delta</c>
    /// </summary>
    OutputAudioDelta,

    /// <summary>
    /// <c>error</c>
    /// </summary>
    Error
}

/// <summary>
/// Parsed server event from a Realtime translation WebSocket session.
/// </summary>
public class RealtimeTranslationEvent
{
    /// <summary>
    /// Raw event type string from the server.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Mapped event type.
    /// </summary>
    public RealtimeTranslationEventTypes EventType { get; set; }

    /// <summary>
    /// Unique server event ID.
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// Append-only transcript or audio delta payload.
    /// </summary>
    public string? Delta { get; set; }

    /// <summary>
    /// Stream alignment metadata in milliseconds (200 ms increments).
    /// </summary>
    public int? ElapsedMs { get; set; }

    /// <summary>
    /// Audio encoding for output audio deltas.
    /// </summary>
    public string? AudioFormat { get; set; }

    /// <summary>
    /// Sample rate for output audio deltas.
    /// </summary>
    public int? SampleRate { get; set; }

    /// <summary>
    /// Number of audio channels for output audio deltas.
    /// </summary>
    public int? Channels { get; set; }

    /// <summary>
    /// Decoded PCM16 audio bytes when <see cref="EventType"/> is <see cref="RealtimeTranslationEventTypes.OutputAudioDelta"/>.
    /// </summary>
    public byte[]? AudioData { get; set; }

    /// <summary>
    /// Session snapshot for <c>session.created</c> and <c>session.updated</c> events.
    /// </summary>
    public RealtimeTranslationSessionState? Session { get; set; }

    /// <summary>
    /// Error details when <see cref="EventType"/> is <see cref="RealtimeTranslationEventTypes.Error"/>.
    /// </summary>
    public RealtimeTranslationError? Error { get; set; }

    /// <summary>
    /// Original JSON payload.
    /// </summary>
    public string? RawJson { get; set; }
}

/// <summary>
/// Translation session state returned by the Realtime API.
/// </summary>
public class RealtimeTranslationSessionState
{
    /// <summary>
    /// Session identifier (e.g. <c>sess_1234567890abcdef</c>).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Model used for this session.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Session type. Always <c>translation</c> for translation sessions.
    /// </summary>
    public string? SessionType { get; set; }

    /// <summary>
    /// Expiration timestamp (Unix seconds).
    /// </summary>
    public long? ExpiresAt { get; set; }

    /// <summary>
    /// Configured output language, if any.
    /// </summary>
    public string? OutputLanguage { get; set; }

    /// <summary>
    /// Configured input transcription model, if any.
    /// </summary>
    public string? InputTranscriptionModel { get; set; }

    /// <summary>
    /// Configured noise reduction mode, if any.
    /// </summary>
    public string? NoiseReduction { get; set; }
}

/// <summary>
/// Realtime translation API error payload.
/// </summary>
public class RealtimeTranslationError
{
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error type (e.g. <c>invalid_request_error</c>).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Error code, if any.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Parameter related to the error, if any.
    /// </summary>
    public string? Param { get; set; }

    /// <summary>
    /// Client event ID that caused the error, if applicable.
    /// </summary>
    public string? EventId { get; set; }
}
