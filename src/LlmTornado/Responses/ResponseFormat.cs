using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// An object specifying the format that the model must output. Used to enable JSON mode.
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// Must be one of `text` or `json_object`.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }
}