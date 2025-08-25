using LlmTornado.Responses;
using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when an annotation is added to output text content.
/// </summary>
public class ResponseEventOutputTextAnnotationAdded : IResponseEvent
{
    /// <summary>
    /// The annotation object being added. (See annotation schema for details.)
    /// </summary>
    [JsonProperty("annotation")]
    public IResponseOutputContentAnnotation Annotation { get; set; } = null!;

    /// <summary>
    /// The index of the annotation within the content part.
    /// </summary>
    [JsonProperty("annotation_index")]
    public int AnnotationIndex { get; set; }

    /// <summary>
    /// The index of the content part within the output item.
    /// </summary>
    [JsonProperty("content_index")]
    public int ContentIndex { get; set; }

    /// <summary>
    /// The unique identifier of the item to which the annotation is being added.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item in the response's output array.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The type of the event. Always 'response.output_text_annotation.added'.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.output_text_annotation.added";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseOutputTextAnnotationAdded;
}