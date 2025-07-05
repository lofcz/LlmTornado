using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Abstract base class for text configuration settings for responses.
/// </summary>
[JsonConverter(typeof(TextConfigurationConverter))]
public abstract class TextConfiguration
{
    /// <summary>
    /// The type of response format being defined.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Creates a text configuration with default text format.
    /// </summary>
    /// <returns>A text configuration with text format</returns>
    public static TextConfiguration CreateText()
    {
        return new TextConfigurationText();
    }

    /// <summary>
    /// Creates a text configuration with JSON schema format.
    /// </summary>
    /// <param name="schema">The JSON schema object</param>
    /// <param name="name">The name of the response format</param>
    /// <param name="description">Description of the response format</param>
    /// <param name="strict">Whether to enable strict mode</param>
    /// <returns>A text configuration with JSON schema format</returns>
    public static TextConfiguration CreateJsonSchema(object schema, string name, string? description = null, bool? strict = null)
    {
        return new TextConfigurationJsonSchema(schema, name, description, strict);
    }

    /// <summary>
    /// Creates a text configuration with JSON object format.
    /// </summary>
    /// <returns>A text configuration with JSON object format</returns>
    public static TextConfiguration CreateJsonObject()
    {
        return new TextConfigurationJsonObject();
    }
}

/// <summary>
/// Default response format. Used to generate text responses.
/// </summary>
public class TextConfigurationText : TextConfiguration
{
    /// <summary>
    /// The type of response format being defined. Always "text".
    /// </summary>
    [JsonIgnore]
    public override string Type => "text";
}

/// <summary>
/// JSON Schema response format. Used to generate structured JSON responses.
/// </summary>
public class TextConfigurationJsonSchema : TextConfiguration
{
    /// <summary>
    /// The type of response format being defined. Always "json_schema".
    /// </summary>
    [JsonIgnore]
    public override string Type => "json_schema";

    /// <summary>
    /// The name of the response format. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The schema for the response format, described as a JSON Schema object.
    /// </summary>
    [JsonProperty("schema")]
    public object Schema { get; set; } = new object();

    /// <summary>
    /// A description of what the response format is for, used by the model to determine how to respond in the format.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether to enable strict schema adherence when generating the response. If set to true, the model will always follow the exact schema defined in the `schema` field. Only a subset of JSON Schema is supported when `strict` is `true`.
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public TextConfigurationJsonSchema() { }

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    /// <param name="schema">The JSON schema object</param>
    /// <param name="name">The name of the response format</param>
    /// <param name="description">Description of the response format</param>
    /// <param name="strict">Whether to enable strict mode</param>
    public TextConfigurationJsonSchema(object schema, string name, string? description = null, bool? strict = null)
    {
        Schema = schema;
        Name = name;
        Description = description;
        Strict = strict;
    }
}

/// <summary>
/// JSON object response format. An older method of generating JSON responses.
/// </summary>
public class TextConfigurationJsonObject : TextConfiguration
{
    /// <summary>
    /// The type of response format being defined. Always "json_object".
    /// </summary>
    [JsonIgnore]
    public override string Type => "json_object";
}

/// <summary>
/// Custom converter for polymorphic deserialization of text configuration
/// </summary>
internal class TextConfigurationConverter : JsonConverter<TextConfiguration>
{
    public override TextConfiguration? ReadJson(JsonReader reader, Type objectType, TextConfiguration? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JToken token = JToken.ReadFrom(reader);
        
        // Check if we have a nested "format" structure
        JToken? formatToken = token["format"];
        JToken actualToken = formatToken ?? token;
        
        string? type = actualToken["type"]?.ToString();

        return type switch
        {
            "text" => new TextConfigurationText(),
            "json_schema" => new TextConfigurationJsonSchema
            {
                Name = actualToken["name"]?.ToString() ?? string.Empty,
                Schema = actualToken["schema"]?.ToObject<object>() ?? new object(),
                Description = actualToken["description"]?.ToString(),
                Strict = actualToken["strict"]?.ToObject<bool?>()
            },
            "json_object" => new TextConfigurationJsonObject(),
            _ => throw new JsonSerializationException($"Unknown text configuration type: {type}")
        };
    }

    public override void WriteJson(JsonWriter writer, TextConfiguration? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        switch (value)
        {
            case TextConfigurationText text:
                writer.WritePropertyName("type");
                writer.WriteValue("text");
                break;

            case TextConfigurationJsonSchema jsonSchema:
                writer.WritePropertyName("type");
                writer.WriteValue("json_schema");
                writer.WritePropertyName("name");
                writer.WriteValue(jsonSchema.Name);
                writer.WritePropertyName("schema");
                serializer.Serialize(writer, jsonSchema.Schema);
                if (jsonSchema.Description != null)
                {
                    writer.WritePropertyName("description");
                    writer.WriteValue(jsonSchema.Description);
                }
                if (jsonSchema.Strict.HasValue)
                {
                    writer.WritePropertyName("strict");
                    writer.WriteValue(jsonSchema.Strict.Value);
                }
                break;

            case TextConfigurationJsonObject jsonObject:
                writer.WritePropertyName("type");
                writer.WriteValue("json_object");
                break;

            default:
                throw new JsonSerializationException($"Unknown text configuration type: {value.GetType()}");
        }

        writer.WriteEndObject();
    }
} 