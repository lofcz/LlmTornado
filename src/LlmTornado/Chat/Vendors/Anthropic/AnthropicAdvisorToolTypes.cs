using System.Runtime.Serialization;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// Configuration for the Anthropic advisor server tool (beta).
/// Pair a faster executor model (<see cref="ChatRequest.Model"/>) with a higher-intelligence advisor model.
/// Requires beta header <c>advisor-tool-2026-03-01</c> (added automatically).
/// </summary>
public class AnthropicAdvisorToolRequest
{
    /// <summary>
    /// The advisor model. Must be at least as capable as the executor (<see cref="ChatRequest.Model"/>).
    /// For example <c>claude-opus-4-8</c> with a Sonnet executor.
    /// </summary>
    public ChatModel AdvisorModel { get; set; } = null!;

    /// <summary>
    /// Optional executor model override for validation/documentation. When omitted, <see cref="ChatRequest.Model"/> is used.
    /// </summary>
    public ChatModel? ExecutorModel { get; set; }

    /// <summary>
    /// Maximum advisor calls allowed in a single request. Omit for unlimited (per-request cap only).
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Caps the advisor's total output (thinking plus text) per call. Minimum 1024.
    /// </summary>
    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Enables prompt caching for the advisor's transcript across calls within a conversation.
    /// Shape: <c>{"type": "ephemeral", "ttl": "5m" | "1h"}</c>.
    /// </summary>
    public AnthropicCacheSettings? Caching { get; set; }

    /// <summary>
    /// Builds the built-in tool definition sent to the API.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolAdvisor20260301 ToBuiltInTool()
    {
        return new VendorAnthropicChatRequestBuiltInToolAdvisor20260301
        {
            AdvisorModel = AdvisorModel.Name,
            MaxUses = MaxUses,
            MaxTokens = MaxTokens,
            Caching = Caching
        };
    }
}

/// <summary>
/// Parsed advisor tool use block (<c>server_tool_use</c> with <c>name: advisor</c>).
/// </summary>
public class AnthropicAdvisorToolUseData : IBuiltInToolCallData
{
    /// <summary>
    /// Tool use id (e.g. <c>srvtoolu_...</c>).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Always <c>advisor</c>.
    /// </summary>
    public string Name { get; set; } = "advisor";

    /// <summary>
    /// Empty input object from the executor.
    /// </summary>
    public string? InputJson { get; set; }
}

/// <summary>
/// Content variant inside <c>advisor_tool_result</c>.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicAdvisorToolResultContentTypes
{
    /// <summary>
    /// Human-readable advice text.
    /// </summary>
    [EnumMember(Value = "advisor_result")]
    AdvisorResult,

    /// <summary>
    /// Opaque encrypted advice blob for multi-turn round-trip.
    /// </summary>
    [EnumMember(Value = "advisor_redacted_result")]
    AdvisorRedactedResult,

    /// <summary>
    /// Advisor sub-inference failed; executor continues without advice.
    /// </summary>
    [EnumMember(Value = "advisor_tool_result_error")]
    AdvisorToolResultError
}

/// <summary>
/// Error codes returned in <c>advisor_tool_result_error</c> content.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicAdvisorToolResultErrorCodes
{
    [EnumMember(Value = "max_uses_exceeded")]
    MaxUsesExceeded,

    [EnumMember(Value = "too_many_requests")]
    TooManyRequests,

    [EnumMember(Value = "overloaded")]
    Overloaded,

    [EnumMember(Value = "prompt_too_long")]
    PromptTooLong,

    [EnumMember(Value = "execution_time_exceeded")]
    ExecutionTimeExceeded,

    [EnumMember(Value = "unavailable")]
    Unavailable
}

/// <summary>
/// Parsed <c>advisor_tool_result</c> block content.
/// </summary>
public class AnthropicAdvisorToolResultData
{
    /// <summary>
    /// Links to the preceding <c>server_tool_use</c> block.
    /// </summary>
    public string ToolUseId { get; set; } = string.Empty;

    /// <summary>
    /// Discriminated content type.
    /// </summary>
    public AnthropicAdvisorToolResultContentTypes ContentType { get; set; }

    /// <summary>
    /// Advice text when <see cref="ContentType"/> is <see cref="AnthropicAdvisorToolResultContentTypes.AdvisorResult"/>.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Encrypted blob when <see cref="ContentType"/> is <see cref="AnthropicAdvisorToolResultContentTypes.AdvisorRedactedResult"/>.
    /// Round-trip verbatim on subsequent turns.
    /// </summary>
    public string? EncryptedContent { get; set; }

    /// <summary>
    /// Error code when <see cref="ContentType"/> is <see cref="AnthropicAdvisorToolResultContentTypes.AdvisorToolResultError"/>.
    /// </summary>
    public AnthropicAdvisorToolResultErrorCodes? ErrorCode { get; set; }

    /// <summary>
    /// Advisor sub-call stop reason when <c>max_tokens</c> is set on the tool definition
    /// (e.g. <c>end_turn</c> or <c>max_tokens</c> when the cap is hit).
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// Raw JSON of the <c>content</c> object for multi-turn passthrough.
    /// </summary>
    public string? RawContentJson { get; set; }
}

/// <summary>
/// Per-iteration usage entry when the advisor tool is used.
/// </summary>
public class VendorAnthropicUsageIteration
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonProperty("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }

    [JsonProperty("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }
}
