using LlmTornado.Responses;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when a response is queued and waiting to be processed.
/// </summary>
public class ResponseQueuedEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always 'response.queued'.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.queued";

    /// <summary>
    /// The sequence number for this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The full response object that is queued.
    /// </summary>
    [JsonProperty("response")]
    public ResponseResult Response { get; set; } = null!;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseQueued;
} 