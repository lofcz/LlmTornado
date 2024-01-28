using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Threads;

public sealed class FunctionCall
{
    /// <summary>
    ///     The name of the function.
    /// </summary>
    [JsonInclude]
    [JsonProperty("name")]
    public string Name { get; private set; }

    /// <summary>
    ///     The arguments that the model expects you to pass to the function.
    /// </summary>
    [JsonInclude]
    [JsonProperty("arguments")]
    public string Arguments { get; private set; }

    /// <summary>
    ///     The output of the function. This will be null if the outputs have not been submitted yet.
    /// </summary>
    [JsonInclude]
    [JsonProperty("output")]
    public string Output { get; private set; }
}