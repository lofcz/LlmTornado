using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     Tool function call output
/// </summary>
public sealed class ToolOutput
{
    /// <summary>
    ///     The ID of the tool call in the <see cref="RequiredAction" /> within the <see cref="TornadoRun" /> the output is
    ///     being submitted for.
    /// </summary>
    [JsonProperty("tool_call_id")]
    public string? ToolCallId { get; set; }

    /// <summary>
    ///     The output of the tool call to be submitted to continue the run.
    /// </summary>
    [JsonProperty("output")]
    public string? Output { get; set; }
}