using System;
using Newtonsoft.Json;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Rubric for outcome grading — inline text or uploaded file.
/// </summary>
[JsonConverter(typeof(AnthropicManagedAgentRubricConverter))]
public class AnthropicManagedAgentRubric
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    /// <summary>
    /// Inline rubric markdown (when <see cref="Type"/> is <c>text</c>).
    /// </summary>
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    /// <summary>
    /// Files API file ID (when <see cref="Type"/> is <c>file</c>).
    /// </summary>
    [JsonProperty("file_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? FileId { get; set; }

    public static AnthropicManagedAgentRubric Text(string content) => new() { Type = "text", Content = content };

    public static AnthropicManagedAgentRubric File(string fileId) => new() { Type = "file", FileId = fileId };
}

/// <summary>
/// <c>user.define_outcome</c> event — starts outcome-oriented work on a session.
/// </summary>
public class AnthropicManagedAgentUserDefineOutcomeEvent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "user.define_outcome";

    /// <summary>
    /// What the agent should produce.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("rubric")]
    public AnthropicManagedAgentRubric Rubric { get; set; } = AnthropicManagedAgentRubric.Text(string.Empty);

    /// <summary>
    /// Eval→revision cycles before giving up. Default 3, max 20.
    /// </summary>
    [JsonProperty("max_iterations", NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxIterations { get; set; }
}

/// <summary>
/// Echo of a sent <c>user.define_outcome</c> event.
/// </summary>
public class AnthropicManagedAgentUserDefineOutcomeEventResponse : AnthropicManagedAgentUserDefineOutcomeEvent
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("outcome_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? OutcomeId { get; set; }

    [JsonProperty("processed_at", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProcessedAt { get; set; }
}

/// <summary>
/// Per-outcome evaluation state on a session.
/// </summary>
public class AnthropicManagedAgentOutcomeEvaluation
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("outcome_id")]
    public string? OutcomeId { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <c>pending</c>, <c>running</c>, <c>evaluating</c>, <c>satisfied</c>, <c>needs_revision</c>, <c>max_iterations_reached</c>, <c>failed</c>, <c>interrupted</c>.
    /// </summary>
    [JsonProperty("result")]
    public string? Result { get; set; }

    [JsonProperty("explanation", NullValueHandling = NullValueHandling.Ignore)]
    public string? Explanation { get; set; }

    [JsonProperty("iteration")]
    public int? Iteration { get; set; }

    [JsonProperty("completed_at", NullValueHandling = NullValueHandling.Ignore)]
    public string? CompletedAt { get; set; }
}

/// <summary>
/// Terminal results from <c>span.outcome_evaluation_end</c>.
/// </summary>
public static class AnthropicManagedAgentOutcomeResults
{
    public const string Satisfied = "satisfied";
    public const string NeedsRevision = "needs_revision";
    public const string MaxIterationsReached = "max_iterations_reached";
    public const string Failed = "failed";
    public const string Interrupted = "interrupted";
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Evaluating = "evaluating";
}

internal sealed class AnthropicManagedAgentRubricConverter : JsonConverter<AnthropicManagedAgentRubric>
{
    public override AnthropicManagedAgentRubric? ReadJson(JsonReader reader, Type objectType, AnthropicManagedAgentRubric? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize<AnthropicManagedAgentRubric>(reader);
    }

    public override void WriteJson(JsonWriter writer, AnthropicManagedAgentRubric? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        if (value.Type is not null)
        {
            writer.WritePropertyName("type");
            writer.WriteValue(value.Type);
        }

        if (value.Content is not null)
        {
            writer.WritePropertyName("content");
            writer.WriteValue(value.Content);
        }

        if (value.FileId is not null)
        {
            writer.WritePropertyName("file_id");
            writer.WriteValue(value.FileId);
        }

        writer.WriteEndObject();
    }
}
