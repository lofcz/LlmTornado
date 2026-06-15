using System.Collections.Generic;
using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
/// Citation referring to an external web search result location.
/// </summary>
public sealed class ChatMessagePartCitationWebSearchResultLocation : IChatMessagePartCitation
{
    /// <inheritdoc />
    [JsonProperty("type")]
    public string Type => "web_search_result_location";

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
    /// Encrypted index value (opaque identifier supplied by the model).
    /// </summary>
    [JsonProperty("encrypted_index")]
    public string EncryptedIndex { get; set; } = string.Empty;

    /// <summary>
    /// Optional title of the source.
    /// </summary>
    [JsonProperty("title")]
    public string? Title { get; set; }

    /// <summary>
    /// URL of the source.
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    void IChatMessagePartCitation.Serialize(LLmProviders provider, Newtonsoft.Json.JsonWriter writer)
    {
        writer.Serialize(this);
    }
    
    public sealed class ChatMessagePartCitationWebGrounding : IChatMessagePartCitation
    {
        [JsonProperty("type")]
        public string Type => "web_search_result_location";
        
        /// <summary>
        /// The quoted text.
        /// </summary>
        public string Text => CitedText;
        
        /// <summary>
        /// Quoted text.
        /// </summary>
        [JsonProperty("cited_text")]
        public string CitedText { get; set; } = string.Empty;

        [JsonIgnore]
        public object? NativeObject { get; set; }
        
        /// <summary>
        /// Sources.
        /// </summary>
        [JsonProperty("sources")]
        public List<ChatMessagePartCitationWebGroundingSource> Sources { get; set; } = [];
        
        public void Serialize(LLmProviders provider, JsonWriter writer)
        {
            writer.Serialize(this);
        }
    }

    /// <summary>
    /// Grounding source.
    /// </summary>
    public class ChatMessagePartCitationWebGroundingSource
    {
        /// <summary>
        /// URL of the source.
        /// </summary>
        [JsonProperty("url")]
        public string? Url { get; set; }
        
        /// <summary>
        /// Title of the source.
        /// </summary>
        [JsonProperty("title")]
        public string? Title { get; set; }
        
        /// <summary>
        /// Content of the source chunk.
        /// </summary>
        [JsonProperty("content")]
        public string? Content { get; set; }

        /// <summary>
        /// File Search store resource name (e.g. fileSearchStores/my-store-123).
        /// </summary>
        [JsonProperty("file_search_store")]
        public string? FileSearchStore { get; set; }

        /// <summary>
        /// Page number for PDF and paginated document citations.
        /// </summary>
        [JsonProperty("page_number")]
        public int? PageNumber { get; set; }

        /// <summary>
        /// Persistent media resource ID for cited image chunks (e.g. fileSearchStores/my-store-123/media/BlobId-456).
        /// </summary>
        [JsonProperty("media_id")]
        public string? MediaId { get; set; }

        /// <summary>
        /// Custom metadata from the imported File Search document.
        /// </summary>
        [JsonProperty("custom_metadata")]
        public List<ChatMessagePartCitationFileSearchMetadata>? CustomMetadata { get; set; }
    }

    /// <summary>
    /// Custom metadata on a File Search grounding citation.
    /// </summary>
    public class ChatMessagePartCitationFileSearchMetadata
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("string_value")]
        public string? StringValue { get; set; }

        [JsonProperty("numeric_value")]
        public double? NumericValue { get; set; }
    }
} 
