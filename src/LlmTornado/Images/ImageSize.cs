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
    /// Supported by Dalle2, Dalle3, Imagen, gpt-image-1; 1:1
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
    Size1280x896,
    
    /// <summary>
    /// Supported by gpt-image-1
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,
    
    /// <summary>
    /// Landscape, supported by gpt-image-1
    /// </summary>
    [EnumMember(Value = "1536x1024")]
    Size1536x1024,
    
    /// <summary>
    /// Portrait, supported by gpt-image-1
    /// </summary>
    [EnumMember(Value = "1024x1536")]
    Size1024x1536,
    
    /// <summary>
    /// When used, forces <see cref="ImageGenerationRequest.Width"/> and <see cref="ImageGenerationRequest.Height"/> to be use instead.
    /// </summary>
    Custom
}

/// <summary>
/// Levels of image moderation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageModerationTypes
{
    /// <summary>
    /// Default.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,
    
    /// <summary>
    /// Reduced filtering.
    /// </summary>
    [EnumMember(Value = "low")]
    Low
}

/// <summary>
/// Formats in which images can be generated.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageOutputFormats
{
    /// <summary>
    /// PNG
    /// </summary>
    [EnumMember(Value = "png")]
    Png,
    
    /// <summary>
    /// JPEG
    /// </summary>
    [EnumMember(Value = "jpeg")]
    Jpeg,
    
    /// <summary>
    /// WEBP
    /// </summary>
    [EnumMember(Value = "webp")]
    Webp
}

/// <summary>
/// Types of image backgrounds.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageBackgroundTypes
{
    /// <summary>
    ///  The model will automatically determine the best background for the image.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,
    
    /// <summary>
    /// Background will be transparent, requires png/webp file type target.
    /// </summary>
    [EnumMember(Value = "transparent")]
    Transparent,
    
    /// <summary>
    /// Opaque background.
    /// </summary>
    [EnumMember(Value = "opaque")]
    Opaque
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