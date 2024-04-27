using System.Runtime.Serialization;

namespace LlmTornado.Threads;

public enum CodeInterpreterOutputType
{
    [EnumMember(Value = "logs")] Logs,
    [EnumMember(Value = "image")] Image
}