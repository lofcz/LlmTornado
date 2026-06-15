using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LlmTornado.Live.Vendors.Google;

internal static class VendorGoogleLiveJson
{
    internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
}

internal class VendorGoogleLiveClientEnvelope
{
    [JsonProperty("setup", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveSetup? Setup { get; set; }

    [JsonProperty("clientContent", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveClientContent? ClientContent { get; set; }

    [JsonProperty("realtimeInput", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveRealtimeInput? RealtimeInput { get; set; }

    [JsonProperty("toolResponse", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveToolResponse? ToolResponse { get; set; }
}

internal class VendorGoogleLiveSetup
{
    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("generationConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveGenerationConfig? GenerationConfig { get; set; }

    [JsonProperty("systemInstruction", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveContent? SystemInstruction { get; set; }

    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? Tools { get; set; }

    [JsonProperty("realtimeInputConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveRealtimeInputConfig? RealtimeInputConfig { get; set; }

    [JsonProperty("sessionResumption", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveSessionResumptionConfig? SessionResumption { get; set; }

    [JsonProperty("contextWindowCompression", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveContextWindowCompressionConfig? ContextWindowCompression { get; set; }

    [JsonProperty("inputAudioTranscription", NullValueHandling = NullValueHandling.Ignore)]
    public object? InputAudioTranscription { get; set; }

    [JsonProperty("outputAudioTranscription", NullValueHandling = NullValueHandling.Ignore)]
    public object? OutputAudioTranscription { get; set; }

    [JsonProperty("historyConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveHistoryConfig? HistoryConfig { get; set; }
}

internal class VendorGoogleLiveGenerationConfig
{
    [JsonProperty("responseModalities", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ResponseModalities { get; set; }

    [JsonProperty("maxOutputTokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxOutputTokens { get; set; }

    [JsonProperty("speechConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveSpeechConfig? SpeechConfig { get; set; }

    [JsonProperty("mediaResolution", NullValueHandling = NullValueHandling.Ignore)]
    public string? MediaResolution { get; set; }

    [JsonProperty("thinkingConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveThinkingConfig? ThinkingConfig { get; set; }
}

internal class VendorGoogleLiveThinkingConfig
{
    [JsonProperty("thinkingLevel", NullValueHandling = NullValueHandling.Ignore)]
    public string? ThinkingLevel { get; set; }

    [JsonProperty("thinkingBudget", NullValueHandling = NullValueHandling.Ignore)]
    public int? ThinkingBudget { get; set; }

    [JsonProperty("includeThoughts", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IncludeThoughts { get; set; }
}

internal class VendorGoogleLiveSpeechConfig
{
    [JsonProperty("voiceConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveVoiceConfig? VoiceConfig { get; set; }
}

internal class VendorGoogleLiveVoiceConfig
{
    [JsonProperty("prebuiltVoiceConfig", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLivePrebuiltVoiceConfig? PrebuiltVoiceConfig { get; set; }
}

internal class VendorGoogleLivePrebuiltVoiceConfig
{
    [JsonProperty("voiceName")]
    public string? VoiceName { get; set; }
}

internal class VendorGoogleLiveRealtimeInputConfig
{
    [JsonProperty("automaticActivityDetection", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveAutomaticActivityDetection? AutomaticActivityDetection { get; set; }

    [JsonProperty("activityHandling", NullValueHandling = NullValueHandling.Ignore)]
    public string? ActivityHandling { get; set; }

    [JsonProperty("turnCoverage", NullValueHandling = NullValueHandling.Ignore)]
    public string? TurnCoverage { get; set; }
}

internal class VendorGoogleLiveAutomaticActivityDetection
{
    [JsonProperty("disabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Disabled { get; set; }

    [JsonProperty("startOfSpeechSensitivity", NullValueHandling = NullValueHandling.Ignore)]
    public string? StartOfSpeechSensitivity { get; set; }

    [JsonProperty("endOfSpeechSensitivity", NullValueHandling = NullValueHandling.Ignore)]
    public string? EndOfSpeechSensitivity { get; set; }

    [JsonProperty("prefixPaddingMs", NullValueHandling = NullValueHandling.Ignore)]
    public int? PrefixPaddingMs { get; set; }

    [JsonProperty("silenceDurationMs", NullValueHandling = NullValueHandling.Ignore)]
    public int? SilenceDurationMs { get; set; }
}

internal class VendorGoogleLiveHistoryConfig
{
    [JsonProperty("initialHistoryInClientContent", NullValueHandling = NullValueHandling.Ignore)]
    public bool? InitialHistoryInClientContent { get; set; }
}

internal class VendorGoogleLiveSessionResumptionConfig
{
    [JsonProperty("handle", NullValueHandling = NullValueHandling.Ignore)]
    public string? Handle { get; set; }
}

internal class VendorGoogleLiveContextWindowCompressionConfig
{
    [JsonProperty("triggerTokens", NullValueHandling = NullValueHandling.Ignore)]
    public long? TriggerTokens { get; set; }

    [JsonProperty("slidingWindow", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveSlidingWindow? SlidingWindow { get; set; }
}

internal class VendorGoogleLiveSlidingWindow
{
    [JsonProperty("targetTokens", NullValueHandling = NullValueHandling.Ignore)]
    public long? TargetTokens { get; set; }
}

internal class VendorGoogleLiveContent
{
    [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
    public string? Role { get; set; }

    [JsonProperty("parts", NullValueHandling = NullValueHandling.Ignore)]
    public List<VendorGoogleLivePart>? Parts { get; set; }
}

internal class VendorGoogleLivePart
{
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }

    [JsonProperty("inlineData", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveBlob? InlineData { get; set; }

    [JsonProperty("functionCall", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveFunctionCall? FunctionCall { get; set; }

    [JsonProperty("functionResponse", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveFunctionResponse? FunctionResponse { get; set; }
}

internal class VendorGoogleLiveBlob
{
    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }

    [JsonProperty("data")]
    public string? Data { get; set; }
}

internal class VendorGoogleLiveClientContent
{
    [JsonProperty("turns", NullValueHandling = NullValueHandling.Ignore)]
    public List<VendorGoogleLiveContent>? Turns { get; set; }

    [JsonProperty("turnComplete", NullValueHandling = NullValueHandling.Ignore)]
    public bool? TurnComplete { get; set; }
}

internal class VendorGoogleLiveRealtimeInput
{
    [JsonProperty("audio", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveBlob? Audio { get; set; }

    [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveBlob? Video { get; set; }

    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }

    [JsonProperty("activityStart", NullValueHandling = NullValueHandling.Ignore)]
    public object? ActivityStart { get; set; }

    [JsonProperty("activityEnd", NullValueHandling = NullValueHandling.Ignore)]
    public object? ActivityEnd { get; set; }

    [JsonProperty("audioStreamEnd", NullValueHandling = NullValueHandling.Ignore)]
    public bool? AudioStreamEnd { get; set; }
}

internal class VendorGoogleLiveToolResponse
{
    [JsonProperty("functionResponses", NullValueHandling = NullValueHandling.Ignore)]
    public List<VendorGoogleLiveFunctionResponse>? FunctionResponses { get; set; }
}

internal class VendorGoogleLiveFunctionCall
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("args", NullValueHandling = NullValueHandling.Ignore)]
    public object? Args { get; set; }
}

internal class VendorGoogleLiveFunctionResponse
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
    public object? Response { get; set; }
}

internal class VendorGoogleLiveServerEnvelope
{
    [JsonProperty("setupComplete", NullValueHandling = NullValueHandling.Ignore)]
    public object? SetupComplete { get; set; }

    [JsonProperty("serverContent", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveServerContent? ServerContent { get; set; }

    [JsonProperty("toolCall", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveToolCall? ToolCall { get; set; }

    [JsonProperty("toolCallCancellation", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveToolCallCancellation? ToolCallCancellation { get; set; }

    [JsonProperty("goAway", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveGoAway? GoAway { get; set; }

    [JsonProperty("sessionResumptionUpdate", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveSessionResumptionUpdate? SessionResumptionUpdate { get; set; }

    [JsonProperty("usageMetadata", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveUsageMetadata? UsageMetadata { get; set; }
}

internal class VendorGoogleLiveServerContent
{
    [JsonProperty("generationComplete", NullValueHandling = NullValueHandling.Ignore)]
    public bool? GenerationComplete { get; set; }

    [JsonProperty("turnComplete", NullValueHandling = NullValueHandling.Ignore)]
    public bool? TurnComplete { get; set; }

    [JsonProperty("interrupted", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Interrupted { get; set; }

    [JsonProperty("modelTurn", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveContent? ModelTurn { get; set; }

    [JsonProperty("inputTranscription", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveTranscription? InputTranscription { get; set; }

    [JsonProperty("outputTranscription", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleLiveTranscription? OutputTranscription { get; set; }
}

internal class VendorGoogleLiveTranscription
{
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }
}

internal class VendorGoogleLiveToolCall
{
    [JsonProperty("functionCalls", NullValueHandling = NullValueHandling.Ignore)]
    public List<VendorGoogleLiveFunctionCall>? FunctionCalls { get; set; }
}

internal class VendorGoogleLiveToolCallCancellation
{
    [JsonProperty("ids", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Ids { get; set; }
}

internal class VendorGoogleLiveGoAway
{
    [JsonProperty("timeLeft", NullValueHandling = NullValueHandling.Ignore)]
    public string? TimeLeft { get; set; }
}

internal class VendorGoogleLiveSessionResumptionUpdate
{
    [JsonProperty("newHandle", NullValueHandling = NullValueHandling.Ignore)]
    public string? NewHandle { get; set; }

    [JsonProperty("resumable", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Resumable { get; set; }
}

internal class VendorGoogleLiveUsageMetadata
{
    [JsonProperty("promptTokenCount", NullValueHandling = NullValueHandling.Ignore)]
    public int? PromptTokenCount { get; set; }

    [JsonProperty("responseTokenCount", NullValueHandling = NullValueHandling.Ignore)]
    public int? ResponseTokenCount { get; set; }

    [JsonProperty("thoughtsTokenCount", NullValueHandling = NullValueHandling.Ignore)]
    public int? ThoughtsTokenCount { get; set; }

    [JsonProperty("totalTokenCount", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalTokenCount { get; set; }
}
