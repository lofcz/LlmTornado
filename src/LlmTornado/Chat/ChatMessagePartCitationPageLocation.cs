using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
/// Citation that refers to page range within a document.
/// </summary>
public sealed class ChatMessagePartCitationPageLocation : IChatMessagePartCitation
{
    /// <inheritdoc />
    [JsonProperty("type")]
    public string Type { get; } = "page_location";

    /// <summary>
    /// The quoted text.
    /// </summary>
    public string Text => CitedText;
        
    /// <summary>
    /// Quoted text from the document.
    /// </summary>
    [JsonProperty("cited_text")]
    public string CitedText { get; set; } = string.Empty;

    /// <summary>
    /// Index of the document (zero-based).
    /// </summary>
    [JsonProperty("document_index")]
    public int DocumentIndex { get; set; }

    /// <summary>
    /// Optional title of the document.
    /// </summary>
    [JsonProperty("document_title")]
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Ending page number of the citation (inclusive).
    /// </summary>
    [JsonProperty("end_page_number")]
    public int EndPageNumber { get; set; }

    /// <summary>
    /// Starting page number of the citation (inclusive, >= 1).
    /// </summary>
    [JsonProperty("start_page_number")]
    public int StartPageNumber { get; set; }

    void IChatMessagePartCitation.Serialize(LLmProviders provider, Newtonsoft.Json.JsonWriter writer)
    {
        writer.Serialize(this);
    }
}