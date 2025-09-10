using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when a partial code snippet is streamed by the code interpreter.
/// </summary>
public class ResponseEventCodeInterpreterCallCodeDelta : IResponseEvent
{
    /// <summary>
    /// The partial code snippet being streamed by the code interpreter.
    /// </summary>
    [JsonProperty("delta")]
    public string Delta { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the code interpreter tool call item.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item in the response for which the code is being streamed.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The sequence number of this event, used to order streaming events.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The type of the event. Always 'response.code_interpreter_call_code.delta'.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.code_interpreter_call_code.delta";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseCodeInterpreterCallCodeDelta;
}