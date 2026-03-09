using System.Collections.Generic;
using System.Threading;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Request for the standalone compact endpoint (POST /responses/compact).
/// Compacts a conversation context window into a smaller representation while preserving
/// the state needed for subsequent turns.
/// </summary>
public class ResponseCompactRequest
{
    /// <summary>
    /// Cancellation token to use with the request.
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    /// <summary>
    /// Model ID used to generate the compacted response.
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(ChatModelJsonConverter))]
    public ChatModel? Model { get; set; }

    /// <summary>
    /// Text input for text requests.
    /// </summary>
    [JsonIgnore]
    public string? InputString { get; set; }

    /// <summary>
    /// Multi-part, multi-modal input items.
    /// </summary>
    [JsonIgnore]
    public List<ResponseInputItem>? InputItems { get; set; }

    /// <summary>
    /// Text, image, or file inputs to the model, used to generate a response.
    /// Can be either a simple string or a list of input items.
    /// </summary>
    [JsonProperty("input")]
    internal object? Input
    {
        get
        {
            if (InputItems?.Count > 0)
            {
                return InputItems;
            }

            return InputString;
        }
    }

    /// <summary>
    /// A system (or developer) message inserted into the model's context.
    /// When used along with previous_response_id, the instructions from a previous response
    /// will not be carried over to the next response.
    /// </summary>
    [JsonProperty("instructions")]
    public string? Instructions { get; set; }

    /// <summary>
    /// The unique ID of the previous response to the model. Use this to create multi-turn conversations.
    /// Cannot be used in conjunction with input items.
    /// </summary>
    [JsonProperty("previous_response_id")]
    public string? PreviousResponseId { get; set; }

    /// <summary>
    /// Creates a new empty compact request.
    /// </summary>
    public ResponseCompactRequest()
    {
    }

    /// <summary>
    /// Creates a compact request using a previous response ID.
    /// </summary>
    /// <param name="model">The model to use for compaction.</param>
    /// <param name="previousResponseId">The ID of the previous response to compact.</param>
    public ResponseCompactRequest(ChatModel model, string previousResponseId)
    {
        Model = model;
        PreviousResponseId = previousResponseId;
    }

    /// <summary>
    /// Creates a compact request using input items.
    /// </summary>
    /// <param name="model">The model to use for compaction.</param>
    /// <param name="inputItems">The input items to compact.</param>
    public ResponseCompactRequest(ChatModel model, List<ResponseInputItem> inputItems)
    {
        Model = model;
        InputItems = inputItems;
    }

    /// <summary>
    /// Serializes the compact request into the request body.
    /// </summary>
    internal string Serialize()
    {
        return JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }
}

/// <summary>
/// Result of a compact request (POST /responses/compact).
/// Contains the compacted output items and usage statistics.
/// </summary>
public class ResponseCompactResult
{
    /// <summary>
    /// The unique identifier for the compacted response.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp (in seconds) when the compacted conversation was created.
    /// </summary>
    [JsonProperty("created_at")]
    public int? CreatedAt { get; set; }

    /// <summary>
    /// The object type. Always "response.compaction".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "response.compaction";

    /// <summary>
    /// The compacted list of output items. Pass these as-is into your next /responses call as input.
    /// </summary>
    [JsonProperty("output")]
    [JsonConverter(typeof(ResponseOutputItemListConverter))]
    public List<IResponseOutputItem>? Output { get; set; }

    /// <summary>
    /// Token accounting for the compaction pass, including cached, reasoning, and total tokens.
    /// </summary>
    [JsonProperty("usage")]
    public ResponseUsage? Usage { get; set; }
}
