using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
/// Dictates the approach taken to generating citations as part of the RAG flow by allowing the user to specify whether they want "accurate" results, "fast" results or no results.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatVendorCohereExtensionCitationQuality
{
    /// <summary>
    /// Prefers low latency.
    /// </summary>
    [EnumMember(Value = "fast")] 
    Fast,
    /// <summary>
    /// Prefers high quality citations.
    /// </summary>
    [EnumMember(Value = "accurate")] 
    Accurate,
    /// <summary>
    /// Citations will be disabled
    /// </summary>
    [EnumMember(Value = "off")] 
    Off
}