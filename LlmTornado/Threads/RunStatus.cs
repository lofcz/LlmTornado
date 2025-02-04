using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

/// <summary>
/// Represents the various states or outcomes of a run within the Tornado execution framework.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum RunStatus
{
    /// <summary>
    /// Represents a state in which the run is queued and awaiting processing or execution.
    /// </summary>
    [JsonProperty("queued")] Queued,

    /// <summary>
    /// Represents a running state where the operation, task, or run is currently in progress and has not yet completed.
    /// This status indicates active execution within the Tornado framework.
    /// </summary>
    [JsonProperty("in_progress")] InProgress,

    /// <summary>
    /// Indicates that the current run requires user or external action in order to proceed.
    /// </summary>
    [JsonProperty("requires_action")] RequiresAction,

    /// <summary>
    /// Indicates that the run is in the process of being cancelled. This state is used
    /// during the transition when a cancellation request has been initiated but has not
    /// yet been fully processed or completed.
    /// </summary>
    [JsonProperty("cancelling")] Cancelling,

    /// <summary>
    /// Indicates that the execution of a run has been cancelled.
    /// This state reflects that the process was actively interrupted
    /// and did not reach a successful or failed conclusion.
    /// </summary>
    [JsonProperty("cancelled")] Cancelled,

    /// <summary>
    /// Indicates that the run has failed to complete successfully.
    /// This status is assigned when an error or unexpected issue prevents
    /// the operation or process from reaching its intended conclusion.
    /// </summary>
    [JsonProperty("failed")] Failed,

    /// <summary>
    /// Indicates that the run has successfully completed execution without errors or interruptions.
    /// </summary>
    [JsonProperty("completed")] Completed,

    /// <summary>
    /// Represents a state where the run is incomplete. This may indicate that the run did not finish successfully
    /// or that its execution is partially completed and requires further actions to reach a terminal state.
    /// </summary>
    [JsonProperty("incomplete")] Incomplete,

    /// <summary>
    /// Indicates that the run has reached its expiration time without being completed.
    /// This status typically occurs when a run exceeds its allowed time window
    /// or is no longer valid for processing.
    /// </summary>
    [JsonProperty("expired")] Expired
}