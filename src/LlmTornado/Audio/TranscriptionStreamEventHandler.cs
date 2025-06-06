using System;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Audio;

/// <summary>
/// Handler of audio streams.
/// </summary>
public class TranscriptionStreamEventHandler
{
    /// <summary>
    ///     Handle after a complete transription.
    /// </summary>
    public Func<TranscriptionResult, ValueTask>? BlockHandler { get; set; }
    
    /// <summary>
    ///     Handle for a transcription chunk.
    /// </summary>
    public Func<TranscriptionResult, ValueTask>? ChunkHandler { get; set; }
    
    /// <summary>
    ///     If this is set, HTTP level exceptions are caught and returned via this handler.
    /// </summary>
    public Func<HttpFailedRequest, ValueTask>? HttpExceptionHandler { get; set; }
    
    /// <summary>
    ///     Called whenever a successful HTTP request is made. In case of streaming requests this is called before the stream is read.
    /// </summary>
    public Func<HttpCallRequest, ValueTask>? OutboundHttpRequestHandler { get; set; }
}