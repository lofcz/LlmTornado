using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
/// Shared interface for message part citations.
/// </summary>
[JsonConverter(typeof(ChatMessagePartCitationJsonConverter))]
public interface IChatMessagePartCitation
{
    /// <summary>
    /// Type of the citation.
    /// </summary>
    [JsonProperty("type")]
    string Type { get; }
        
    /// <summary>
    /// Text of the citation
    /// </summary>
    [JsonIgnore]
    public string Text { get; }

    internal void Serialize(LLmProviders provider, JsonWriter writer);
}