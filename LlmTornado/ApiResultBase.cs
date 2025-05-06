using System;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Common;
using LlmTornado.Models;
using Newtonsoft.Json;

namespace LlmTornado;


/// <summary>
///     Represents a result from calling the LLM provider API, with all the common metadata returned from every endpoint.
/// </summary>
public class ApiResultBase
{
    /// <summary>
    ///     The time when the result was generated.
    /// </summary>
    [JsonIgnore]
    public DateTime? Created => CreatedUnixTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds(CreatedUnixTime.Value).DateTime : null;

    /// <summary>
    ///     The time when the result was generated in unix epoch format.
    /// </summary>
    [JsonProperty("created")]
    public long? CreatedUnixTime { get; set; }

    /// <summary>
    ///     Which model was used to generate this result.
    /// </summary>
    [JsonProperty("model")]
    public string? Model { get; set; }

    /// <summary>
    ///     Object type, ie: text_completion, file, fine-tune, list, etc.
    /// </summary>
    [JsonProperty("object")]
    public string? Object { get; set; }

    /// <summary>
    ///     The organization associated with the API request, as reported by the API. Only used by OpenAI and Azure OpenAI.
    /// </summary>
    [JsonIgnore]
    public string? Organization { get; internal set; }

    /// <summary>
    ///     The server-side processing time as reported by the API. This can be useful for debugging where a delay occurs.
    /// </summary>
    [JsonIgnore]
    public TimeSpan? ProcessingTime { get; internal set; }

    /// <summary>
    ///     The request id of this API call, as reported in the response headers. This may be useful for troubleshooting or
    ///     when contacting OpenAI support in reference to a specific request.
    /// </summary>
    [JsonIgnore]
    public string? RequestId { get; internal set; }

    /// <summary>
    ///     The version of model/service used to generate this response, as reported in the response headers.
    /// </summary>
    [JsonIgnore]
    public string? Version { get; internal set; }
    
    /// <summary>
    ///		The provider used to execute the request.
    /// </summary>
    [JsonIgnore]
    public IEndpointProvider? Provider { get; set; }
    
    [JsonIgnore]
    internal HttpCallRequest? Request { get; set; }
}