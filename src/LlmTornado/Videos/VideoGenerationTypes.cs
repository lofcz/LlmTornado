using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Videos;

/// <summary>
/// Represents an image for video generation (initial frame, last frame, etc.)
/// </summary>
public class VideoImage
{
    /// <summary>
    /// Creates a new video image
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    public VideoImage(string content)
    {
        Url = content;
    }

    /// <summary>
    /// Creates a new video image with MIME type
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    /// <param name="mimeType">MIME type of the image (e.g., "image/png", "image/jpeg")</param>
    public VideoImage(string content, string mimeType)
    {
        Url = content;
        MimeType = mimeType;
    }

    /// <summary>
    /// The image URL or base64 encoded content
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; }

    /// <summary>
    /// The MIME type of the image
    /// </summary>
    [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// Implicit conversion from string to VideoImage
    /// </summary>
    public static implicit operator VideoImage(string content) => new VideoImage(content);
}

/// <summary>
/// Represents a reference image for video generation with type specification
/// </summary>
public class VideoReferenceImage
{
    /// <summary>
    /// Creates a new reference image
    /// </summary>
    /// <param name="image">The image to use as reference</param>
    /// <param name="referenceType">The type of reference (asset, style, etc.)</param>
    public VideoReferenceImage(VideoImage image, VideoReferenceType referenceType = VideoReferenceType.Asset)
    {
        Image = image;
        ReferenceType = referenceType;
    }

    /// <summary>
    /// The reference image
    /// </summary>
    [JsonProperty("image")]
    public VideoImage Image { get; set; }

    /// <summary>
    /// The type of reference
    /// </summary>
    [JsonProperty("referenceType")]
    public VideoReferenceType ReferenceType { get; set; }
}

/// <summary>
/// Represents a video for video extension
/// </summary>
public class VideoInput
{
    /// <summary>
    /// Creates a new video input
    /// </summary>
    /// <param name="content">Publicly available URL to the video or base64 encoded content</param>
    public VideoInput(string content)
    {
        Url = content;
    }

    /// <summary>
    /// Creates a new video input with MIME type
    /// </summary>
    /// <param name="content">Publicly available URL to the video or base64 encoded content</param>
    /// <param name="mimeType">MIME type of the video (e.g., "video/mp4")</param>
    public VideoInput(string content, string mimeType)
    {
        Url = content;
        MimeType = mimeType;
    }

    /// <summary>
    /// The video URL or base64 encoded content
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; }

    /// <summary>
    /// The MIME type of the video
    /// </summary>
    [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// Implicit conversion from string to VideoInput
    /// </summary>
    public static implicit operator VideoInput(string content) => new VideoInput(content);
}

/// <summary>
/// Video aspect ratio options
/// </summary>
public enum VideoAspectRatio
{
    /// <summary>
    /// 16:9 aspect ratio (widescreen)
    /// </summary>
    [EnumMember(Value = "16:9")]
    Widescreen,

    /// <summary>
    /// 9:16 aspect ratio (portrait)
    /// </summary>
    [EnumMember(Value = "9:16")]
    Portrait
}

/// <summary>
/// Video resolution options
/// </summary>
public enum VideoResolution
{
    /// <summary>
    /// 720p resolution (1280x720)
    /// </summary>
    [EnumMember(Value = "720p")]
    HD,

    /// <summary>
    /// 1080p resolution (1920x1080) - Only supports 8s duration for Veo 3.1
    /// </summary>
    [EnumMember(Value = "1080p")]
    FullHD
}

/// <summary>
/// Person generation control options
/// </summary>
public enum VideoPersonGeneration
{
    /// <summary>
    /// Allow generation of all people (text-to-video and extension)
    /// </summary>
    [EnumMember(Value = "allow_all")]
    AllowAll,

    /// <summary>
    /// Allow generation of adults only (image-to-video, interpolation, and reference images)
    /// </summary>
    [EnumMember(Value = "allow_adult")]
    AllowAdult,

    /// <summary>
    /// Don't allow generation of people (Veo 2 only)
    /// </summary>
    [EnumMember(Value = "dont_allow")]
    DontAllow
}

/// <summary>
/// Reference image type for Veo 3.1
/// </summary>
public enum VideoReferenceType
{
    /// <summary>
    /// Asset reference - preserves subject appearance
    /// </summary>
    [EnumMember(Value = "asset")]
    Asset,

    /// <summary>
    /// Style reference - guides visual style
    /// </summary>
    [EnumMember(Value = "style")]
    Style
}

/// <summary>
/// Video duration options for different Veo models
/// </summary>
public enum VideoDuration
{
    /// <summary>
    /// 4 seconds - Supported by Veo 3.1, Veo 3.1 Fast, Veo 3, Veo 3 Fast
    /// </summary>
    Seconds4 = 4,

    /// <summary>
    /// 5 seconds - Supported by Veo 2
    /// </summary>
    Seconds5 = 5,

    /// <summary>
    /// 6 seconds - Supported by Veo 3.1, Veo 3.1 Fast, Veo 3, Veo 3 Fast, Veo 2
    /// </summary>
    Seconds6 = 6,

    /// <summary>
    /// 8 seconds - Supported by Veo 3.1, Veo 3.1 Fast, Veo 3, Veo 3 Fast, Veo 2
    /// Required when using extension, interpolation, or referenceImages
    /// </summary>
    Seconds8 = 8,

    /// <summary>
    /// Custom duration - Use DurationSecondsCustom property to specify
    /// </summary>
    Custom = 0
}

