using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Result of the input token counting endpoint (POST /responses/input_tokens).
/// Returns the number of input tokens the request would consume.
/// </summary>
public class ResponseInputTokensResult
{
    /// <summary>
    /// The object type. Always "response.input_tokens".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "response.input_tokens";

    /// <summary>
    /// The number of input tokens the request would use.
    /// </summary>
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }
}
