using System;
using LlmTornado.Files;

namespace LlmTornado.Images;

/// <summary>
/// Video context for Gemini video-to-image generation (<c>gemini-3.1-flash-image</c>).
/// Pass a public YouTube URL or a file uploaded via the Files API.
/// </summary>
public class ImageGenerationVideoContext
{
    /// <summary>
    /// URI of the video. Public YouTube URLs and Google Files API URIs are supported.
    /// </summary>
    public string FileUri { get; set; }

    /// <summary>
    /// MIME type of the video (e.g. <c>video/mp4</c>). Required for uploaded files; optional for YouTube URLs.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Optional sampling and trimming settings applied while the model reads the video.
    /// </summary>
    public ImageGenerationVideoMetadata? Metadata { get; set; }

    /// <summary>
    /// Creates video context from a URI.
    /// </summary>
    /// <param name="fileUri">Public YouTube URL or Google Files API URI.</param>
    /// <param name="mimeType">MIME type for uploaded files (e.g. <c>video/mp4</c>).</param>
    /// <param name="metadata">Optional frame-rate and offset settings.</param>
    public ImageGenerationVideoContext(string fileUri, string? mimeType = null, ImageGenerationVideoMetadata? metadata = null)
    {
        FileUri = fileUri;
        MimeType = mimeType;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates video context from a file uploaded via the Files API.
    /// </summary>
    /// <param name="file">Uploaded file with an active URI.</param>
    /// <param name="metadata">Optional frame-rate and offset settings.</param>
    public ImageGenerationVideoContext(TornadoFile file, ImageGenerationVideoMetadata? metadata = null)
    {
        FileUri = file.Uri ?? file.Id;
        MimeType = file.MimeType;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates video context from a public YouTube watch or youtu.be URL.
    /// </summary>
    /// <param name="youtubeUrl">Public YouTube video URL.</param>
    /// <param name="metadata">Optional frame-rate and offset settings.</param>
    public static ImageGenerationVideoContext FromYouTubeUrl(string youtubeUrl, ImageGenerationVideoMetadata? metadata = null)
    {
        return new ImageGenerationVideoContext(youtubeUrl, metadata: metadata);
    }
}

/// <summary>
/// Video metadata for Gemini video-to-image generation.
/// Should only be set when video data is provided via <see cref="ImageGenerationVideoContext"/>.
/// </summary>
public class ImageGenerationVideoMetadata
{
    /// <summary>
    /// Frame rate of the video sent to the model. Range: (0.0, 24.0]. Default: 1.0.
    /// </summary>
    public double? Fps { get; set; }

    /// <summary>
    /// Start offset into the video.
    /// </summary>
    public TimeSpan? StartOffset { get; set; }

    /// <summary>
    /// End offset into the video.
    /// </summary>
    public TimeSpan? EndOffset { get; set; }
}
