using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

internal class PromptVariableStringConverter : JsonConverter<PromptVariableString>
{
    public override void WriteJson(JsonWriter writer, PromptVariableString? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.Value);
        }
    }

    public override PromptVariableString? ReadJson(JsonReader reader, Type objectType, PromptVariableString? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        string? str = JToken.Load(reader).ToObject<string>();
        return str == null ? null : new PromptVariableString(str);
    }
} 