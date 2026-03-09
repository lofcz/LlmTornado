using Newtonsoft.Json;

namespace LlmTornado.Audio.Vendors.MiniMax;

/// <summary>
/// MiniMax-specific lyrics generation request DTO.
/// </summary>
internal class VendorMiniMaxLyricsRequest
{
    [JsonProperty("mode")]
    public string Mode { get; set; } = "write_full_song";
    
    [JsonProperty("prompt")]
    public string? Prompt { get; set; }
    
    [JsonProperty("lyrics")]
    public string? Lyrics { get; set; }
    
    [JsonProperty("title")]
    public string? Title { get; set; }

    public VendorMiniMaxLyricsRequest(LyricsGenerationRequest request)
    {
        Mode = request.Mode switch
        {
            LyricsGenerationMode.Edit => "edit",
            _ => "write_full_song"
        };
        Prompt = request.Prompt;
        Lyrics = request.Lyrics;
        Title = request.Title;
    }
}

/// <summary>
/// MiniMax lyrics generation response DTO.
/// </summary>
internal class VendorMiniMaxLyricsResponse
{
    [JsonProperty("song_title")]
    public string? SongTitle { get; set; }
    
    [JsonProperty("style_tags")]
    public string? StyleTags { get; set; }
    
    [JsonProperty("lyrics")]
    public string? Lyrics { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxLyricsBaseResp? BaseResp { get; set; }
    
    public LyricsGenerationResult ToResult()
    {
        return new LyricsGenerationResult
        {
            SongTitle = SongTitle,
            StyleTags = StyleTags,
            Lyrics = Lyrics
        };
    }
}

internal class VendorMiniMaxLyricsBaseResp
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }
    
    [JsonProperty("status_msg")]
    public string? StatusMsg { get; set; }
}
