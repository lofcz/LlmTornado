using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

/// <summary>
/// Represents an action that is required to progress a workflow or process.
/// </summary>
/// <remarks>
/// The <see cref="RequiredAction"/> class contains information about the type of required action and any associated
/// data that is relevant to fulfilling the action, such as outputs from tools used in the process.
/// </remarks>
public sealed class RequiredAction
{
    /// <summary>
    ///     For now, this is always submit_tool_outputs.
    /// </summary>
    [JsonProperty("type")]
    public RequiredActionType Type { get; set; }

    /// <summary>
    ///     Details on the tool outputs needed for this run to continue.
    /// </summary>
    [JsonProperty("submit_tool_outputs")]
    public SubmitToolOutputs SubmitToolOutputs { get; set; } = null!;
}

/// <summary>
/// Defines the types of actions required to progress a workflow or process.
/// </summary>
/// <remarks>
/// Provides categorical identifiers for specific required actions to facilitate process execution and handling.
/// </remarks>
[JsonConverter(typeof(StringEnumConverter))]
public enum RequiredActionType
{
    /// <summary>
    /// Indicates an action requiring submission of output data from tool executions.
    /// </summary>
    /// <remarks>
    /// This is used to progress workflows by providing the results of tools used in the process.
    /// </remarks>
    [EnumMember(Value = "submit_tool_outputs")]
    SubmitToolOutputs
}