using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Chat
{
    /// <summary>
    /// Citation pointing to a location within aggregated search results returned by the model.
    /// </summary>
    public sealed class ChatMessagePartCitationSearchResultLocation : IChatMessagePartCitation
    {
        /// <inheritdoc />
        [JsonProperty("type")]
        public string Type { get; } = "search_result_location";

        /// <summary>
        /// The quoted text.
        /// </summary>
        public string Text => CitedText;
        
        /// <summary>
        /// Quoted citation text.
        /// </summary>
        [JsonProperty("cited_text")]
        public string CitedText { get; set; } = string.Empty;

        /// <summary>
        /// Index of the block where the citation ends (inclusive).
        /// </summary>
        [JsonProperty("end_block_index")]
        public int EndBlockIndex { get; set; }

        /// <summary>
        /// Index of the search result in the list (>= 0).
        /// </summary>
        [JsonProperty("search_result_index")]
        public int SearchResultIndex { get; set; }

        /// <summary>
        /// Original source identifier (e.g., URL or other reference).
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Index of the block where the citation starts (inclusive).
        /// </summary>
        [JsonProperty("start_block_index")]
        public int StartBlockIndex { get; set; }

        /// <summary>
        /// Optional title of the referenced search result.
        /// </summary>
        [JsonProperty("title")]
        public string? Title { get; set; }

        void IChatMessagePartCitation.Serialize(LLmProviders provider, Newtonsoft.Json.JsonWriter writer)
        {
            writer.Serialize(this);
        }
    }
} 