using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Audio;

/// <summary>
/// Request for lyrics generation. Supports full song creation and lyrics editing/continuation.
/// </summary>
public class LyricsGenerationRequest
{
    /// <summary>
    /// Generation mode: write a complete song or edit/continue existing lyrics.
    /// </summary>
    public LyricsGenerationMode Mode { get; set; } = LyricsGenerationMode.WriteFullSong;
    
    /// <summary>
    /// Prompt/instruction describing the song theme, style, or editing direction.
    /// If empty in write_full_song mode, a random song will be generated.
    /// Max 2000 characters.
    /// </summary>
    public string? Prompt { get; set; }
    
    /// <summary>
    /// Existing lyrics content. Only effective in Edit mode.
    /// Can be used for continuation or modification of existing lyrics.
    /// Max 3500 characters.
    /// </summary>
    public string? Lyrics { get; set; }
    
    /// <summary>
    /// Song title. If provided, the output will keep this title unchanged.
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Creates a lyrics generation request for writing a full song.
    /// </summary>
    /// <param name="prompt">Theme/style description for the song.</param>
    public LyricsGenerationRequest(string? prompt = null)
    {
        Prompt = prompt;
    }
    
    /// <summary>
    /// Creates a lyrics generation request for editing existing lyrics.
    /// </summary>
    /// <param name="lyrics">Existing lyrics to edit/continue.</param>
    /// <param name="prompt">Editing direction/instructions.</param>
    public LyricsGenerationRequest(string lyrics, string prompt)
    {
        Mode = LyricsGenerationMode.Edit;
        Lyrics = lyrics;
        Prompt = prompt;
    }
    
    /// <summary>
    /// Parameterless constructor.
    /// </summary>
    public LyricsGenerationRequest()
    {
    }
}

/// <summary>
/// Mode for lyrics generation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum LyricsGenerationMode
{
    /// <summary>
    /// Write a complete song from scratch.
    /// </summary>
    [EnumMember(Value = "write_full_song")]
    WriteFullSong,
    
    /// <summary>
    /// Edit or continue existing lyrics.
    /// </summary>
    [EnumMember(Value = "edit")]
    Edit
}
