using System.Collections.Generic;
using Argon;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     Chat features supported only by Cohere.
/// </summary>
public class ChatRequestVendorCohereExtensions
{
    /// <summary>
    ///     When specified, the model's reply will be enriched with information found by quering each of the connectors (RAG).
    /// </summary>
    [JsonProperty("connectors")]
    public List<ChatVendorCohereExtensionConnector>? Connectors { get; set; }

    /// <summary>
    ///     Empty Cohere extensions.
    /// </summary>
    public ChatRequestVendorCohereExtensions()
    {
        
    }

    /// <summary>
    ///     RAG Connectors.
    /// </summary>
    /// <param name="connectors"></param>
    public ChatRequestVendorCohereExtensions(List<ChatVendorCohereExtensionConnector> connectors)
    {
        Connectors = connectors;
    }
}