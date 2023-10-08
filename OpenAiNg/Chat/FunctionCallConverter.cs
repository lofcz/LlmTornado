using System;
using Newtonsoft.Json;
using OpenAiNg.ChatFunctions;

namespace OpenAiNg.Chat
{
    internal class FunctionCallConverter : JsonConverter
    {
        public FunctionCallConverter() : base() { }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(FunctionCall));
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            FunctionCall? functionCall = value as FunctionCall;

            if (functionCall is { Name: "none" or "auto" })
            {
                serializer.Serialize(writer, functionCall.Name);
            }
            else
            {
                serializer.Serialize(writer, functionCall);
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                {
                    string? functionCallType = (string?)serializer.Deserialize(reader, typeof(string));

                    if (functionCallType is "none" or "auto")
                    {
                        return new FunctionCall { Name = functionCallType };
                    }

                    break;
                }
                case JsonToken.StartObject:
                    return serializer.Deserialize<FunctionCall>(reader);
            }

            throw new ArgumentException("Unsupported type for FunctionCall");
        }
    }

}