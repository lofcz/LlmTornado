using System;
using Argon;

namespace LlmTornado.Images;

/// <summary>
///     Represents available qualities for image generation endpoints, only supported by dalle3
/// </summary>
public class ImageQuality
{
    private ImageQuality(string? value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    ///     Standard image
    /// </summary>
    public static ImageQuality Standard => new("standard");

    /// <summary>
    ///     Standard image
    /// </summary>
    public static ImageQuality Hd => new("hd");

    /// <summary>
    ///     Gets the string value for this quality to pass to the API
    /// </summary>
    /// <returns>The quality as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this quality to pass to the API
    /// </summary>
    /// <param name="value">The ImageQuality to convert</param>
    public static implicit operator string(ImageQuality value)
    {
        return value.Value;
    }

    internal class ImageQualityJsonConverter : JsonConverter<ImageQuality>
    {
        public override void WriteJson(JsonWriter writer, ImageQuality value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ImageQuality ReadJson(JsonReader reader, Type objectType, ImageQuality existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ImageQuality(reader.ReadAsString());
        }
    }
}