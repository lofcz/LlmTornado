using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Live;

/// <summary>
/// Session configuration for a Gemini Live API WebSocket connection.
/// Maps to <c>BidiGenerateContentSetup</c>.
/// </summary>
public class LiveSessionConfig
{
    /// <summary>
    /// Model to use. Defaults to <see cref="ChatModelGoogleGeminiPreview.ModelGemini31FlashLivePreview"/>.
    /// </summary>
    public ChatModel? Model { get; set; }

    /// <summary>
    /// Output modalities. Native audio Live models require <see cref="LiveResponseModality.Audio"/>.
    /// </summary>
    public List<LiveResponseModality> ResponseModalities { get; set; } = [LiveResponseModality.Audio];

    /// <summary>
    /// Optional system instruction text.
    /// </summary>
    public string? SystemInstruction { get; set; }

    /// <summary>
    /// Tools available to the model during the session.
    /// Async (non-blocking) function calling is not supported on Gemini 3.1 Flash Live.
    /// </summary>
    public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Thinking depth for Gemini 3.x Live models. Default is <see cref="LiveThinkingLevel.Minimal"/>.
    /// Do not set <see cref="ThinkingBudget"/> on Gemini 3.1 models.
    /// </summary>
    public LiveThinkingLevel? ThinkingLevel { get; set; }

    /// <summary>
    /// Legacy thinking token budget for Gemini 2.5 Live models only.
    /// </summary>
    public int? ThinkingBudget { get; set; }

    /// <summary>
    /// When true, thought summaries may be included in responses.
    /// </summary>
    public bool? IncludeThoughts { get; set; }

    /// <summary>
    /// Prebuilt TTS voice name (e.g. Kore, Puck).
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// Input media resolution for video/image frames.
    /// </summary>
    public LiveMediaResolution? MediaResolution { get; set; }

    /// <summary>
    /// Enables transcription of user audio input.
    /// </summary>
    public bool InputAudioTranscription { get; set; }

    /// <summary>
    /// Enables transcription of model audio output.
    /// </summary>
    public bool OutputAudioTranscription { get; set; }

    /// <summary>
    /// Realtime input / VAD configuration.
    /// </summary>
    public LiveRealtimeInputConfig? RealtimeInput { get; set; }

    /// <summary>
    /// History exchange configuration. Required when seeding initial context via client content on Gemini 3.1.
    /// </summary>
    public LiveHistoryConfig? History { get; set; }

    /// <summary>
    /// Session resumption handle from a previous connection.
    /// </summary>
    public LiveSessionResumptionConfig? SessionResumption { get; set; }

    /// <summary>
    /// Context window compression when token count exceeds configured thresholds.
    /// </summary>
    public LiveContextWindowCompressionConfig? ContextWindowCompression { get; set; }

    /// <summary>
    /// Maximum output tokens per model turn.
    /// </summary>
    public int? MaxOutputTokens { get; set; }
}

/// <summary>
/// Realtime input behavior for Live API sessions.
/// </summary>
public class LiveRealtimeInputConfig
{
    /// <summary>
    /// Automatic voice activity detection. Enabled by default.
    /// </summary>
    public LiveAutomaticActivityDetection? AutomaticActivityDetection { get; set; }

    /// <summary>
    /// Whether user activity interrupts the model (barge-in). Default is interrupt.
    /// </summary>
    public LiveActivityHandling? ActivityHandling { get; set; }

    /// <summary>
    /// Which input is included in each user turn.
    /// </summary>
    public LiveTurnCoverage TurnCoverage { get; set; } = LiveTurnCoverage.Unspecified;
}

/// <summary>
/// Automatic VAD tuning parameters.
/// </summary>
public class LiveAutomaticActivityDetection
{
    /// <summary>
    /// When true, the client must send explicit activity start/end signals.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Start-of-speech sensitivity.
    /// </summary>
    public LiveStartSensitivity? StartOfSpeechSensitivity { get; set; }

    /// <summary>
    /// End-of-speech sensitivity.
    /// </summary>
    public LiveEndSensitivity? EndOfSpeechSensitivity { get; set; }

    /// <summary>
    /// Audio prepended before detected speech (ms). Recommended ≥ 20.
    /// </summary>
    public int? PrefixPaddingMs { get; set; }

    /// <summary>
    /// Silence duration before end-of-speech (ms). Recommended 500–800.
    /// </summary>
    public int? SilenceDurationMs { get; set; }
}

/// <summary>
/// Start-of-speech detection sensitivity.
/// </summary>
public enum LiveStartSensitivity
{
    /// <summary>
    /// Provider default (high).
    /// </summary>
    Unspecified,

    /// <summary>
    /// Detect speech start more often.
    /// </summary>
    High,

    /// <summary>
    /// Detect speech start less often.
    /// </summary>
    Low
}

/// <summary>
/// End-of-speech detection sensitivity.
/// </summary>
public enum LiveEndSensitivity
{
    /// <summary>
    /// Provider default (high).
    /// </summary>
    Unspecified,

    /// <summary>
    /// End speech turns more often.
    /// </summary>
    High,

    /// <summary>
    /// End speech turns less often.
    /// </summary>
    Low
}

/// <summary>
/// How user activity affects an in-progress model response.
/// </summary>
public enum LiveActivityHandling
{
    /// <summary>
    /// Provider default: activity interrupts the model.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Start of activity interrupts the model (barge-in).
    /// </summary>
    StartOfActivityInterrupts,

    /// <summary>
    /// Activity does not interrupt the model.
    /// </summary>
    NoInterruption
}

/// <summary>
/// History configuration for Gemini 3.1 Live sessions.
/// </summary>
public class LiveHistoryConfig
{
    /// <summary>
    /// When true, the first <see cref="LiveSession.SendClientContentAsync"/> messages seed initial history
    /// before realtime input begins. Required for Gemini 3.1 Flash Live when using client content.
    /// </summary>
    public bool InitialHistoryInClientContent { get; set; }
}

/// <summary>
/// Resume a previous Live session using a server-issued handle.
/// </summary>
public class LiveSessionResumptionConfig
{
    /// <summary>
    /// Handle token from a prior <see cref="LiveSessionResumptionUpdate.NewHandle"/>.
    /// </summary>
    public string? Handle { get; set; }
}

/// <summary>
/// Sliding-window context compression configuration.
/// </summary>
public class LiveContextWindowCompressionConfig
{
    /// <summary>
    /// Token count that triggers compression. Defaults to ~80% of model context when unset.
    /// </summary>
    public long? TriggerTokens { get; set; }

    /// <summary>
    /// Target token count after compression.
    /// </summary>
    public long? TargetTokens { get; set; }
}
