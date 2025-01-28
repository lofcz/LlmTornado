using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace LlmTornado.Assistants;

/// <summary>
///     Specifies the format that the model must output.
///     Compatible with GPT-4o, GPT-4 Turbo, and all GPT-3.5 Turbo models since gpt-3.5-turbo-1106.
/// </summary>
public abstract class ResponseFormat
{
}

/// <summary>
/// 
/// </summary>
public class ResponseFormatAuto : ResponseFormat
{
    /// <summary>
    /// 
    /// </summary>
    public static ResponseFormatAuto Instance { get; } = new ResponseFormatAuto();
}

/// <summary>
/// 
/// </summary>
public class ResponseFormatJsonObject : ResponseFormat
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "json_object";
}

/// <summary>
/// 
/// </summary>
public class ResponseFormatText : ResponseFormat
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "text";
}

/// <summary>
/// 
/// </summary>
public class ResponseFormatJsonSchema : ResponseFormat
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "json_schema";
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("json_schema")]
    public required ResponseFormatJsonSchemaConfig JsonSchema { get; set; } // Adjust type based on schema structure
}

/// <summary>
/// 
/// </summary>
public class ResponseFormatJsonSchemaConfig
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("schema")]
    public object? Schema { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }
}

/// <summary>
/// 
/// </summary>
public class ResponseFormatConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(ResponseFormat).IsAssignableFrom(objectType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    /// <exception cref="JsonSerializationException"></exception>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
            {
                var value = reader.Value?.ToString();
                return value switch
                {
                    "auto" => ResponseFormatAuto.Instance,
                    _ => null
                };
            }
            case JsonToken.StartObject:
            {
                JObject obj = JObject.Load(reader);
                var type = obj["type"]?.ToString();
                return type switch
                {
                    "json_object" => obj.ToObject<ResponseFormatJsonObject>(serializer),
                    "json_schema" => obj.ToObject<ResponseFormatJsonSchema>(serializer),
                    "text" => obj.ToObject<ResponseFormatText>(serializer),
                    _ => null
                };
            }
            default:
                throw new JsonSerializationException("Unexpected token type.");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    /// <exception cref="JsonSerializationException"></exception>
    /// 
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        switch (value)
        {
            case ResponseFormatAuto _:
                writer.WriteValue("auto");
                break;
            case ResponseFormatJsonObject jsonObject:
                JObject.FromObject(jsonObject, serializer).WriteTo(writer);
                break;
            case ResponseFormatText text:
                JObject.FromObject(text, serializer).WriteTo(writer);
                break;
            case ResponseFormatJsonSchema jsonSchema:
                JObject.FromObject(jsonSchema, serializer).WriteTo(writer);
                break;
        }
    }
}