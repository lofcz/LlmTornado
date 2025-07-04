using System.Collections.Generic;
using LlmTornado.Common;
using LlmTornado.Responses;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when the response is in progress.
/// </summary>
public class ResponseInProgressEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.in_progress".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.in_progress";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The full response object returned by the API.
    /// </summary>
    [JsonProperty("response")]
    public ResponseResult Response { get; set; } = null!;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseInProgress;
}

/// <summary>
/// An error object returned when the model fails to generate a Response.
/// </summary>
public class ResponseError
{
    /// <summary>
    /// The error code for the response.
    /// </summary>
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Details about why the response is incomplete.
/// </summary>
public class ResponseIncompleteDetails
{
    /// <summary>
    /// The reason why the response is incomplete.
    /// </summary>
    [JsonProperty("reason")]
    public string Reason { get; set; } = string.Empty;
} 