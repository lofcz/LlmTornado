using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
/// Citation that refers to block index ranges within a document.
/// </summary>
public sealed class ChatMessagePartCitationContentBlockLocation : IChatMessagePartCitation
{
    /// <inheritdoc />
    [JsonProperty("type")]
    public string Type { get; } = "content_block_location";

    /// <summary>
    /// The quoted text.
    /// </summary>
    public string Text => CitedText;
        
    /// <summary>
    /// Quoted text.
    /// </summary>
    [JsonProperty("cited_text")]
    public string CitedText { get; set; } = string.Empty;

    /// <summary>
    /// Document index (zero-based).
    /// </summary>
    [JsonProperty("document_index")]
    public int DocumentIndex { get; set; }

    /// <summary>
    /// Document title – optional.
    /// </summary>
    [JsonProperty("document_title")]
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Ending block index (inclusive).
    /// </summary>
    [JsonProperty("end_block_index")]
    public int EndBlockIndex { get; set; }

    /// <summary>
    /// Starting block index (inclusive, >= 0).
    /// </summary>
    [JsonProperty("start_block_index")]
    public int StartBlockIndex { get; set; }

    void IChatMessagePartCitation.Serialize(LLmProviders provider, Newtonsoft.Json.JsonWriter writer)
    {
        writer.Serialize(this);
    }
}