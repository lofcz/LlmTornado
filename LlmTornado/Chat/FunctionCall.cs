using Newtonsoft.Json;

namespace LlmTornado.ChatFunctions;

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class FunctionCall
{
    [JsonIgnore] 
    private string? JsonEncoded { get; set; }

    /// <summary>
    ///     The name of the function.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    ///     Any arguments that need to be passed to the function. This needs to be in JSON format.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = default!;

    /// <summary>
    ///     Gets the json encoded function call, this is cached to avoid serializing the function over and over.
    /// </summary>
    /// <returns></returns>
    public string GetJson()
    {
        return JsonEncoded ??= JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }
}