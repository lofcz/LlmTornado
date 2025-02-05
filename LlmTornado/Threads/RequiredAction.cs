using System.Text.Json.Serialization;
using Newtonsoft.Json;

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
    [JsonProperty("type")]
    public string Type { get; set; }

    /// <summary>
    ///     Details on the tool outputs needed for this run to continue.
    /// </summary>
    [JsonProperty("submit_tool_outputs")]
    public SubmitToolOutputs SubmitToolOutputs { get; set; } = null!;
}