using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Base class for text response format configurations
/// </summary>
[JsonConverter(typeof(TextResponseFormatJsonConverter))]
public abstract class TextResponseFormatConfiguration
{
    /// <summary>
    /// The type of response format being defined
    /// </summary>
    [JsonProperty("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Default text response format
/// </summary>
public class ResponseFormatText : TextResponseFormatConfiguration
{
    /// <summary>
    /// The type of response format being defined. Always "text".
    /// </summary>
    public override string Type => "text";
}

/// <summary>
/// JSON schema response format for structured outputs
/// </summary>
public class TextResponseFormatJsonSchema : TextResponseFormatConfiguration
{
    /// <summary>
    /// The type of response format being defined. Always "json_schema".
    /// </summary>
    public override string Type => "json_schema";

    /// <summary>
    /// A description of what the response format is for, used by the model to determine how to respond in the format.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The name of the response format. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The JSON schema object describing the parameters of the function.
    /// </summary>
    [JsonProperty("schema")]
    public object Schema { get; set; } = new();

    /// <summary>
    /// Whether to enable strict schema adherence when generating the output.
    /// If set to true, the model will always follow the exact schema defined in the schema field.
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }

    public TextResponseFormatJsonSchema() { }

    public TextResponseFormatJsonSchema(object schema, string? name = null, string? description = null, bool? strict = null)
    {
        Schema = schema;
        Name = name;
        Description = description;
        Strict = strict;
    }
}

/// <summary>
/// JSON object response format (legacy)
/// </summary>
public class ResponseFormatJsonObject : TextResponseFormatConfiguration
{
    /// <summary>
    /// The type of response format being defined. Always "json_object".
    /// </summary>
    public override string Type => "json_object";
}

/// <summary>
/// JSON converter for TextResponseFormatConfiguration types
/// </summary>
internal class TextResponseFormatJsonConverter : JsonConverter<TextResponseFormatConfiguration>
{
    public override void WriteJson(JsonWriter writer, TextResponseFormatConfiguration? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // Create a new serializer without this converter to avoid circular reference
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters = serializer.Converters.Where(c => c.GetType() != typeof(TextResponseFormatJsonConverter)).ToList(),
            NullValueHandling = serializer.NullValueHandling,
            DefaultValueHandling = serializer.DefaultValueHandling,
            Formatting = serializer.Formatting
        };
        JsonSerializer tempSerializer = JsonSerializer.Create(settings);
        
        JObject jo = JObject.FromObject(value, tempSerializer);
        jo.WriteTo(writer);
    }

    public override TextResponseFormatConfiguration? ReadJson(JsonReader reader, Type objectType, TextResponseFormatConfiguration? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();

        return type switch
        {
            "text" => jo.ToObject<ResponseFormatText>(serializer),
            "json_schema" => jo.ToObject<TextResponseFormatJsonSchema>(serializer),
            "json_object" => jo.ToObject<ResponseFormatJsonObject>(serializer),
            _ => throw new JsonSerializationException($"Unknown text response format type: {type}")
        };
    }
} 