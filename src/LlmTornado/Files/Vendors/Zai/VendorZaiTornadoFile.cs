using System;
using LlmTornado.Chat;
using LlmTornado.Files;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors.Zai;

/// <summary>
/// ZAI file upload response model.
/// </summary>
internal class VendorZaiTornadoFile
{
    /// <summary>
    /// Unique identifier of the uploaded file.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Object type. Always "file".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "file";

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [JsonProperty("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    /// Name of the uploaded file.
    /// </summary>
    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Purpose of the uploaded file.
    /// </summary>
    [JsonProperty("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of file creation.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }

    /// <summary>
    /// Converts ZAI file response to TornadoFile.
    /// </summary>
    /// <returns>TornadoFile instance</returns>
    public TornadoFile ToFile()
    {
        return new TornadoFile
        {
            Id = Id,
            Object = Object,
            Bytes = Bytes,
            Name = Filename,
            CreatedAt = CreatedAt
        };
    }
}
