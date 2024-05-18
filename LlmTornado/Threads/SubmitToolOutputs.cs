using System.Collections.Generic;
using System.Text.Json.Serialization;
using Argon;

namespace LlmTornado.Threads;

public sealed class SubmitToolOutputs
{
    /// <summary>
    ///     A list of the relevant tool calls.
    /// </summary>
    [JsonInclude]
    [JsonProperty("tool_calls")]
    public IReadOnlyList<ToolCall> ToolCalls { get; private set; }
}