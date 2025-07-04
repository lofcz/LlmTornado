using LlmTornado.Responses.Events;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when a response fails.
/// </summary>
public class ResponseFailedEvent : ResponseInProgressEvent
{
    /// <summary>
    /// The type of the event. Always "response.failed".
    /// </summary>
    [JsonProperty("type")]
    public override string Type { get; set; } = "response.failed";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseFailed;
} 