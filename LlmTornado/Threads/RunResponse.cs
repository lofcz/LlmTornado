using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     An invocation of an Assistant on a Thread.
///     The Assistant uses it's configuration and the Thread's Messages to perform tasks by calling models and tools.
///     As part of a Run, the Assistant appends Messages to the Thread.
/// </summary>
public sealed class RunResponse : BaseResponse
{
    /// <summary>
    ///     The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonInclude]
    [JsonProperty("id")]
    public string Id { get; private set; }

    /// <summary>
    ///     The object type, which is always run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("object")]
    public string Object { get; private set; }

    /// <summary>
    ///     The thread ID that this run belongs to.
    /// </summary>
    [JsonInclude]
    [JsonProperty("thread_id")]
    public string ThreadId { get; private set; }

    /// <summary>
    ///     The ID of the assistant used for execution of this run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("assistant_id")]
    public string AssistantId { get; private set; }

    /// <summary>
    ///     The status of the run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("status")]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter<RunStatus>))]
    public RunStatus Status { get; private set; }

    /// <summary>
    ///     Details on the action required to continue the run.
    ///     Will be null if no action is required.
    /// </summary>
    [JsonInclude]
    [JsonProperty("required_action")]
    public RequiredAction RequiredAction { get; private set; }

    /// <summary>
    ///     The Last error Associated with this run.
    ///     Will be null if there are no errors.
    /// </summary>
    [JsonInclude]
    [JsonProperty("last_error")]
    public Error LastError { get; private set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the thread was created.
    /// </summary>
    [JsonInclude]
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run will expire.
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
    ///     The Unix timestamp (in seconds) for when the run was started.
    /// </summary>
    [JsonInclude]
    [JsonProperty("started_at")]
    public int? StartedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? StartedAt
        => StartedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(StartedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run was cancelled.
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
    ///     The Unix timestamp (in seconds) for when the run failed.
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
    ///     The Unix timestamp (in seconds) for when the run was completed.
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
    ///     The model that the assistant used for this run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("model")]
    public string Model { get; private set; }

    /// <summary>
    ///     The instructions that the assistant used for this run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("instructions")]
    public string Instructions { get; private set; }

    /// <summary>
    ///     The list of tools that the assistant used for this run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("tools")]
    public IReadOnlyList<Tool> Tools { get; private set; }

    /// <summary>
    ///     The list of File IDs the assistant used for this run.
    /// </summary>
    [JsonInclude]
    [JsonProperty("file_ids")]
    public IReadOnlyList<string> FileIds { get; private set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonInclude]
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    /// <summary>
    ///     Usage statistics related to the run. This value will be `null` if the run is not in a terminal state (i.e.
    ///     `in_progress`, `queued`, etc.).
    /// </summary>
    [JsonInclude]
    [JsonProperty("usage")]
    public Usage Usage { get; private set; }

    public static implicit operator string(RunResponse run)
    {
        return run?.ToString();
    }

    public override string ToString()
    {
        return Id;
    }
}