using System.Runtime.Serialization;

namespace LlmTornado.Common;

/// <summary>
/// Sorting order.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Descending.
    /// </summary>
    [EnumMember(Value = "desc")] 
    Descending,
    
    /// <summary>
    /// Ascending.
    /// </summary>
    [EnumMember(Value = "asc")] 
    Ascending
}