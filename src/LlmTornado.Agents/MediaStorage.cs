using System;
using System.IO;

namespace LlmTornado.Agents;

/// <summary>
/// Manages external storage of binary media files alongside conversation JSONL files.
/// Media is stored in a <c>media/</c> subfolder next to the conversation file, referenced
/// by relative path in <see cref="PersistentPart.MediaFilePath"/>.
/// </summary>
public static class MediaStorage
{
    /// <summary>
    /// The subfolder name used for media files.
    /// </summary>
    public const string MediaFolderName = "media";

    /// <summary>
    /// Save binary data to a media file alongside the conversation.
    /// </summary>
    /// <param name="conversationFilePath">Path to the conversation JSONL file.</param>
    /// <param name="data">Raw bytes to store.</param>
    /// <param name="extension">File extension including dot (e.g., <c>.png</c>).</param>
    /// <returns>Relative path from the conversation directory to the saved media file.</returns>
    public static string SaveMedia(string conversationFilePath, byte[] data, string extension)
    {
        string dir = GetMediaDirectory(conversationFilePath);
        Directory.CreateDirectory(dir);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(dir, fileName);

        File.WriteAllBytes(fullPath, data);

        // Return path relative to the conversation file's directory
        return Path.Combine(MediaFolderName, fileName);
    }

    /// <summary>
    /// Load binary data from a media file reference.
    /// </summary>
    /// <param name="conversationFilePath">Path to the conversation JSONL file.</param>
    /// <param name="relativePath">The relative path stored in <see cref="PersistentPart.MediaFilePath"/>.</param>
    /// <returns>The file bytes, or null if the file does not exist.</returns>
    public static byte[]? LoadMedia(string conversationFilePath, string relativePath)
    {
        string? convDir = Path.GetDirectoryName(conversationFilePath);
        if (string.IsNullOrEmpty(convDir))
            return null;

        string fullPath = Path.Combine(convDir, relativePath);

        return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
    }

    /// <summary>
    /// Get the absolute path to the media directory for a conversation.
    /// </summary>
    public static string GetMediaDirectory(string conversationFilePath)
    {
        string? convDir = Path.GetDirectoryName(conversationFilePath);
        return Path.Combine(convDir ?? ".", MediaFolderName);
    }

    /// <summary>
    /// Try to infer a file extension from a MIME type.
    /// </summary>
    public static string GetExtensionForMime(string? mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            "image/tiff" => ".tiff",
            "image/x-icon" => ".ico",
            "application/pdf" => ".pdf",
            "audio/wav" => ".wav",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            "audio/mp4" or "audio/m4a" => ".m4a",
            "audio/aac" => ".aac",
            "audio/webm" => ".webm",
            _ => ".bin"
        };
    }

    /// <summary>
    /// Try to infer a MIME type from an audio format enum.
    /// </summary>
    public static string GetMimeForAudioFormat(Code.ChatAudioFormats? format)
    {
        return format switch
        {
            Code.ChatAudioFormats.Wav => "audio/wav",
            Code.ChatAudioFormats.Mp3 => "audio/mpeg",
            Code.ChatAudioFormats.L16 => "audio/L16",
            _ => "audio/wav"
        };
    }
}
