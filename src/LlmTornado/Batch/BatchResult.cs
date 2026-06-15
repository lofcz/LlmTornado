using LlmTornado.Batch.Vendors.Anthropic;
using LlmTornado.Batch.Vendors.Google;
using LlmTornado.Batch.Vendors.OpenAi;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Videos;
using Newtonsoft.Json;

namespace LlmTornado.Batch;

/// <summary>
/// An individual result from a batch.
/// </summary>
public class BatchResult
{
    /// <summary>
    /// Developer-provided ID for matching results to requests.
    /// </summary>
    [JsonProperty("custom_id")]
    public string CustomId { get; set; } = string.Empty;
    
    /// <summary>
    /// The result content when using inline result format.
    /// </summary>
    [JsonProperty("result")]
    internal BatchResultContent? ResultInternal { get; set; }
    
    /// <summary>
    /// The response object when using response format.
    /// </summary>
    [JsonProperty("response")]
    internal BatchResultResponse? ResponseInternal { get; set; }
    
    /// <summary>
    /// Error information if the request failed.
    /// </summary>
    [JsonProperty("error")]
    public BatchError? Error { get; set; }
    
    /// <summary>
    /// Unique identifier for the batch request.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }
    
    /// <summary>
    /// The result type (succeeded, errored, canceled, expired).
    /// </summary>
    [JsonIgnore]
    public BatchResultType ResultType => ResultInternal?.Type ?? 
        (ResponseInternal?.StatusCode is >= 200 and < 300 ? BatchResultType.Succeeded : 
         Error is not null ? BatchResultType.Errored : BatchResultType.Unknown);
    
    /// <summary>
    /// The chat completion result if the request succeeded.
    /// </summary>
    [JsonIgnore]
    public ChatResult? ChatResult => ResultInternal?.Message ?? ResponseInternal?.Body;

    /// <summary>
    /// The video generation result if the batch item targeted /v1/videos.
    /// </summary>
    [JsonIgnore]
    public VideoJob? VideoResult
    {
        get
        {
            if (ResponseInternal?.Body is not null)
            {
                return null;
            }

            if (ResponseInternal?.VideoBody is not null)
            {
                ResponseInternal.VideoBody.SourceProvider = LLmProviders.OpenAi;
                return ResponseInternal.VideoBody;
            }

            return null;
        }
    }
    
    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    [JsonIgnore]
    public int? StatusCode => ResponseInternal?.StatusCode;
    
    /// <summary>
    /// Unique request identifier.
    /// </summary>
    [JsonIgnore]
    public string? RequestId => ResponseInternal?.RequestId;
    
    /// <summary>
    /// The raw JSON response for debugging purposes.
    /// </summary>
    [JsonIgnore]
    public string? RawResponse { get; internal set; }
    
    /// <summary>
    /// Deserializes a batch result from JSON, handling provider-specific formats.
    /// </summary>
    /// <param name="provider">The provider that generated the result.</param>
    /// <param name="jsonData">The JSON data to deserialize.</param>
    /// <returns>The deserialized batch result.</returns>
    public static BatchResult? Deserialize(LLmProviders provider, string jsonData)
    {
        return provider switch
        {
            LLmProviders.Anthropic => VendorAnthropicBatchResult.Deserialize(jsonData),
            LLmProviders.Google => VendorGoogleBatchResult.Deserialize(jsonData),
            _ => VendorOpenAiBatchResult.Deserialize(jsonData)
        };
    }

    /// <summary>
    /// String representation.
    /// </summary>
    public override string ToString()
    {
        return ChatResult?.ToString() ?? $"[raw response: {RawResponse}\n, status code: {StatusCode}]";
    }
}

/// <summary>
/// The response portion of a batch result.
/// </summary>
internal class BatchResultResponse
{
    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }
    
    /// <summary>
    /// Unique request identifier.
    /// </summary>
    [JsonProperty("request_id")]
    public string? RequestId { get; set; }
    
    /// <summary>
    /// The response body containing the chat result.
    /// </summary>
    [JsonProperty("body")]
    public ChatResult? Body { get; set; }

    /// <summary>
    /// Parsed video result when the batch item targeted /v1/videos.
    /// </summary>
    [JsonIgnore]
    public VideoJob? VideoBody { get; set; }
}

/// <summary>
/// Content of a batch result.
/// </summary>
internal class BatchResultContent
{
    /// <summary>
    /// The type of result: succeeded, errored, canceled, or expired.
    /// </summary>
    [JsonProperty("type")]
    public BatchResultType Type { get; set; }
    
    /// <summary>
    /// The message result if the request succeeded.
    /// </summary>
    [JsonProperty("message")]
    public ChatResult? Message { get; set; }
    
    /// <summary>
    /// Error information if the request failed.
    /// </summary>
    [JsonProperty("error")]
    public BatchResultError? Error { get; set; }
    
    internal BatchResultContent() { }
}

/// <summary>
/// Error response in a batch result.
/// </summary>
internal class BatchResultError
{
    /// <summary>
    /// The type of error response.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }
    
    /// <summary>
    /// The error details.
    /// </summary>
    [JsonProperty("error")]
    public BatchError? Error { get; set; }
}
