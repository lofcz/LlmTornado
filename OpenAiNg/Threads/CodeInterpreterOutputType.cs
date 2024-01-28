using System.Runtime.Serialization;

namespace OpenAiNg.Threads;

public enum CodeInterpreterOutputType
{
    [EnumMember(Value = "logs")] Logs,
    [EnumMember(Value = "image")] Image
}