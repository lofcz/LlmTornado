using LlmTornado.Responses.Events;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when a response is created.
/// </summary>
public class ResponseCreatedEvent : ResponseInProgressEvent
{
    /// <summary>
    /// The type of the event. Always "response.created".
    /// </summary>
    [JsonProperty("type")]
    public override string Type { get; set; } = "response.created";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseCreated;
} 