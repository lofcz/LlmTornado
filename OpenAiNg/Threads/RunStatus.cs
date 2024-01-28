using System.Runtime.Serialization;

namespace OpenAiNg.Threads;

public enum RunStatus
{
    [EnumMember(Value = "queued")] Queued,
    [EnumMember(Value = "in_progress")] InProgress,

    [EnumMember(Value = "requires_action")]
    RequiresAction,
    [EnumMember(Value = "cancelling")] Cancelling,
    [EnumMember(Value = "cancelled")] Cancelled,
    [EnumMember(Value = "failed")] Failed,
    [EnumMember(Value = "completed")] Completed,
    [EnumMember(Value = "expired")] Expired
}