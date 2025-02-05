using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     The details of the run step.
/// </summary>
public sealed class StepDetails
{
    /// <summary>
    ///     Details of the message creation by the run step.
    /// </summary>
    [JsonProperty("message_creation")]
    public RunStepMessageCreation MessageCreation { get; set; } = null!;

    /// <summary>
    ///     An array of tool calls the run step was involved in.
    ///     These can be associated with one of three types of tools: 'code_interpreter', 'retrieval', or 'function'.
    /// </summary>
    [JsonProperty("tool_calls")]
    public ToolCalls ToolCalls { get; set; } = null!;
}

/// <summary>
/// Represents a collection of tool call details used in a process or workflow,
/// including the type of the tool calls and their associated data.
/// </summary>
public class ToolCalls
{
    /// <summary>
    ///     Always tool_calls.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = null!;
    
    
    /// <summary>
    ///     An array of tool calls the run step was involved in.
    ///     These can be associated with one of three types of tools: code_interpreter, file_search, or function.
    /// </summary>
    [JsonProperty("tool_calls")]
    public IReadOnlyList<ToolCall> ToolCallItems { get; set; } = null!;
}