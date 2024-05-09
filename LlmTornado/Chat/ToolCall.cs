using Newtonsoft.Json;

namespace LlmTornado.ChatFunctions;

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class ToolCall
{
    [JsonIgnore] 
    private string? JsonEncoded { get; set; }

    /// <summary>
    ///     Index of the tool call.
    /// </summary>
    [JsonProperty("index")]
    public int? Index { get; set; }
    
    /// <summary>
    ///     The ID of the tool.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; } = default!;

    /// <summary>
    ///     The type of the tool. Currently, this should be always "function".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = default!;

    /// <summary>
    ///     The type of the tool. Currently, this should be always "function".
    /// </summary>
    [JsonProperty("function")]
    public FunctionCall FunctionCall { get; set; } = default!;

    /// <summary>
    ///     Gets the json encoded function call, this is cached to avoid serializing the function over and over.
    /// </summary>
    /// <returns></returns>
    public string GetJson()
    {
        return JsonEncoded ??= JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }
}