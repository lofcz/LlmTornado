using LlmTornado.Code;

namespace LlmTornado.Audio;

/// <summary>
/// Result of a lyrics generation request.
/// </summary>
public class LyricsGenerationResult : ApiResultBase
{
    /// <summary>
    /// Generated song title.
    /// </summary>
    public string? SongTitle { get; set; }
    
    /// <summary>
    /// Style tags, comma-separated. For example: "Pop, Upbeat, Female Vocals".
    /// </summary>
    public string? StyleTags { get; set; }
    
    /// <summary>
    /// Generated lyrics with structure tags. Can be directly used in the Lyrics parameter
    /// of MusicGenerationRequest to generate songs.
    /// </summary>
    public string? Lyrics { get; set; }
}
