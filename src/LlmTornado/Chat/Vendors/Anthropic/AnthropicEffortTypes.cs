using System;
using System.Runtime.Serialization;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// Anthropic <c>output_config.effort</c> levels (GA on Claude Opus 4.6+, Sonnet 4.6, and beta on Opus 4.5).
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicEffortLevels
{
    /// <summary>Low effort — faster, lower latency.</summary>
    [EnumMember(Value = "low")]
    Low,

    /// <summary>Medium effort — balanced.</summary>
    [EnumMember(Value = "medium")]
    Medium,

    /// <summary>High effort — more thorough reasoning.</summary>
    [EnumMember(Value = "high")]
    High,

    /// <summary>Extra-high effort (Opus 4.6+).</summary>
    [EnumMember(Value = "xhigh")]
    XHigh,

    /// <summary>Maximum effort (Opus 4.6+).</summary>
    [EnumMember(Value = "max")]
    Max
}

/// <summary>
/// Maps effort values to Anthropic Messages API <c>output_config.effort</c> strings.
/// </summary>
public static class AnthropicEffortHelper
{
    /// <summary>
    /// Returns the API string for <see cref="AnthropicEffortLevels"/>.
    /// </summary>
    public static string ToApiValue(AnthropicEffortLevels effort) => effort switch
    {
        AnthropicEffortLevels.Low => "low",
        AnthropicEffortLevels.Medium => "medium",
        AnthropicEffortLevels.High => "high",
        AnthropicEffortLevels.XHigh => "xhigh",
        AnthropicEffortLevels.Max => "max",
        _ => throw new ArgumentOutOfRangeException(nameof(effort), effort, "Unsupported Anthropic effort level.")
    };

    /// <summary>
    /// Maps harmonized <see cref="ChatReasoningEfforts"/> to Anthropic effort API values.
    /// </summary>
    public static string? ToApiValue(ChatReasoningEfforts effort) => effort switch
    {
        ChatReasoningEfforts.Low => "low",
        ChatReasoningEfforts.Medium => "medium",
        ChatReasoningEfforts.High => "high",
        ChatReasoningEfforts.XHigh => "xhigh",
        ChatReasoningEfforts.Max => "max",
        _ => null
    };
}
