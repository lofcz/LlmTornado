using System;
using System.Runtime.Serialization;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// Anthropic <c>output_config.effort</c> levels. Pair with
/// <see cref="AnthropicThinkingTypes.Adaptive"/> (<c>thinking.type = "adaptive"</c>) on supported models.
/// GA on Claude Opus 4.6+, Sonnet 4.6, Opus 4.7/4.8; beta on Opus 4.5.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicEffortLevels
{
    /// <summary>Minimizes thinking; skips thinking for simple tasks when using adaptive mode.</summary>
    [EnumMember(Value = "low")]
    Low,

    /// <summary>Moderate thinking; may skip thinking for very simple queries.</summary>
    [EnumMember(Value = "medium")]
    Medium,

    /// <summary>Default API behavior — Claude almost always thinks on complex tasks.</summary>
    [EnumMember(Value = "high")]
    High,

    /// <summary>Deep extended exploration. Claude Opus 4.7+ and Opus 4.8.</summary>
    [EnumMember(Value = "xhigh")]
    XHigh,

    /// <summary>Maximum thinking depth with no constraints. Opus 4.6+, Sonnet 4.6, Opus 4.7/4.8.</summary>
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
