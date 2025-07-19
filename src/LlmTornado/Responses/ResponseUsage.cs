using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Responses
{
    /// <summary>
    /// Usage statistics for the Response, this will correspond to billing. A
    /// Realtime API session will maintain a conversation context and append new
    /// Items to the Conversation, thus output from previous turns (text and
    /// audio tokens) will become the input for later turns.
    /// </summary>
    public class ResponseUsage
    {
        /// <summary>
        /// Details about the input tokens used in the Response.
        /// </summary>
        [JsonProperty("input_token_details")]
        public ResponsesUsageInputTokenDetails InputTokenDetails { get; set; }

        /// <summary>
        /// The number of input tokens used in the Response, including text and audio tokens.
        /// </summary>
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        /// <summary>
        /// Details about the output tokens used in the Response.
        /// </summary>
        [JsonProperty("output_token_details")]
        public ResponsesUsageOutputTokenDetails OutputTokenDetails { get; set; }

        /// <summary>
        /// The number of output tokens sent in the Response, including text and audio tokens.
        /// </summary>
        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }

        /// <summary>
        /// The total number of tokens in the Response including input and output text and audio tokens.
        /// </summary>
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// Details about the input tokens used in the Response.
    /// </summary>
    public class ResponsesUsageInputTokenDetails
    {
        /// <summary>
        /// The number of audio tokens used in the Response.
        /// </summary>
        [JsonProperty("audio_tokens")]
        public int AudioTokens { get; set; }

        /// <summary>
        /// The number of cached tokens used in the Response.
        /// </summary>
        [JsonProperty("cached_tokens")]
        public int CachedTokens { get; set; }

        /// <summary>
        /// The number of text tokens used in the Response.
        /// </summary>
        [JsonProperty("text_tokens")]
        public int TextTokens { get; set; }
    }

    /// <summary>
    /// Details about the output tokens used in the Response.
    /// </summary>
    public class ResponsesUsageOutputTokenDetails
    {
        /// <summary>
        /// The number of reasoning tokens in the Response.
        /// </summary>
        [JsonProperty("reasoning_tokens")]
        public int ReasoningTokens { get; set; }
    }
} 