using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
///     Marker interface for all Responses API event types.
/// </summary>
public interface IResponseEvent
{
    /// <summary>
    ///     The type of this response event.
    /// </summary>
    [JsonIgnore]
    ResponseEventTypes EventType { get; }
} 