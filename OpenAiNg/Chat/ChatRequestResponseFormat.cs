using System;
using Newtonsoft.Json;

namespace OpenAiNg.ChatFunctions;

/// <summary>
///     Represents requested type of response
/// </summary>
public class ChatRequestResponseFormats
{
    /// <summary>
    ///     Type of the response
    /// </summary>
    [JsonProperty("type")]
    [JsonConverter(typeof(ChatRequestResponseFormatTypes.ChatRequestResponseFormatTypesJsonConverter))]
    public ChatRequestResponseFormatTypes? Type { get; set; }

    internal ChatRequestResponseFormats() { }
    
    /// <summary>
    ///     Signals the output should be plaintext.
    /// </summary>
    public static ChatRequestResponseFormats Text = new ChatRequestResponseFormats
    {
        Type = ChatRequestResponseFormatTypes.Text
    };

    /// <summary>
    ///     Signals output should be JSON. The string "JSON" needs to be included in either system or user message in the conversation.
    /// </summary>
    public static ChatRequestResponseFormats Json = new ChatRequestResponseFormats
    {
        Type = ChatRequestResponseFormatTypes.Json
    };
}

/// <summary>
///     Represents response types 
/// </summary>
public class ChatRequestResponseFormatTypes
{
    private ChatRequestResponseFormatTypes(string value)
    {
        Value = value;
    }

    private string Value { get; }

    /// <summary>
    ///     Response should be in plaintext format, default.
    /// </summary>
    public static ChatRequestResponseFormatTypes Text => new("text");

    /// <summary>
    ///     Response should be in JSON. System prompt must include "JSON" substring.
    /// </summary>
    public static ChatRequestResponseFormatTypes Json => new("json_object");

    /// <summary>
    ///     Gets the string value for this response format to pass to the API
    /// </summary>
    /// <returns>The response format as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this response format to pass to the API
    /// </summary>
    /// <param name="value">The ChatRequestResponseFormatTypes to convert</param>
    public static implicit operator string(ChatRequestResponseFormatTypes value)
    {
        return value.Value;
    }

    internal class ChatRequestResponseFormatTypesJsonConverter : JsonConverter<ChatRequestResponseFormatTypes>
    {
        public override void WriteJson(JsonWriter writer, ChatRequestResponseFormatTypes? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override ChatRequestResponseFormatTypes ReadJson(JsonReader reader, Type objectType, ChatRequestResponseFormatTypes? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ChatRequestResponseFormatTypes(reader.ReadAsString());
        }
    }
}