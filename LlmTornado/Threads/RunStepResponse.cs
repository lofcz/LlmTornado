using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     A detailed list of steps the Assistant took as part of a Run.
///     An Assistant can call tools or create Messages during it's run.
///     Examining Run Steps allows you to introspect how the Assistant is getting to it's final results.
/// </summary>
public sealed class RunStepResponse
{
    /// <summary>
    ///     The identifier of the run step, which can be referenced in API endpoints.
    /// </summary>
    [JsonInclude]
    [JsonProperty("id")]
    public string Id { get; private set; }

    [JsonInclude] [JsonProperty("object")] public string Object { get; private set; }

    /// <summary>
    ///     The ID of the assistant associated with the run step.
    /// </summary>
    [JsonInclude]
    [JsonProperty("assistant_id")]
    public string AssistantId { get; private set; }

    /// <summary>
    ///     The ID of the thread that was run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("thread_id")]
    public string ThreadId { get; private set; }

    /// <summary>
    ///     The ID of the run that this run step is a part of.
    /// </summary>
    [JsonInclude]
    [JsonProperty("run_id")]
    public string RunId { get; private set; }

    /// <summary>
    ///     The type of run step.
    /// </summary>
    [JsonInclude]
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter<RunStepType>))]
    public RunStepType Type { get; private set; }

    /// <summary>
    ///     The status of the run step.
    /// </summary>
    [JsonInclude]
    [JsonProperty("status")]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter<RunStatus>))]
    public RunStatus Status { get; private set; }

    /// <summary>
    ///     The details of the run step.
    /// </summary>
    [JsonInclude]
    [JsonProperty("step_details")]
    public StepDetails StepDetails { get; private set; }

    /// <summary>
    ///     The last error associated with this run step. Will be null if there are no errors.
    /// </summary>
    [JsonInclude]
    [JsonProperty("last_error")]
    public Error LastError { get; private set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run step was created.
    /// </summary>
    [JsonInclude]
    [JsonProperty("created_at")]
    public int? CreatedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? CreatedAt
        => CreatedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run step expired. A step is considered expired if the parent run is
    ///     expired.
    /// </summary>
    [JsonInclude]
    [JsonProperty("expires_at")]
    public int? ExpiresAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? ExpiresAt
        => ExpiresAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(ExpiresAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run step was cancelled.
    /// </summary>
    [JsonInclude]
    [JsonProperty("cancelled_at")]
    public int? CancelledAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? CancelledAt
        => CancelledAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CancelledAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run step failed.
    /// </summary>
    [JsonInclude]
    [JsonProperty("failed_at")]
    public int? FailedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? FailedAt
        => FailedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(FailedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run step completed.
    /// </summary>
    [JsonInclude]
    [JsonProperty("completed_at")]
    public int? CompletedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? CompletedAt
        => CompletedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CompletedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonInclude]
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    /// <summary>
    ///     Usage statistics related to the run step. This value will be `null` while the run step's status is `in_progress`.
    /// </summary>
    [JsonInclude]
    [JsonProperty("usage")]
    public Usage Usage { get; private set; }

    public static implicit operator string(RunStepResponse runStep)
    {
        return runStep?.ToString();
    }

    public override string ToString()
    {
        return Id;
    }
}