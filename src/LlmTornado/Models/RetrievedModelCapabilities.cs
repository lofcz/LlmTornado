using Newtonsoft.Json;

namespace LlmTornado.Models;

/// <summary>
/// Whether a model supports a specific capability.
/// </summary>
public class RetrievedModelCapabilitySupport
{
    /// <summary>
    /// Whether this capability is supported by the model.
    /// </summary>
    [JsonProperty("supported")]
    public bool Supported { get; set; }
}

/// <summary>
/// Context management support and available strategies.
/// </summary>
public class RetrievedModelContextManagementCapability : RetrievedModelCapabilitySupport
{
    /// <summary>
    /// Whether clear_thinking_20251015 is supported.
    /// </summary>
    [JsonProperty("clear_thinking_20251015")]
    public RetrievedModelCapabilitySupport? ClearThinking20251015 { get; set; }

    /// <summary>
    /// Whether clear_tool_uses_20250919 is supported.
    /// </summary>
    [JsonProperty("clear_tool_uses_20250919")]
    public RetrievedModelCapabilitySupport? ClearToolUses20250919 { get; set; }

    /// <summary>
    /// Whether compact_20260112 is supported.
    /// </summary>
    [JsonProperty("compact_20260112")]
    public RetrievedModelCapabilitySupport? Compact20260112 { get; set; }
}

/// <summary>
/// Effort (reasoning_effort) support and available levels.
/// </summary>
public class RetrievedModelEffortCapability : RetrievedModelCapabilitySupport
{
    /// <summary>
    /// Whether the model supports high effort level.
    /// </summary>
    [JsonProperty("high")]
    public RetrievedModelCapabilitySupport? High { get; set; }

    /// <summary>
    /// Whether the model supports low effort level.
    /// </summary>
    [JsonProperty("low")]
    public RetrievedModelCapabilitySupport? Low { get; set; }

    /// <summary>
    /// Whether the model supports max effort level.
    /// </summary>
    [JsonProperty("max")]
    public RetrievedModelCapabilitySupport? Max { get; set; }

    /// <summary>
    /// Whether the model supports medium effort level.
    /// </summary>
    [JsonProperty("medium")]
    public RetrievedModelCapabilitySupport? Medium { get; set; }

    /// <summary>
    /// Whether the model supports xhigh effort level.
    /// </summary>
    [JsonProperty("xhigh")]
    public RetrievedModelCapabilitySupport? XHigh { get; set; }
}

/// <summary>
/// Supported thinking type configurations.
/// </summary>
public class RetrievedModelThinkingTypes
{
    /// <summary>
    /// Whether the model supports thinking with type 'adaptive' (auto).
    /// </summary>
    [JsonProperty("adaptive")]
    public RetrievedModelCapabilitySupport? Adaptive { get; set; }

    /// <summary>
    /// Whether the model supports thinking with type 'enabled'.
    /// </summary>
    [JsonProperty("enabled")]
    public RetrievedModelCapabilitySupport? Enabled { get; set; }
}

/// <summary>
/// Thinking capability and supported type configurations.
/// </summary>
public class RetrievedModelThinkingCapability : RetrievedModelCapabilitySupport
{
    /// <summary>
    /// Supported thinking type configurations.
    /// </summary>
    [JsonProperty("types")]
    public RetrievedModelThinkingTypes? Types { get; set; }
}

/// <summary>
/// Model capability information returned by the Anthropic Models API.
/// </summary>
public class RetrievedModelCapabilities
{
    /// <summary>
    /// Whether the model supports the Batch API.
    /// </summary>
    [JsonProperty("batch")]
    public RetrievedModelCapabilitySupport? Batch { get; set; }

    /// <summary>
    /// Whether the model supports citation generation.
    /// </summary>
    [JsonProperty("citations")]
    public RetrievedModelCapabilitySupport? Citations { get; set; }

    /// <summary>
    /// Whether the model supports code execution tools.
    /// </summary>
    [JsonProperty("code_execution")]
    public RetrievedModelCapabilitySupport? CodeExecution { get; set; }

    /// <summary>
    /// Context management support and available strategies.
    /// </summary>
    [JsonProperty("context_management")]
    public RetrievedModelContextManagementCapability? ContextManagement { get; set; }

    /// <summary>
    /// Effort (reasoning_effort) support and available levels.
    /// </summary>
    [JsonProperty("effort")]
    public RetrievedModelEffortCapability? Effort { get; set; }

    /// <summary>
    /// Whether the model accepts image content blocks.
    /// </summary>
    [JsonProperty("image_input")]
    public RetrievedModelCapabilitySupport? ImageInput { get; set; }

    /// <summary>
    /// Whether the model accepts PDF content blocks.
    /// </summary>
    [JsonProperty("pdf_input")]
    public RetrievedModelCapabilitySupport? PdfInput { get; set; }

    /// <summary>
    /// Whether the model supports structured output / JSON mode / strict tool schemas.
    /// </summary>
    [JsonProperty("structured_outputs")]
    public RetrievedModelCapabilitySupport? StructuredOutputs { get; set; }

    /// <summary>
    /// Thinking capability and supported type configurations.
    /// </summary>
    [JsonProperty("thinking")]
    public RetrievedModelThinkingCapability? Thinking { get; set; }
}
