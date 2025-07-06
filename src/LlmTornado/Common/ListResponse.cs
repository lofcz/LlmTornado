using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Common;

/// <summary>
/// List of items with pagination.
/// </summary>
public sealed class ListResponse<T> : IListResponse<T>
{
    /// <summary>
    /// Always "list"
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "list";

    /// <summary>
    /// Whether there are more items available.
    /// </summary>
    [JsonProperty("has_more")] 
    public bool HasMore { get; private set; }

    /// <summary>
    /// The ID of the first item in the list.
    /// </summary>
    [JsonProperty("first_id")] 
    public string FirstId { get; private set; }

    /// <summary>
    /// The ID of the last item in the list.
    /// </summary>
    [JsonProperty("last_id")] 
    public string LastId { get; private set; }

    /// <summary>
    /// A list of items used to generate this response.
    /// </summary>
    [JsonProperty("data")] 
    public IReadOnlyList<T> Items { get; private set; } = [];
}