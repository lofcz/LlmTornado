using LlmTornado.Responses;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when a response fails.
/// </summary>
public class ResponseEventFailed : IResponseEvent
{
    /// <summary>
    /// The type of the event. Always "response.failed".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.failed";

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
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseFailed;
} 