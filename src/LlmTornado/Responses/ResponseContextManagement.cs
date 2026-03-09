using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Responses;

/// <summary>
/// The type of context management strategy.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseContextManagementTypes
{
    /// <summary>
    /// Compaction strategy. Compresses the conversation context when the token count exceeds the threshold.
    /// </summary>
    [EnumMember(Value = "compaction")]
    Compaction
}

/// <summary>
/// Represents a context management configuration item for the Responses API.
/// Used to enable server-side compaction when the rendered token count crosses a configured threshold.
/// </summary>
public class ResponseContextManagementItem
{
    /// <summary>
    /// The type of context management.
    /// </summary>
    [JsonProperty("type")]
    public ResponseContextManagementTypes Type { get; set; } = ResponseContextManagementTypes.Compaction;

    /// <summary>
    /// The token count threshold at which server-side compaction is triggered.
    /// When the rendered token count crosses this threshold, the server runs compaction automatically.
    /// </summary>
    [JsonProperty("compact_threshold")]
    public int? CompactThreshold { get; set; }

    /// <summary>
    /// Creates a new context management item with default values.
    /// </summary>
    public ResponseContextManagementItem()
    {
    }

    /// <summary>
    /// Creates a compaction context management item with the specified threshold.
    /// </summary>
    /// <param name="compactThreshold">The token count threshold at which compaction is triggered.</param>
    public ResponseContextManagementItem(int compactThreshold)
    {
        CompactThreshold = compactThreshold;
    }

    /// <summary>
    /// Creates a compaction context management item with the specified threshold.
    /// </summary>
    /// <param name="compactThreshold">The token count threshold at which compaction is triggered.</param>
    /// <returns>A new <see cref="ResponseContextManagementItem"/> configured for compaction.</returns>
    public static ResponseContextManagementItem Compaction(int compactThreshold)
    {
        return new ResponseContextManagementItem
        {
            Type = ResponseContextManagementTypes.Compaction,
            CompactThreshold = compactThreshold
        };
    }
}
