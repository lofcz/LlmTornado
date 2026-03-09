using LlmTornado.Code;

namespace LlmTornado.Audio;

/// <summary>
/// Result of a music generation request.
/// </summary>
public class MusicGenerationResult : ApiResultBase
{
    /// <summary>
    /// The generated audio data. Contains hex-encoded audio when OutputFormat is Hex,
    /// or a download URL when OutputFormat is Url.
    /// </summary>
    public string? Audio { get; set; }
    
    /// <summary>
    /// Generation status: 1 = in progress, 2 = completed.
    /// </summary>
    public int? Status { get; set; }
    
    /// <summary>
    /// Whether the generation is complete.
    /// </summary>
    public bool IsCompleted => Status == 2;
    
    /// <summary>
    /// Duration of the generated music in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
    
    /// <summary>
    /// Sample rate of the generated audio.
    /// </summary>
    public int? SampleRate { get; set; }
    
    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public int? Channels { get; set; }
    
    /// <summary>
    /// Bitrate of the generated audio.
    /// </summary>
    public int? Bitrate { get; set; }
    
    /// <summary>
    /// Size of the generated audio file in bytes.
    /// </summary>
    public long? Size { get; set; }
    
    /// <summary>
    /// Trace ID for request tracking.
    /// </summary>
    public string? TraceId { get; set; }
}
