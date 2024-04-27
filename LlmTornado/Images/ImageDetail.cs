using System;
using Newtonsoft.Json;

namespace LlmTornado.Images;

/// <summary>
///     Represents available response formats for image generation endpoints
/// </summary>
public class ImageDetail
{
    private ImageDetail(string value)
    {
        Value = value;
    }

    private string Value { get; }

    /// <summary>
    ///     If the image is exactly 512x512 or lower, the image is processed as <see cref="Low" />, <see cref="High" />
    ///     otherwise
    /// </summary>
    public static ImageDetail Auto => new("auto");

    /// <summary>
    ///     The image will be automatically split into 512x512 chunks, each chunk is billed separately
    /// </summary>
    public static ImageDetail High => new("high");

    /// <summary>
    ///     The image should be 512x512
    /// </summary>
    public static ImageDetail Low => new("low");

    /// <summary>
    ///     Gets the string value for this response format to pass to the API
    /// </summary>
    /// <returns>The response format as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this image detail to pass to the API
    /// </summary>
    /// <param name="value">The ImageDetail to convert</param>
    public static implicit operator string(ImageDetail value)
    {
        return value.Value;
    }

    internal class ImageDetailJsonConverter : JsonConverter<ImageDetail>
    {
        public override ImageDetail ReadJson(JsonReader reader, Type objectType, ImageDetail existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ImageDetail(reader.ReadAsString() ?? Auto);
        }

        public override void WriteJson(JsonWriter writer, ImageDetail value, JsonSerializer serializer)
        {
            if (value is not null) writer.WriteValue(value.ToString());
        }
    }
}