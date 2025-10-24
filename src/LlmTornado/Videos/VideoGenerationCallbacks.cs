using System;
using System.Threading.Tasks;

namespace LlmTornado.Videos;

/// <summary>
/// Events for video generation progress and completion
/// </summary>
public class VideoGenerationEvents
{
    /// <summary>
    /// Called on each poll with the current result, poll index, and total elapsed time
    /// </summary>
    public Func<VideoGenerationResult?, int, TimeSpan, ValueTask>? OnPoll { get; set; }
    
    /// <summary>
    /// Called when video generation is complete with the final result and video stream.
    /// The VideoStream provides access to the underlying stream and a SaveToFileAsync method.
    /// </summary>
    public Func<VideoGenerationResult, VideoStream, ValueTask>? OnFinished { get; set; }
}

