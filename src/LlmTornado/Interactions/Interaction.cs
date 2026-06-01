using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Interactions;

/// <summary>
/// A Gemini Interactions API resource (model or managed agent run).
/// </summary>
public class Interaction : ApiResultBase
{
    /// <summary>
    /// Resource type, always <c>interaction</c>.
    /// </summary>
    [JsonProperty("object")]
    public string? Object { get; set; }

    /// <summary>
    /// Unique interaction ID.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Model used when this interaction was created with <c>model</c>.
    /// </summary>
    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public string? Model { get; set; }

    /// <summary>
    /// Managed agent ID when this interaction was created with <c>agent</c>.
    /// </summary>
    [JsonProperty("agent", NullValueHandling = NullValueHandling.Ignore)]
    public string? Agent { get; set; }

    /// <summary>
    /// Interaction status.
    /// </summary>
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Parsed <see cref="Status"/> value.
    /// </summary>
    [JsonIgnore]
    public InteractionStatus StatusEnum => Status switch
    {
        "in_progress" => InteractionStatus.InProgress,
        "requires_action" => InteractionStatus.RequiresAction,
        "completed" => InteractionStatus.Completed,
        "failed" => InteractionStatus.Failed,
        "cancelled" => InteractionStatus.Cancelled,
        "incomplete" => InteractionStatus.Incomplete,
        "budget_exceeded" => InteractionStatus.BudgetExceeded,
        _ => InteractionStatus.Unknown
    };

    /// <summary>
    /// ISO 8601 creation time.
    /// </summary>
    [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
    public string? Created { get; set; }

    /// <summary>
    /// ISO 8601 last update time.
    /// </summary>
    [JsonProperty("updated", NullValueHandling = NullValueHandling.Ignore)]
    public string? Updated { get; set; }

    /// <summary>
    /// Environment ID for sandbox-backed interactions.
    /// </summary>
    [JsonProperty("environment_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? EnvironmentId { get; set; }

    /// <summary>
    /// Request input echoed when <c>include_input</c> is set on GET.
    /// </summary>
    [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionInput? Input { get; set; }

    /// <summary>
    /// Reasoning and tool steps produced by the interaction (May 2026 schema).
    /// </summary>
    [JsonProperty("steps", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionStep>? Steps { get; set; }

    /// <summary>
    /// Legacy flat outputs array (pre-May 2026). Normalized into <see cref="Steps"/> after deserialization.
    /// </summary>
    [JsonProperty("outputs", NullValueHandling = NullValueHandling.Ignore)]
    internal List<InteractionLegacyOutput>? LegacyOutputs { get; set; }

    /// <summary>
    /// Whether this interaction was deserialized from the legacy <c>outputs</c> schema.
    /// </summary>
    [JsonIgnore]
    public bool UsesLegacyResponseSchema { get; internal set; }

    /// <summary>
    /// Token usage for the interaction.
    /// </summary>
    [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionUsage? Usage { get; set; }

    /// <summary>
    /// System instruction from the request.
    /// </summary>
    [JsonProperty("system_instruction", NullValueHandling = NullValueHandling.Ignore)]
    public string? SystemInstruction { get; set; }

    /// <summary>
    /// Previous interaction ID from the request.
    /// </summary>
    [JsonProperty("previous_interaction_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? PreviousInteractionId { get; set; }

    /// <summary>
    /// Error details when <see cref="Status"/> is <c>failed</c>.
    /// </summary>
    [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionError? Error { get; set; }

    /// <summary>
    /// Convenience text extracted from the last <c>model_output</c> step (SDK parity).
    /// Works with both <see cref="Steps"/> and legacy <c>outputs</c> responses.
    /// </summary>
    [JsonIgnore]
    public string? OutputText
    {
        get
        {
            InteractionStep? lastOutput = Steps?.LastOrDefault(s => s.Type == "model_output");
            if (lastOutput?.Content is not null)
            {
                return string.Concat(lastOutput.Content.Where(c => c.Type == "text" && c.Text is not null).Select(c => c.Text));
            }

            InteractionLegacyOutput? legacyText = LegacyOutputs?.LastOrDefault(o => o.Type == "text");
            return legacyText?.Text;
        }
    }

    /// <summary>
    /// Convenience image from the last image block in the final <c>model_output</c> step.
    /// </summary>
    [JsonIgnore]
    public InteractionContent? OutputImage
    {
        get
        {
            InteractionStep? lastOutput = Steps?.LastOrDefault(s => s.Type == "model_output");
            return lastOutput?.Content?.LastOrDefault(c => c.Type == "image");
        }
    }

    /// <summary>
    /// Convenience audio from the last audio block in the final <c>model_output</c> step.
    /// </summary>
    [JsonIgnore]
    public InteractionContent? OutputAudio
    {
        get
        {
            InteractionStep? lastOutput = Steps?.LastOrDefault(s => s.Type == "model_output");
            return lastOutput?.Content?.LastOrDefault(c => c.Type == "audio");
        }
    }
}
