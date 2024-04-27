using System.Runtime.Serialization;

namespace LlmTornado.Common;

public enum SortOrder
{
    [EnumMember(Value = "desc")] Descending,
    [EnumMember(Value = "asc")] Ascending
}