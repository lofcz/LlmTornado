using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable InconsistentNaming

namespace LlmTornado.Images;

/// <summary>
/// Possible image sizes.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageSizes
{
    /// <summary>
    /// Supported by Dalle2, 1:1
    /// </summary>
    [EnumMember(Value = "256x256")]
    Size256x256,
    
    /// <summary>
    /// Supported by Dalle2, 1:1
    /// </summary>
    [EnumMember(Value = "512x512")]
    Size512x512,
    
    /// <summary>
    /// Supported by Dalle2, Dalle3, Imagen, 1:1
    /// </summary>
    [EnumMember(Value = "1024x1024")]
    Size1024x1024,
    
    /// <summary>
    /// Supported by Dalle3
    /// </summary>
    [EnumMember(Value = "1792x1024")]
    Size1792x1024,
    
    /// <summary>
    /// Supported by Dalle3
    /// </summary>
    [EnumMember(Value = "1024x1792")]
    Size1024x1792,
    
    /// <summary>
    /// Supported by Imagen, 9:16
    /// </summary>
    Size768x1408,
    
    /// <summary>
    /// Supported by Imagen, 16:9
    /// </summary>
    Size1408x768,
    
    /// <summary>
    /// Supported by Imagen, 3:4
    /// </summary>
    Size896x1280,
    
    /// <summary>
    /// Supported by Imagen, 4:3
    /// </summary>
    Size1280x896
}

/// <summary>
///     Represents available sizes for image generation endpoints
/// </summary>
public class ImageSize
{
    private ImageSize(string value)
    {
        Value = value;
    }

    private string Value { get; }

    /// <summary>
    ///     Requests an image that is 256x256
    /// </summary>
    public static ImageSize _256 => new ImageSize("256x256");

    /// <summary>
    ///     Requests an image that is 512x512
    /// </summary>
    public static ImageSize _512 => new ImageSize("512x512");

    /// <summary>
    ///     Requests and image that is 1024x1024
    /// </summary>
    public static ImageSize _1024 => new ImageSize("1024x1024");

    /// <summary>
    ///     Requests and image that is 1792x1024, only for dalle3
    /// </summary>
    public static ImageSize _1792x1024 => new ImageSize("1792x1024");

    /// <summary>
    ///     Requests and image that is 1024x1792
    /// </summary>
    public static ImageSize _1024x1792 => new ImageSize("1024x1792");

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <returns>The size as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <param name="value">The ImageSize to convert</param>
    public static implicit operator string(ImageSize value)
    {
        return value.Value;
    }

    internal class ImageSizeJsonConverter : JsonConverter<ImageSize>
    {
        public override void WriteJson(JsonWriter writer, ImageSize value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ImageSize ReadJson(JsonReader reader, Type objectType, ImageSize existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ImageSize(reader.ReadAsString());
        }
    }
}