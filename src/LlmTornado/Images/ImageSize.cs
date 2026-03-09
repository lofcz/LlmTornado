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
/// Aspect ratio options for image generation. Use this when a provider supports aspect ratio instead of fixed sizes.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageAspectRatio
{
    /// <summary>
    /// 1:1 square aspect ratio. Supported by: xAI, Google Imagen.
    /// </summary>
    [EnumMember(Value = "1:1")]
    Square,
    
    /// <summary>
    /// 2:3 portrait aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "2:3")]
    Portrait2x3,
    
    /// <summary>
    /// 3:2 landscape aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "3:2")]
    Landscape3x2,
    
    /// <summary>
    /// 3:4 portrait aspect ratio. Supported by: xAI, Google Imagen.
    /// </summary>
    [EnumMember(Value = "3:4")]
    Portrait3x4,
    
    /// <summary>
    /// 4:3 landscape aspect ratio. Supported by: xAI, Google Imagen.
    /// </summary>
    [EnumMember(Value = "4:3")]
    Landscape4x3,
    
    /// <summary>
    /// 9:16 portrait aspect ratio (mobile portrait). Supported by: xAI, Google Imagen.
    /// </summary>
    [EnumMember(Value = "9:16")]
    Portrait9x16,
    
    /// <summary>
    /// 16:9 landscape aspect ratio (widescreen). Supported by: xAI, Google Imagen.
    /// </summary>
    [EnumMember(Value = "16:9")]
    Landscape16x9,
    
    /// <summary>
    /// 9:19.5 portrait aspect ratio (iPhone notch display). Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "9:19.5")]
    Portrait9x19_5,
    
    /// <summary>
    /// 19.5:9 landscape aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "19.5:9")]
    Landscape19_5x9,
    
    /// <summary>
    /// 9:20 portrait aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "9:20")]
    Portrait9x20,
    
    /// <summary>
    /// 20:9 ultrawide landscape aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "20:9")]
    Landscape20x9,
    
    /// <summary>
    /// 1:2 portrait aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "1:2")]
    Portrait1x2,
    
    /// <summary>
    /// 2:1 landscape aspect ratio. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "2:1")]
    Landscape2x1,
    
    /// <summary>
    /// 21:9 ultrawide landscape aspect ratio. Supported by: MiniMax.
    /// </summary>
    [EnumMember(Value = "21:9")]
    Landscape21x9,
    
    /// <summary>
    /// Automatic aspect ratio selection. Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto
}

/// <summary>
/// Resolution options for image generation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageResolution
{
    /// <summary>
    /// 1K resolution (default). Supported by: xAI.
    /// </summary>
    [EnumMember(Value = "1k")]
    Resolution1k,
    
    /// <summary>
    /// 2K resolution. Supported by: xAI (coming soon).
    /// </summary>
    [EnumMember(Value = "2k")]
    Resolution2k
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