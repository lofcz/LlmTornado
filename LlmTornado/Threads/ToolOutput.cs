using Argon;

namespace LlmTornado.Threads;

/// <summary>
///     Tool function call output
/// </summary>
public sealed class ToolOutput
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="toolCallId">
    ///     The ID of the tool call in the <see cref="RequiredAction" /> within the <see cref="RunResponse" /> the output is
    ///     being submitted for.
    /// </param>
    /// <param name="output">
    ///     The output of the tool call to be submitted to continue the run.
    /// </param>
    [System.Text.Json.Serialization.JsonConstructor]
    public ToolOutput(string toolCallId, string output)
    {
        ToolCallId = toolCallId;
        Output = output;
    }

    /// <summary>
    ///     The ID of the tool call in the <see cref="RequiredAction" /> within the <see cref="RunResponse" /> the output is
    ///     being submitted for.
    /// </summary>
    [JsonProperty("tool_call_id")]
    public string ToolCallId { get; }

    /// <summary>
    ///     The output of the tool call to be submitted to continue the run.
    /// </summary>
    [JsonProperty("output")]
    public string Output { get; }
}