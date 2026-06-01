using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// A single step in an interaction (model output, tool call, user input, etc.).
/// </summary>
public class InteractionStep
{
    /// <summary>
    /// Step type (e.g. <c>model_output</c>, <c>function_call</c>, <c>user_input</c>, <c>thought</c>).
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Content blocks for model or user steps.
    /// </summary>
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionContent>? Content { get; set; }

    /// <summary>
    /// Thought summary blocks (May 2026 <c>thought</c> steps).
    /// </summary>
    [JsonProperty("summary", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionContent>? Summary { get; set; }

    /// <summary>
    /// Encrypted reasoning signature for thought/tool steps.
    /// </summary>
    [JsonProperty("signature", NullValueHandling = NullValueHandling.Ignore)]
    public string? Signature { get; set; }

    /// <summary>
    /// Function call ID.
    /// </summary>
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    /// <summary>
    /// Function or tool name.
    /// </summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    /// <summary>
    /// Function call arguments.
    /// </summary>
    [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Arguments { get; set; }

    /// <summary>
    /// Function or tool result payload.
    /// </summary>
    [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
    public object? Result { get; set; }

    /// <summary>
    /// Matching function call ID for result steps.
    /// </summary>
    [JsonProperty("call_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? CallId { get; set; }

    /// <summary>
    /// Whether the tool call failed.
    /// </summary>
    [JsonProperty("is_error", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsError { get; set; }
}

/// <summary>
/// Token usage statistics for an interaction.
/// </summary>
public class InteractionUsage
{
    [JsonProperty("total_input_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalInputTokens { get; set; }

    [JsonProperty("total_output_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalOutputTokens { get; set; }

    [JsonProperty("total_cached_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalCachedTokens { get; set; }

    /// <summary>
    /// Thought/reasoning tokens (May 2026 field name).
    /// </summary>
    [JsonProperty("total_thought_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalThoughtTokens { get; set; }

    /// <summary>
    /// Legacy field name for thought tokens (pre-May 2026).
    /// </summary>
    [JsonProperty("total_reasoning_tokens", NullValueHandling = NullValueHandling.Ignore)]
    internal int? LegacyTotalReasoningTokens { get; set; }

    [JsonProperty("total_tool_use_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalToolUseTokens { get; set; }

    [JsonProperty("total_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? TotalTokens { get; set; }
}
