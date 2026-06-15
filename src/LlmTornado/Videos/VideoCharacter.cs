using System;
using LlmTornado.Videos.Models;
using Newtonsoft.Json;

namespace LlmTornado.Videos;

/// <summary>
/// A reusable Sora character asset created from an uploaded video clip.
/// </summary>
public class VideoCharacter
{
    /// <summary>
    /// Unique identifier for the character.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the character.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("created_at")]
    internal object? CreatedAtRaw { get; set; }

    /// <summary>
    /// When the character was created.
    /// </summary>
    [JsonIgnore]
    public DateTime? CreatedAt => CreatedAtRaw switch
    {
        null => null,
        long unixTimestamp => DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime,
        int unixTimestamp => DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime,
        double unixTimestamp => DateTimeOffset.FromUnixTimeSeconds((long)unixTimestamp).UtcDateTime,
        string dateString when DateTime.TryParse(dateString, out DateTime parsed) => parsed,
        _ => null
    };
}

/// <summary>
/// Reference to a Sora character in a video generation request.
/// </summary>
public class VideoCharacterReference
{
    /// <summary>
    /// Creates a character reference by ID.
    /// </summary>
    public VideoCharacterReference(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Character identifier returned from <c>POST /v1/videos/characters</c>.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
}

/// <summary>
/// JSON input reference for Sora video generation (Batch API and JSON requests).
/// Provide exactly one of <see cref="FileId"/> or <see cref="ImageUrl"/>.
/// </summary>
public class VideoInputReference
{
    /// <summary>
    /// OpenAI file ID for a previously uploaded image.
    /// </summary>
    [JsonProperty("file_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? FileId { get; set; }

    /// <summary>
    /// A fully qualified URL or base64-encoded data URL.
    /// </summary>
    [JsonProperty("image_url", NullValueHandling = NullValueHandling.Ignore)]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Reference to a completed Sora video by ID.
/// </summary>
public class VideoReference
{
    /// <summary>
    /// Creates a video reference by ID.
    /// </summary>
    public VideoReference(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Identifier of the completed video.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
}

/// <summary>
/// Request to extend a completed Sora video.
/// </summary>
public class VideoExtensionRequest
{
    /// <summary>
    /// Identifier of the completed source video.
    /// </summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// Prompt describing how the scene should continue.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Length of the extension segment. Supports up to 20 seconds per extension.
    /// </summary>
    public VideoDuration? Duration { get; set; }

    /// <summary>
    /// Custom duration in seconds. Takes precedence over <see cref="Duration"/>.
    /// </summary>
    public int? DurationSeconds { get; set; }
}

/// <summary>
/// Request to edit an existing Sora video.
/// </summary>
public class VideoEditRequest
{
    /// <summary>
    /// Creates an edit request for a completed video by ID.
    /// </summary>
    public VideoEditRequest(string videoId, string prompt)
    {
        VideoId = videoId;
        Prompt = prompt;
    }

    /// <summary>
    /// Creates an empty edit request.
    /// </summary>
    public VideoEditRequest()
    {
    }

    /// <summary>
    /// Identifier of a completed video to edit. Mutually exclusive with <see cref="VideoBytes"/>.
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// Prompt describing the desired edit.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Model to use when editing an uploaded video. Required when <see cref="VideoBytes"/> is set.
    /// </summary>
    public VideoModel? Model { get; set; }

    /// <summary>
    /// Uploaded source video bytes for editing. Mutually exclusive with <see cref="VideoId"/>.
    /// </summary>
    public byte[]? VideoBytes { get; set; }

    /// <summary>
    /// MIME type of the uploaded source video. Defaults to video/mp4.
    /// </summary>
    public string? VideoMimeType { get; set; }
}
