using System.Runtime.Serialization;

namespace OpenAiNg.Threads;

public enum RunStepType
{
    [EnumMember(Value = "message_creation")]
    MessageCreation,
    [EnumMember(Value = "tool_calls")] ToolCalls
}