using System;
using System.Collections.Generic;
using System.ComponentModel;
using LlmTornado.Audio;
using LlmTornado.Code;
using Argon;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     Represents Cohere Connector.
/// </summary>
[JsonConverter(typeof(ChatVendorCohereExtensionConnectorJsonConverter))]
public class ChatVendorCohereExtensionConnector
{
    /// <summary>
    ///     Known Cohere Connectors, available to all customers.
    /// </summary>
    public enum ChatVendorCohereExtensionConnectorWellKnownIds
    {
        /// <summary>
        ///     Searches the internet/specified site.
        /// </summary>
        [Description("web-search")]
        WebSearch
    }
    
    /// <summary>
    ///     The identifier of the connector for built-in connectors.
    /// </summary>
    [JsonIgnore]
    public ChatVendorCohereExtensionConnectorWellKnownIds? WellKnownId { get; set; }
    
    /// <summary>
    ///     The identifier of the connector for custom connectors.
    /// </summary>
    [JsonIgnore]
    public string? Id { get; set; }
    
    /// <summary>
    ///     When specified, this user access token will be passed to the connector in the Authorization header instead of the Cohere generated one.
    /// </summary>
    [JsonProperty("user_access_token")]
    public string? UserAccessToken { get; set; }
    
    /// <summary>
    ///     When true, the request will continue if this connector returned an error.
    /// </summary>
    [JsonProperty("continue_on_failure")]
    public bool ContinueOnFailure { get; set; }
    
    /// <summary>
    ///     Configuration of the connector.
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object?>? Options { get; set; }

    /// <summary>
    ///     Creates a well-known connector.
    /// </summary>
    /// <param name="wellKnownId"></param>
    public ChatVendorCohereExtensionConnector(ChatVendorCohereExtensionConnectorWellKnownIds wellKnownId)
    {
        WellKnownId = wellKnownId;
    }

    /// <summary>
    ///     Creates a new custom connector.
    /// </summary>
    /// <param name="id"></param>
    public ChatVendorCohereExtensionConnector(string id)
    {
        Id = id;
    }

    /// <summary>
    ///     Built-in web connector.
    /// </summary>
    public static readonly ChatVendorCohereExtensionConnector WebConnector = new ChatVendorCohereExtensionConnector(ChatVendorCohereExtensionConnectorWellKnownIds.WebSearch);

    /// <summary>
    ///     Constructs a new web connector which searches only specified site.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    public static ChatVendorCohereExtensionConnector WebConnectorCustomSite(string site)
    {
        return new ChatVendorCohereExtensionConnector(ChatVendorCohereExtensionConnectorWellKnownIds.WebSearch)
        {
            Options = new Dictionary<string, object?>
            {
                { "site", site }
            }
        };
    }
    
    internal class ChatVendorCohereExtensionConnectorJsonConverter : JsonConverter<ChatVendorCohereExtensionConnector>
    {
        public override void WriteJson(JsonWriter writer, ChatVendorCohereExtensionConnector? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                return;
            }
            
            writer.WriteStartObject();
            
            if (value.WellKnownId is not null)
            {
                writer.WritePropertyName("id");
                writer.WriteValue(value.WellKnownId.GetDescription());
            }
            else if (value.Id is not null)
            {
                writer.WritePropertyName("id");
                writer.WriteValue(value.Id);
            }
            else
            {
                throw new Exception("Neither well-known id or custom id provided for a Cohere connector. One option is required.");
            }

            if (value.ContinueOnFailure)
            {
                writer.WritePropertyName("continue_on_failure");
                writer.WriteValue(true);
            }

            if (value.UserAccessToken is not null)
            {
                writer.WritePropertyName("user_access_token");
                writer.WriteValue(value.UserAccessToken);
            }

            if (value.Options is not null)
            {
                writer.WritePropertyName("options");
                serializer.Serialize(writer, value.Options);
            }
  
            writer.WriteEndObject();
        }

        public override ChatVendorCohereExtensionConnector ReadJson(JsonReader reader, Type objectType, ChatVendorCohereExtensionConnector existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ChatVendorCohereExtensionConnector(reader.ReadAsString());
        }
    }
}