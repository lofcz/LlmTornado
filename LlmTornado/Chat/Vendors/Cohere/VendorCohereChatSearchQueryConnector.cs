using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     The connector from which information was fetched.
/// </summary>
public class VendorCohereChatSearchQueryConnector
{
    /// <summary>
    ///     The identifier of the connector.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
}