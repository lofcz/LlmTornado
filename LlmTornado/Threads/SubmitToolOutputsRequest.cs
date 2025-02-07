using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a request to submit outputs from one or more tools for processing.
/// </summary>
public sealed class SubmitToolOutputsRequest
{
    /// <summary>
    ///     Tool output to be submitted.
    /// </summary>
    /// <param name="toolOutput"><see cref="ToolOutput" />.</param>
    public SubmitToolOutputsRequest(ToolOutput toolOutput)
        : this([toolOutput])
    {
    }

    /// <summary>
    ///     A list of tools for which the outputs are being submitted.
    /// </summary>
    /// <param name="toolOutputs">Collection of tools for which the outputs are being submitted.</param>
    public SubmitToolOutputsRequest(IEnumerable<ToolOutput> toolOutputs)
    {
        ToolOutputs = toolOutputs.ToList();
    }

    /// <summary>
    ///     A list of tools for which the outputs are being submitted.
    /// </summary>
    [JsonProperty("tool_outputs")]
    public IReadOnlyList<ToolOutput> ToolOutputs { get; set; }

    /// <summary>
    ///     Indicates whether the tool output should be streamed.
    /// </summary>
    [JsonProperty("stream")]
    internal bool Stream { get; set; }
}