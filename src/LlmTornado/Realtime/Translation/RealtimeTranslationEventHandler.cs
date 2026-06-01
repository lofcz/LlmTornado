using System;
using System.Threading.Tasks;

namespace LlmTornado.Realtime.Translation;

/// <summary>
/// Handlers for Realtime translation WebSocket events.
/// </summary>
public class RealtimeTranslationEventHandler
{
    /// <summary>
    /// Invoked for every parsed server event.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? EventHandler { get; set; }

    /// <summary>
    /// Invoked when translated output audio is received.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? OutputAudioHandler { get; set; }

    /// <summary>
    /// Invoked when translated transcript text is received.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? OutputTranscriptHandler { get; set; }

    /// <summary>
    /// Invoked when source-language transcript text is received.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? InputTranscriptHandler { get; set; }

    /// <summary>
    /// Invoked when the session is created or updated.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? SessionHandler { get; set; }

    /// <summary>
    /// Invoked when the session is closed.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? SessionClosedHandler { get; set; }

    /// <summary>
    /// Invoked when the server returns an error event.
    /// </summary>
    public Func<RealtimeTranslationEvent, ValueTask>? ErrorHandler { get; set; }
}
