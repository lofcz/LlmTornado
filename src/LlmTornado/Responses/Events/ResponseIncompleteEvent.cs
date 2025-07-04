using LlmTornado.Responses.Events;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when a response finishes as incomplete.
/// </summary>
public class ResponseIncompleteEvent : ResponseInProgressEvent
{
    /// <summary>
    /// The type of the event. Always "response.incomplete".
    /// </summary>
    [JsonProperty("type")]
    public override string Type { get; set; } = "response.incomplete";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseIncomplete;
} 