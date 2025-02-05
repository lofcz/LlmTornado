using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a function call within a system, specifying the function name, the arguments
/// passed to it, and the output generated as a result of the call.
/// </summary>
public sealed class FunctionCall
{
    /// <summary>
    ///     The name of the function.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    ///     The arguments that the model expects you to pass to the function.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = null!;

    /// <summary>
    ///     The output of the function. This will be null if the outputs have not been submitted yet.
    /// </summary>
    [JsonProperty("output")]
    public string? Output { get; set; }
}