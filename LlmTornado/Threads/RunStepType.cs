using System.Runtime.Serialization;

namespace LlmTornado.Threads;

public enum RunStepType
{
    [EnumMember(Value = "message_creation")]
    MessageCreation,
    [EnumMember(Value = "tool_calls")] ToolCalls
}