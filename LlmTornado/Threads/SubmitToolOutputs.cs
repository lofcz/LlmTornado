using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents the output data of tool calls within a workflow or process.
/// </summary>
/// <remarks>
/// Holds a collection of tool call results for further processing or evaluation.
/// </remarks>
public sealed class SubmitToolOutputs
{
    /// <summary>
    ///     A list of the relevant tool calls. (for now, only a function type is supported)
    /// </summary>
    [JsonProperty("tool_calls")]
    [JsonConverter(typeof(ToolCallListConverter))]
    public required IReadOnlyList<ToolCall> ToolCalls { get; set; }
}