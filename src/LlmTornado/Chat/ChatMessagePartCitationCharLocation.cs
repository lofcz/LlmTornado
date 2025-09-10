using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
/// Citation type that references a span of characters within a document.
/// </summary>
public sealed class ChatMessagePartCitationCharLocation : IChatMessagePartCitation
{
    /// <inheritdoc />
    [JsonProperty("type")]
    public string Type { get; } = "char_location";

    /// <summary>
    /// The quoted text.
    /// </summary>
    public string Text => CitedText;

    /// <summary>
    /// The quoted text. Required.
    /// </summary>
    [JsonProperty("cited_text")]
    public string CitedText { get; set; } = string.Empty;

    /// <summary>
    /// Index of the document in the message that this citation refers to. Required. Must be >= 0.
    /// </summary>
    [JsonProperty("document_index")]
    public int DocumentIndex { get; set; }

    /// <summary>
    /// Title of the cited document. May be <c>null</c>. Length 1–255 when not null.
    /// </summary>
    [JsonProperty("document_title")]
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Index of the character where the citation ends (exclusive).
    /// </summary>
    [JsonProperty("end_char_index")]
    public int EndCharIndex { get; set; }

    /// <summary>
    /// Index of the character where the citation starts (inclusive). Must be >= 0.
    /// </summary>
    [JsonProperty("start_char_index")]
    public int StartCharIndex { get; set; }

    void IChatMessagePartCitation.Serialize(LLmProviders provider, JsonWriter writer)
    {
        writer.Serialize(this);
    }
}