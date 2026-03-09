using LlmTornado.Audio.Models;
using Newtonsoft.Json;

namespace LlmTornado.Audio.Vendors.MiniMax;

/// <summary>
/// MiniMax-specific music generation request DTO.
/// </summary>
internal class VendorMiniMaxMusicRequest
{
    [JsonProperty("model")]
    public string? Model { get; set; }
    
    [JsonProperty("prompt")]
    public string? Prompt { get; set; }
    
    [JsonProperty("lyrics")]
    public string? Lyrics { get; set; }
    
    [JsonProperty("stream")]
    public bool Stream { get; set; }
    
    [JsonProperty("output_format")]
    public string? OutputFormat { get; set; }
    
    [JsonProperty("audio_setting")]
    public VendorMiniMaxAudioSetting? AudioSetting { get; set; }

    public VendorMiniMaxMusicRequest(MusicGenerationRequest request)
    {
        Model = (request.Model ?? AudioModel.MiniMax.Music.Music25).GetApiName;
        Prompt = request.Prompt;
        Lyrics = request.Lyrics;
        Stream = false;
        
        if (request.OutputFormat.HasValue)
        {
            OutputFormat = request.OutputFormat.Value switch
            {
                MusicOutputFormat.Url => "url",
                MusicOutputFormat.Hex => "hex",
                _ => null
            };
        }
        
        if (request.AudioSetting is not null)
        {
            AudioSetting = new VendorMiniMaxAudioSetting
            {
                SampleRate = request.AudioSetting.SampleRate,
                Bitrate = request.AudioSetting.Bitrate
            };
            
            if (request.AudioSetting.Format.HasValue)
            {
                AudioSetting.Format = request.AudioSetting.Format.Value switch
                {
                    MusicAudioFormat.Mp3 => "mp3",
                    MusicAudioFormat.Wav => "wav",
                    MusicAudioFormat.Pcm => "pcm",
                    _ => null
                };
            }
        }
    }
}

/// <summary>
/// MiniMax audio setting DTO.
/// </summary>
internal class VendorMiniMaxAudioSetting
{
    [JsonProperty("sample_rate")]
    public int? SampleRate { get; set; }
    
    [JsonProperty("bitrate")]
    public int? Bitrate { get; set; }
    
    [JsonProperty("format")]
    public string? Format { get; set; }
}

/// <summary>
/// MiniMax music generation response DTO.
/// </summary>
internal class VendorMiniMaxMusicResponse
{
    [JsonProperty("data")]
    public VendorMiniMaxMusicData? Data { get; set; }
    
    [JsonProperty("trace_id")]
    public string? TraceId { get; set; }
    
    [JsonProperty("extra_info")]
    public VendorMiniMaxMusicExtraInfo? ExtraInfo { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxMusicBaseResp? BaseResp { get; set; }
    
    public MusicGenerationResult ToResult()
    {
        MusicGenerationResult result = new MusicGenerationResult
        {
            Audio = Data?.Audio,
            Status = Data?.Status,
            TraceId = TraceId
        };
        
        if (ExtraInfo is not null)
        {
            result.DurationMs = ExtraInfo.MusicDuration;
            result.SampleRate = ExtraInfo.MusicSampleRate;
            result.Channels = ExtraInfo.MusicChannel;
            result.Bitrate = ExtraInfo.Bitrate;
            result.Size = ExtraInfo.MusicSize;
        }
        
        return result;
    }
}

internal class VendorMiniMaxMusicData
{
    [JsonProperty("audio")]
    public string? Audio { get; set; }
    
    [JsonProperty("status")]
    public int? Status { get; set; }
}

internal class VendorMiniMaxMusicExtraInfo
{
    [JsonProperty("music_duration")]
    public long? MusicDuration { get; set; }
    
    [JsonProperty("music_sample_rate")]
    public int? MusicSampleRate { get; set; }
    
    [JsonProperty("music_channel")]
    public int? MusicChannel { get; set; }
    
    [JsonProperty("bitrate")]
    public int? Bitrate { get; set; }
    
    [JsonProperty("music_size")]
    public long? MusicSize { get; set; }
}

internal class VendorMiniMaxMusicBaseResp
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }
    
    [JsonProperty("status_msg")]
    public string? StatusMsg { get; set; }
}
