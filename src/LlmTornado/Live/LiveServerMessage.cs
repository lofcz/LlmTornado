using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;

namespace LlmTornado.Live;

/// <summary>
/// Parsed server message from a Gemini Live API WebSocket session.
/// Corresponds to <c>BidiGenerateContentServerMessage</c>.
/// </summary>
public class LiveServerMessage
{
    /// <summary>
    /// Raw JSON payload.
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Token usage metadata when present.
    /// </summary>
    public LiveUsageMetadata? UsageMetadata { get; set; }

    /// <summary>
    /// Present once session setup completes.
    /// </summary>
    public bool SetupComplete { get; set; }

    /// <summary>
    /// Incremental model output.
    /// </summary>
    public LiveServerContent? ServerContent { get; set; }

    /// <summary>
    /// Tool call request from the model.
    /// </summary>
    public LiveToolCall? ToolCall { get; set; }

    /// <summary>
    /// Cancelled tool call ids after interruption.
    /// </summary>
    public LiveToolCallCancellation? ToolCallCancellation { get; set; }

    /// <summary>
    /// Server will disconnect soon.
    /// </summary>
    public LiveGoAway? GoAway { get; set; }

    /// <summary>
    /// Session resumption state update.
    /// </summary>
    public LiveSessionResumptionUpdate? SessionResumptionUpdate { get; set; }
}

/// <summary>
/// Incremental server content from the model.
/// </summary>
public class LiveServerContent
{
    /// <summary>
    /// Model generation finished but turn may not be complete yet.
    /// </summary>
    public bool GenerationComplete { get; set; }

    /// <summary>
    /// Model finished its turn.
    /// </summary>
    public bool TurnComplete { get; set; }

    /// <summary>
    /// User activity interrupted model generation.
    /// </summary>
    public bool Interrupted { get; set; }

    /// <summary>
    /// Model output turn. May contain multiple parts in a single event on Gemini 3.1.
    /// </summary>
    public ChatMessage? ModelTurn { get; set; }

    /// <summary>
    /// Transcription of user audio input.
    /// </summary>
    public LiveTranscription? InputTranscription { get; set; }

    /// <summary>
    /// Transcription of model audio output.
    /// </summary>
    public LiveTranscription? OutputTranscription { get; set; }
}

/// <summary>
/// Audio or output transcription fragment.
/// </summary>
public class LiveTranscription
{
    /// <summary>
    /// Transcription text.
    /// </summary>
    public string? Text { get; set; }
}

/// <summary>
/// Tool call emitted by the model.
/// </summary>
public class LiveToolCall
{
    /// <summary>
    /// Function calls to execute.
    /// </summary>
    public List<FunctionCall>? FunctionCalls { get; set; }
}

/// <summary>
/// Cancelled tool calls after barge-in.
/// </summary>
public class LiveToolCallCancellation
{
    /// <summary>
    /// Ids of cancelled tool calls.
    /// </summary>
    public List<string>? Ids { get; set; }
}

/// <summary>
/// Impending server disconnect notice.
/// </summary>
public class LiveGoAway
{
    /// <summary>
    /// Remaining connection time (protobuf duration string when provided).
    /// </summary>
    public string? TimeLeft { get; set; }
}

/// <summary>
/// Session resumption handle update.
/// </summary>
public class LiveSessionResumptionUpdate
{
    /// <summary>
    /// New resumption handle when resumable.
    /// </summary>
    public string? NewHandle { get; set; }

    /// <summary>
    /// Whether the session can be resumed at this point.
    /// </summary>
    public bool Resumable { get; set; }
}

/// <summary>
/// Token usage for a Live API server message.
/// </summary>
public class LiveUsageMetadata
{
    /// <summary>
    /// Prompt token count.
    /// </summary>
    public int? PromptTokenCount { get; set; }

    /// <summary>
    /// Response token count.
    /// </summary>
    public int? ResponseTokenCount { get; set; }

    /// <summary>
    /// Thought token count for thinking models.
    /// </summary>
    public int? ThoughtsTokenCount { get; set; }

    /// <summary>
    /// Total token count.
    /// </summary>
    public int? TotalTokenCount { get; set; }
}
