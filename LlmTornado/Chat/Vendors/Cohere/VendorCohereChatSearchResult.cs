using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     RAG search result.
/// </summary>
public class VendorCohereChatSearchResult
{
    /// <summary>
    ///     The generated search query. Contains the text of the query and a unique identifier for the query.
    /// </summary>
    [JsonProperty("search_query")]
    public VendorCohereChatSearchQuery SearchQuery { get; set; }
    
    /// <summary>
    ///     Identifiers of documents found by this search query.    
    /// </summary>
    [JsonProperty("document_ids")]
    public List<string> DocumentIds { get; set; }
    
    /// <summary>
    ///     The connector from which this result comes from.
    /// </summary>
    [JsonProperty("connector")]
    public VendorCohereChatSearchQueryConnector Connector { get; set; }
    
    /// <summary>
    ///     An error message if the search failed.
    /// </summary>
    [JsonProperty("error_message")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    ///     Whether a chat request should continue or not if the request to this connector fails.
    /// </summary>
    [JsonProperty("continue_on_failure")]
    public bool ContinueOnFailure { get; set; }
}