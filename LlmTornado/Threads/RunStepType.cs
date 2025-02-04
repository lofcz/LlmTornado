using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

/// <summary>
/// Represents the type of step in a run sequence.
/// This enumeration defines the possible categories of steps that can occur during a run.
/// Each option corresponds to a specific type of action or phase that the system performs.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum RunStepType
{
    /// <summary>
    /// Represents the "message_creation" step of a run process where a new message is generated or composed.
    /// </summary>
    [JsonProperty("message_creation")] MessageCreation,

    /// <summary>
    /// Represents a step during a run where tools or external functionalities
    /// are invoked or executed as part of the process.
    /// </summary>
    [JsonProperty("tool_calls")] ToolCalls
}