using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat;

/// <summary>
///     Represents requested type of response
/// </summary>
public class ChatRequestResponseFormats
{
    internal class ChatRequestResponseJsonSchema
    {
        [JsonProperty("strict")]
        public bool? Strict { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("schema")]
        public object Schema { get; set; }
    }
    
    /// <summary>
    ///     Type of the response
    /// </summary>
    [JsonProperty("type")]
    public ChatRequestResponseFormatTypes? Type { get; set; }

    [JsonProperty("json_schema", NullValueHandling = NullValueHandling.Ignore)]
    internal ChatRequestResponseJsonSchema? Schema { get; set; }
    
    internal ChatRequestResponseFormats() { }
    
    /// <summary>
    ///     Signals the output should be plaintext.
    /// </summary>
    public static ChatRequestResponseFormats Text = new ChatRequestResponseFormats
    {
        Type = ChatRequestResponseFormatTypes.Text
    };

    /// <summary>
    ///     Signals output should be JSON. The string "JSON" needs to be included in either system or user message in the conversation.<br/>
    ///     <b>This is legacy tech. Consider switching to <see cref="ChatRequestResponseFormats.StructuredJson"/>.</b>
    /// </summary>
    public static readonly ChatRequestResponseFormats Json = new ChatRequestResponseFormats
    {
        Type = ChatRequestResponseFormatTypes.Json
    };
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="schema">JSON serializable class / anonymous object.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public static ChatRequestResponseFormats StructuredJson(string name, object schema, bool strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = name,
                Strict = strict,
                Schema = schema
            }
        };
    }
}

/// <summary>
///     Represents response types 
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestResponseFormatTypes
{
    /// <summary>
    /// Response should be in plaintext format, default.
    /// </summary>
    [EnumMember(Value = "text")]
    Text,
    
    /// <summary>
    /// Response should be in JSON. System prompt must include "JSON" substring.
    /// </summary>
    [EnumMember(Value = "json_object")]
    Json,
    
    /// <summary>
    /// Response should be in structured JSON. The model will always follow the provided schema.
    /// </summary>
    [EnumMember(Value = "json_schema")]
    StructuredJson
}