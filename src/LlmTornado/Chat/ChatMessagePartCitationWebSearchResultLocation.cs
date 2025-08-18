using System.Collections.Generic;
using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Chat
{
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
        public List<ChatMessagePartCitationWebGroundingSource> Sources { get; set; } = [];
        
        public void Serialize(LLmProviders provider, JsonWriter writer)
        {
            
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
        public string Url { get; set; }
        
        /// <summary>
        /// Title of the source.
        /// </summary>
        public string? Title { get; set; }
    }
} 