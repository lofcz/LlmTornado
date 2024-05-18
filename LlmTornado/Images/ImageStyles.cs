using System;
using Argon;

namespace LlmTornado.Images;

/// <summary>
///     Represents available styles for image generation endpoints, only supported by dalle3
/// </summary>
public class ImageStyles
{
    private ImageStyles(string? value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    ///     Standard image
    /// </summary>
    public static ImageStyles Natural => new("natural");

    /// <summary>
    ///     Standard image
    /// </summary>
    public static ImageStyles Vivid => new("vivid");

    /// <summary>
    ///     Gets the string value for this style to pass to the API
    /// </summary>
    /// <returns>The style as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this styles to pass to the API
    /// </summary>
    /// <param name="value">The ImageStyles to convert</param>
    public static implicit operator string(ImageStyles value)
    {
        return value.Value;
    }

    internal class ImageStyleJsonConverter : JsonConverter<ImageStyles>
    {
        public override void WriteJson(JsonWriter writer, ImageStyles value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ImageStyles ReadJson(JsonReader reader, Type objectType, ImageStyles existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ImageStyles(reader.ReadAsString());
        }
    }
}