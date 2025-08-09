using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when the code snippet is finalized by the code interpreter.
/// </summary>
public class ResponseEventCodeInterpreterCallCodeDone : IResponseEvent
{
    /// <summary>
    /// The final code snippet output by the code interpreter.
    /// </summary>
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the code interpreter tool call item.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item in the response for which the code is finalized.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The sequence number of this event, used to order streaming events.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The type of the event. Always 'response.code_interpreter_call_code.done'.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.code_interpreter_call_code.done";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseCodeInterpreterCallCodeDone;
}