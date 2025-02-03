using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace LlmTornado.Assistants;

/// <summary>
/// Specifies the format that the model must output.
/// Compatible with GPT-4o, GPT-4 Turbo, and all GPT-3.5 Turbo models since gpt-3.5-turbo-1106.
/// </summary>
public abstract class ResponseFormat
{
}

/// <summary>
/// Represents the default "auto" response format.
/// The "auto" format specifies no special formatting and is the default value.
/// </summary>
public class ResponseFormatAuto : ResponseFormat
{
    /// <summary>
    /// An instance of the "auto" format.
    /// </summary>
    public static ResponseFormatAuto Inst { get; } = new();
}

/// <summary>
/// Represents the JSON object response format.
/// Ensures the output is a valid JSON object.
/// </summary>
public class ResponseFormatJsonObject : ResponseFormat
{
    /// <summary>
    /// The type of the response format. Set to "json_object".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "json_object";
}

/// <summary>
/// Represents the plain text response format.
/// Outputs the response in plain text.
/// </summary>
public class ResponseFormatText : ResponseFormat
{

    /// <summary>
    /// An instance of the "text" format.
    /// </summary>
    public static ResponseFormatText Inst { get; } = new();
    
    /// <summary>
    /// The type of the response format. Set to "text".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "text";
}

/// <summary>
/// Represents the JSON schema response format.
/// Ensures the response is generated according to a specified JSON schema.
/// </summary>
public class ResponseFormatJsonSchema : ResponseFormat
{
    /// <summary>
    /// The type of the response format. Set to "json_schema".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "json_schema";

    /// <summary>
    /// The JSON schema configuration for the response.
    /// </summary>
    [JsonProperty("json_schema")]
    public required ResponseFormatJsonSchemaConfig JsonSchema { get; set; } // Adjust type based on schema structure
}

/// <summary>
/// Defines the configuration for the JSON schema used in the "json_schema" response format.
/// </summary>
public class ResponseFormatJsonSchemaConfig
{
    /// <summary>
    /// The name of the JSON schema.
    /// A required field that must be alphanumeric and can include underscores or dashes.
    /// Maximum length is 64 characters.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; set; }

    /// <summary>
    /// A description of the purpose of the schema.
    /// Provides additional context for the response format.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The schema structure itself, defined as a JSON Schema object.
    /// Includes details such as properties, required fields, and data types.
    /// </summary>
    [JsonProperty("schema")]
    public object? Schema { get; set; }

    /// <summary>
    /// Indicates whether the schema requires strict adherence.
    /// If true, the model will strictly follow the schema definition. Only a subset of JSON Schema is supported.
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }
}

/// <summary>
/// A custom JSON converter for handling different response formats.
/// </summary>
internal class ResponseFormatConverter : JsonConverter<ResponseFormat>
{
    /// <summary>
    /// Reads and converts JSON input into the appropriate ResponseFormat object.
    /// </summary>
    public override ResponseFormat? ReadJson(JsonReader reader, Type objectType, ResponseFormat? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
            {
                string? value = reader.Value?.ToString();
                return value switch
                {
                    "auto" => ResponseFormatAuto.Inst,
                    _ => null
                };
            }
            case JsonToken.StartObject:
            {
                JObject obj = JObject.Load(reader);
                string? type = obj["type"]?.ToString();
                return type switch
                {
                    "json_object" => obj.ToObject<ResponseFormatJsonObject>(serializer),
                    "json_schema" => obj.ToObject<ResponseFormatJsonSchema>(serializer),
                    "text" => obj.ToObject<ResponseFormatText>(serializer),
                    _ => null
                };
            }
            default:
                return null;
        }
    }

    /// <summary>
    /// Writes the ResponseFormat object as JSON output.
    /// </summary>
    public override void WriteJson(JsonWriter writer, ResponseFormat? value, JsonSerializer serializer)
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