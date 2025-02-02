using System.Collections.Generic;

namespace LlmTornado.Files;

/// <summary>
/// List of retrieved files.
/// </summary>
public class TornadoPagingList<T>
{
    /// <summary>
    /// Retrieved items.
    /// </summary>
    public List<T> Items { get; set; }
    
    /// <summary>
    /// Used by Google.
    /// </summary>
    public string? PageToken { get; set; }
}