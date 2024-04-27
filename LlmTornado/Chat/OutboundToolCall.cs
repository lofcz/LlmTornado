using System;
using Newtonsoft.Json;

namespace LlmTornado.ChatFunctions;

/// <summary>
///     Represents a function to be called
/// </summary>
public class OutboundToolCallFunction
{
    /// <summary>
    ///     Name of the function
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
}

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class OutboundToolCall
{
    /// <summary>
    ///     The type of the tool. Currently, this should be always "function".
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; } = "function";

    /// <summary>
    ///     The type of the tool. Currently, this should be always "function".
    /// </summary>
    [JsonProperty("function")]
    public OutboundToolCallFunction? Function { get; set; }

    internal class OutboundToolCallConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OutboundToolCall);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            OutboundToolCall? functionCall = value as OutboundToolCall;

            if (functionCall?.Function is { Name: "none" or "auto" })
                serializer.Serialize(writer, functionCall.Function.Name);
            else
                serializer.Serialize(writer, functionCall);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                {
                    string? functionCallType = (string?)serializer.Deserialize(reader, typeof(string));

                    if (functionCallType is "none" or "auto") return new OutboundToolCall { Function = new OutboundToolCallFunction { Name = functionCallType } };

                    break;
                }
                case JsonToken.StartObject:
                {
                    return serializer.Deserialize<OutboundToolCallFunction>(reader);
                }
            }

            throw new ArgumentException("Unsupported type for OutboundToolCallFunction");
        }
    }
}