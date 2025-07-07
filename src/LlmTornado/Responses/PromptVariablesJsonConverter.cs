using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

internal class PromptVariablesJsonConverter : JsonConverter<Dictionary<string, IPromptVariable>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<string, IPromptVariable>? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        foreach (KeyValuePair<string, IPromptVariable> kvp in value)
        {
            writer.WritePropertyName(kvp.Key);

            switch (kvp.Value)
            {
                case null:
                    writer.WriteNull();
                    break;
                case PromptVariableString strWrapper:
                    writer.WriteValue(strWrapper.Value);
                    break;
                case ResponseInputContent ric:
                    serializer.Serialize(writer, ric);
                    break;
                default:
                    throw new JsonSerializationException($"Unsupported variable value type: {kvp.Value.GetType()}.");
            }
        }

        writer.WriteEndObject();
    }

    public override Dictionary<string, IPromptVariable>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, IPromptVariable>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        JObject jo = JObject.Load(reader);
        Dictionary<string, IPromptVariable> result = new Dictionary<string, IPromptVariable>(StringComparer.OrdinalIgnoreCase);

        foreach (JProperty property in jo.Properties())
        {
            JToken token = property.Value;

            IPromptVariable value = token.Type switch
            {
                JTokenType.String or JTokenType.Boolean or JTokenType.Integer or JTokenType.Float => new PromptVariableString(token.ToObject<string>()!),
                JTokenType.Object => token.ToObject<ResponseInputContent>(serializer) ?? throw new JsonSerializationException($"Unable to deserialize variable '{property.Name}' to a supported type."),
                _ => throw new JsonSerializationException($"Unsupported JSON token type for variable '{property.Name}': {token.Type}.")
            };

            result[property.Name] = value;
        }

        return result;
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;
} 