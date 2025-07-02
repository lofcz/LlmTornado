using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Configuration options for a text response from the model. Can be plain text or structured JSON data.
/// </summary>
public class TextConfiguration
{
    /// <summary>
    /// Configuration for the format of the text response
    /// </summary>
    [JsonProperty("format")]
    public object? Format { get; set; }

    public TextConfiguration() { }

    public TextConfiguration(object? format = null)
    {
        Format = format;
    }

    /// <summary>
    /// Create a default text configuration
    /// </summary>
    public static TextConfiguration Default()
    {
        return new TextConfiguration(new { type = "text" });
    }

    /// <summary>
    /// Create a JSON schema configuration
    /// </summary>
    public static TextConfiguration JsonSchema(object schema, string? name = null, string? description = null, bool? strict = null)
    {
        return new TextConfiguration(new { type = "json_schema", schema, name, description, strict });
    }

    /// <summary>
    /// Create a JSON object configuration (legacy)
    /// </summary>
    public static TextConfiguration JsonObject()
    {
        return new TextConfiguration(new { type = "json_object" });
    }
} 