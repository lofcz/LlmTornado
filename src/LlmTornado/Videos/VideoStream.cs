using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Videos;

/// <summary>
/// Wrapper for video content stream with additional functionality
/// </summary>
public class VideoStream : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    /// <summary>
    /// The underlying stream containing the video data
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Creates a new VideoStream wrapper
    /// </summary>
    /// <param name="stream">The underlying stream containing video data</param>
    public VideoStream(Stream stream)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Saves the video stream to a file asynchronously
    /// </summary>
    /// <param name="filePath">The path where the video should be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<string> SaveToFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VideoStream));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        // Create directory if it doesn't exist
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Save the stream to file
        await using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        // Reset stream position if seekable
        if (Stream.CanSeek)
        {
            Stream.Position = 0;
        }
        
        await Stream.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);
        
        return Path.GetFullPath(filePath);
    }

    /// <summary>
    /// Disposes the underlying stream
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stream?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the underlying stream
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (Stream != null)
        {
            await Stream.DisposeAsync();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

