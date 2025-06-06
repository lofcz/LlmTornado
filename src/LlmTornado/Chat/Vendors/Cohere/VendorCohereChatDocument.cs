using System;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     Represents a document used by a model in its response.
/// </summary>
public class VendorCohereChatDocument
{
    /// <summary>
    ///     Format: {connector-name}_{index}
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
    
    /// <summary>
    ///     Relevant passage, as judged by the model.
    /// </summary>
    [JsonProperty("snippet")]
    public string Snippet { get; set; }
    
    /// <summary>
    ///     Date the information was fetched.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    ///     Title of the document.
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; }
    
    /// <summary>
    ///     URL of the document.
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; set; }
}