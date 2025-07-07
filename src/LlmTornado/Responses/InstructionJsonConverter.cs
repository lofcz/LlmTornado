using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

internal class InstructionJsonConverter : JsonConverter<IResponseInstruction>
{
    public override void WriteJson(JsonWriter writer, IResponseInstruction? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        switch (value)
        {
            case StringResponseInstruction stringInstruction:
                writer.WriteValue(stringInstruction.Value);
                break;
            
            case ArrayResponseInstruction arrayInstruction:
                serializer.Serialize(writer, arrayInstruction.Items);
                break;
            
            default:
                throw new JsonSerializationException($"Unexpected instruction type: {value.GetType()}");
        }
    }

    public override IResponseInstruction? ReadJson(JsonReader reader, Type objectType, IResponseInstruction? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            string? stringValue = reader.Value?.ToString();
            return stringValue != null ? new StringResponseInstruction(stringValue) : null;
        }
        
        if (reader.TokenType == JsonToken.StartArray)
        {
            JArray jArray = JArray.Load(reader);
            List<ResponseInputItem> items = [];

            foreach (JToken token in jArray)
            {
                if (token is JObject)
                {
                    ResponseInputItem? inputItem = token.ToObject<ResponseInputItem>(serializer);
                    
                    if (inputItem != null)
                    {
                        items.Add(inputItem);
                    }
                }
            }

            return new ArrayResponseInstruction(items);
        }

        throw new JsonSerializationException($"Unexpected token type for instruction: {reader.TokenType}");
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;
} 