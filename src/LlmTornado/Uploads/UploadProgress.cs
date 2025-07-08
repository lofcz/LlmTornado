using System;

namespace LlmTornado.Uploads;

/// <summary>
///     Progress information for upload operations.
/// </summary>
public class UploadProgress
{
    /// <summary>
    ///     Progress as a value between 0.0 (start) and 1.0 (completed).
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    ///     Total size of the file being uploaded in bytes.
    /// </summary>
    public long TotalFileSize { get; set; }

    /// <summary>
    ///     Time elapsed since the upload started.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    ///     Total number of chunks that will be uploaded.
    /// </summary>
    public int TotalChunkCount { get; set; }

    /// <summary>
    ///     Number of chunks that have been successfully uploaded.
    /// </summary>
    public int CompletedChunks { get; set; }

    /// <summary>
    ///     Number of chunks remaining to be uploaded.
    /// </summary>
    public int RemainingChunks => TotalChunkCount - CompletedChunks;

    /// <summary>
    ///     Progress formatted as a percentage string
    /// </summary>
    public string ProgressPercent => $"{(Progress * 100).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}%";
} 