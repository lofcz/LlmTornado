using System;

namespace LlmTornado.Uploads;

/// <summary>
///     Options for automatic uploads.
/// </summary>
public class UploadOptions
{
    /// <summary>
    ///     Size of each chunk in bytes. Default is 64 MB, which is the maximum allowed by the API.
    /// </summary>
    public int ChunkSize { get; set; } = 64 * 1024 * 1024;

    /// <summary>
    ///     Maximum number of chunk uploads to run in parallel. Default is 1 (sequential).
    /// </summary>
    public int DegreeOfParallelism { get; set; } = 1;

    /// <summary>
    ///     Progress reporter with detailed upload information.
    /// </summary>
    public IProgress<UploadProgress>? Progress { get; set; }
} 