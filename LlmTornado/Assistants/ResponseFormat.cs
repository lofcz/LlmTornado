using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
///     Specifies the format that the model must output.
///     Compatible with GPT-4o, GPT-4 Turbo, and all GPT-3.5 Turbo models since gpt-3.5-turbo-1106.
/// </summary>
internal class ResponseFormat
{
    /// <summary>
    ///     The type of response format. Can be "auto", "json_schema", or "json_object"
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; }

    /// <summary>
    ///     The JSON schema to validate the response against when type is "json_schema"
    /// </summary>
    [JsonProperty("json_schema")]
    public object? JsonSchema { get; set; }
}