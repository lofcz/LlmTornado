using System;
using Newtonsoft.Json;

namespace OpenAiNg.Chat;

/// <summary>
///     Represents available message types
/// </summary>
public class ChatMessageTypes
{
    private ChatMessageTypes(string? value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    ///     Text message
    /// </summary>
    public static ChatMessageTypes Text => new("text");
    
    /// <summary>
    ///     ChatImage chatImage
    /// </summary>
    public static ChatMessageTypes Image => new("image_url");
    
    /// <summary>
    ///     Gets the string value for this message type to pass to the API
    /// </summary>
    /// <returns>The type as a string</returns>
    public override string ToString()
    {
        return Value;
    }
    
    /// <summary>
    ///     Gets the string value for this message type to pass to the API
    /// </summary>
    /// <param name="value">The message type to convert</param>
    public static implicit operator string(ChatMessageTypes value)
    {
        return value.Value;
    }

    internal class ChatMessageTypesJsonConverter : JsonConverter<ChatMessageTypes>
    {
        public override void WriteJson(JsonWriter writer, ChatMessageTypes value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ChatMessageTypes ReadJson(JsonReader reader, Type objectType, ChatMessageTypes existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ChatMessageTypes(reader.ReadAsString());
        }
    }
}