using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Threads;

/// <summary>
///     The details of the run step.
/// </summary>
public abstract class StepDetails
{
    /// <summary>
    /// Enumerates the types of step details in a process or workflow.
    /// </summary>
    [JsonProperty("type")]
    public StepDetailsType StepDetailsType { get; set; }
}

/// <summary>
/// Details of the message creation step in a process or workflow.
/// </summary>
public class MessageCreationStepDetails : StepDetails
{
    /// <summary>
    ///     Details of the message creation by the run step.
    /// </summary>
    [JsonProperty("message_creation")]
    public RunStepMessageCreation MessageCreation { get; set; } = null!;
}

/// <summary>
/// Details of the tool calls step in a process or workflow.
/// </summary>
public class ToolCallsStepDetails : StepDetails
{
    /// <summary>
    ///     An array of tool calls the run step was involved in.
    ///     These can be associated with one of three types of tools: code_interpreter, file_search, or function.
    /// </summary>
    [JsonProperty("tool_calls")]
    [JsonConverter(typeof(ToolCallListConverter))]
    public IReadOnlyList<ToolCall> ToolCallItems { get; set; } = null!;
}

/// <summary>
/// Specifies types of step details within a process or workflow.
/// </summary>
public enum StepDetailsType
{
    /// <summary>
    /// Represents the step type for creating a message during the workflow or process execution.
    /// Typically associated with the generation of messages or communication elements.
    /// Used to define and specify the message creation behavior in a step.
    /// </summary>
    [EnumMember(Value = "message_creation" )]
    MessageCreation,

    /// <summary>
    /// Represents tool call details within a specific step of a process or workflow.
    /// Used to define and manage operations involving external tools.
    /// Corresponds to the "tool_calls" step type.
    /// </summary>
    [EnumMember(Value = "tool_calls" )]
    ToolCalls
}
internal class StepDetailsConverter : JsonConverter<StepDetails>
{
    public override void WriteJson(JsonWriter writer, StepDetails? value, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.FromObject(value!, serializer);
        jsonObject.WriteTo(writer);
    }

    public override StepDetails? ReadJson(JsonReader reader, Type objectType,
        StepDetails? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        StepDetailsType? stepDetailsType = jsonObject["type"]?.ToObject<StepDetailsType>();

        return stepDetailsType switch
        {
            StepDetailsType.MessageCreation => jsonObject.ToObject<MessageCreationStepDetails>(serializer)!,
            StepDetailsType.ToolCalls => jsonObject.ToObject<ToolCallsStepDetails>(serializer)!,
            _ => null
        };
    }
}
