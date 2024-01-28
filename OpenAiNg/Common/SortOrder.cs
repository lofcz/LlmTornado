using System.Runtime.Serialization;

namespace OpenAiNg.Common;

public enum SortOrder
{
    [EnumMember(Value = "desc")] Descending,
    [EnumMember(Value = "asc")] Ascending
}