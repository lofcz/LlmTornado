using System;
using System.Collections.Generic;
using LlmTornado.Assistants;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     An invocation of an Assistant on a Thread.
///     The Assistant uses it's configuration and the Thread's Messages to perform tasks by calling models and tools.
///     As part of a Run, the Assistant appends Messages to the Thread.
/// </summary>
public sealed class TornadoRun : ApiResultBase
{
    /// <summary>
    ///     The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the assistant was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt
    {
        get => CreatedUnixTime ?? 0;
        set => CreatedUnixTime = value;
    }

    /// <summary>
    ///     The thread ID that this run belongs to.
    /// </summary>
    [JsonProperty("thread_id")]
    public string ThreadId { get; set; } = null!;

    /// <summary>
    ///     The ID of the assistant used for execution of this run.
    /// </summary>
    [JsonProperty("assistant_id")]
    public string AssistantId { get; set; } = null!;

    /// <summary>
    ///     The status of the run.
    /// </summary>
    [JsonProperty("status")]
    public RunStatus Status { get; set; }

    /// <summary>
    ///     Details on the action required to continue the run.
    ///     Will be null if no action is required.
    /// </summary>
    [JsonProperty("required_action")]
    public RequiredAction? RequiredAction { get; private set; }

    /// <summary>
    ///     The Last error Associated with this run.
    ///     Will be null if there are no errors.
    /// </summary>
    [JsonProperty("last_error")]
    public Error? LastError { get; private set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run will expire.
    /// </summary>
    [JsonProperty("expires_at")]
    public int? ExpiresAtUnixTimeSeconds { get; set; }

    /// <summary>
    /// The expiration date and time for the current response, converted from Unix time, if available.
    /// </summary>
    [JsonIgnore]
    public DateTime? ExpiresAt
        => ExpiresAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(ExpiresAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run was started.
    /// </summary>
    [JsonProperty("started_at")]
    public int? StartedAtUnixTimeSeconds { get; private set; }

    /// <summary>
    /// The timestamp indicating when the process or operation started.
    /// </summary>
    [JsonIgnore]
    public DateTime? StartedAt
        => StartedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(StartedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run was cancelled.
    /// </summary>
    [JsonProperty("cancelled_at")]
    public int? CancelledAtUnixTimeSeconds { get; private set; }

    /// <summary>
    /// The timestamp indicating when the operation was cancelled, if applicable.
    /// </summary>
    [JsonIgnore]
    public DateTime? CancelledAt
        => CancelledAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CancelledAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run failed.
    /// </summary>
    [JsonProperty("failed_at")]
    public int? FailedAtUnixTimeSeconds { get; private set; }

    /// <summary>
    /// The timestamp indicating when the operation failed, represented as a <see cref="DateTime"/> object.
    /// </summary>
    [JsonIgnore]
    public DateTime? FailedAt
        => FailedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(FailedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the run was completed.
    /// </summary>
    [JsonProperty("completed_at")]
    public int? CompletedAtUnixTimeSeconds { get; private set; }

    /// <summary>
    /// The timestamp indicating when the operation was completed.
    /// </summary>
    [JsonIgnore]
    public DateTime? CompletedAt
        => CompletedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CompletedAtUnixTimeSeconds.Value).DateTime
            : null;

    /// <summary>
    ///     The instructions that the assistant used for this run.
    /// </summary>
    [JsonProperty("instructions")]
    public string Instructions { get; set; } = null!;

    /// <summary>
    ///     The list of tools that the assistant used for this run.
    /// </summary>
    [JsonProperty("tools")]
    [JsonConverter(typeof(AssistantToolConverter))]
    public IReadOnlyList<AssistantTool> Tools { get; set; } = null!;

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = null!;

    /// <summary>
    ///     Usage statistics related to the run. This value will be `null` if the run is not in a terminal state (i.e.
    ///     `in_progress`, `queued`, etc.).
    /// </summary>
    [JsonProperty("usage")]
    public RunUsage? Usage { get; set; }
    
    /// <summary>
    ///     What sampling temperature to use, between 0 and 2.
    ///     Higher values like 0.8 will make the output more random,
    ///     while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    ///     An alternative to sampling with temperature, called nucleus sampling,
    ///     where the model considers the results of the tokens with top_p probability mass.
    ///     So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    ///     We generally recommend altering this or temperature but not both.
    /// </summary>
    [JsonProperty("top_p")]
    public double? TopP { get; set; }
    
    /// <summary>
    ///     The maximum number of prompt tokens specified to have been used over the course of the run.
    /// </summary>
    [JsonProperty("max_tokens")]
    public int? MaxPromptTokens { get; set; }
    
    /// <summary>
    ///     The maximum number of completion tokens specified to have been used over the course of the run.
    /// </summary>
    [JsonProperty("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }

    /// <summary>
    ///     Controls for how a thread will be truncated prior to the run. Use this to control the initial context window of the run.
    /// </summary>
    [JsonProperty("truncation_strategy")]
    public TruncationStrategy TruncationStrategy { get; set; } = null!;
    
    /// <summary>
    ///     Controls which (if any) tool is called by the model.
    ///     none means the model will not call any tools and instead generates a message.
    ///     auto is the default value and means the model can pick between generating a message or calling one or more tools.
    ///     required means the model must call one or more tools before responding to the user.
    ///     Specifying a particular tool like {"type": "file_search"} or {"type": "function", "function": {"name": "my_function"}}
    ///     forces the model to call that tool.
    /// </summary>
    [JsonProperty("tool_choice")]
    [JsonConverter(typeof(ToolChoiceConverter))]
    public ToolChoice ToolChoice { get; set; } = null!;

    /// <summary>
    ///     Whether to enable parallel function calling during tool use.
    /// </summary>
    [JsonProperty("parallel_tool_calls")] 
    public bool ParallelToolCalls { get; set; }

    /// <summary>
    ///     Specifies the format that the model must output. 
    /// </summary>
    [JsonProperty("response_format")]
    [JsonConverter(typeof(ResponseFormatConverter))]
    public ResponseFormat ResponseFormat { get; set; } = null!;
}