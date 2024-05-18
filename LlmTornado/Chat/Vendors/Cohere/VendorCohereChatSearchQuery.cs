using System.Collections.Generic;
using Argon;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     The generated search query. Contains the text of the query and a unique identifier for the query.
/// </summary>
public class VendorCohereChatSearchQuery
{
    /// <summary>
    ///     The text of the search query.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; }
    
    /// <summary>
    ///     Unique identifier for the generated search query. Useful for submitting feedback.
    /// </summary>
    [JsonProperty("generation_id")]
    public string GenerationId { get; set; }
}