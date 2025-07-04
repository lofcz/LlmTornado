using LlmTornado.Responses.Events;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when the model response is complete.
/// </summary>
public class ResponseCompletedEvent : ResponseInProgressEvent
{
    /// <summary>
    /// The type of the event. Always "response.completed".
    /// </summary>
    [JsonProperty("type")]
    public override string Type { get; set; } = "response.completed";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseCompleted;
} 