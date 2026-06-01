using System;
using System.Collections.Generic;
using LlmTornado.Audio.Models;
using LlmTornado.Audio.Models.Google;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Code;
using Newtonsoft.Json;
using ChatImage = LlmTornado.Code.ChatImage;

namespace LlmTornado.Audio.Vendors.Google;

/// <summary>
/// Google Lyria 3 music generation request (generateContent).
/// </summary>
internal class VendorGoogleMusicRequest
{
    [JsonProperty("contents")]
    public List<VendorGoogleChatRequest.VendorGoogleChatRequestMessage> Contents { get; set; } = [];
    
    [JsonProperty("generationConfig")]
    public VendorGoogleMusicGenerationConfig? GenerationConfig { get; set; }

    public VendorGoogleMusicRequest(MusicGenerationRequest request)
    {
        AudioModel model = request.Model ?? AudioModel.Google.Lyria.Lyria3ClipPreview;
        string prompt = BuildPrompt(request);
        
        List<VendorGoogleChatRequestMessagePart> parts =
        [
            new VendorGoogleChatRequestMessagePart
            {
                Text = prompt
            }
        ];
        
        if (request.Images?.Count > 0)
        {
            int imageCount = Math.Min(request.Images.Count, 10);
            
            for (int i = 0; i < imageCount; i++)
            {
                ChatImage image = request.Images[i];
                
                if (image.MimeType is null)
                {
                    throw new ArgumentException("Google Lyria requires MIME type on all reference images. Supported values: image/png, image/jpeg.");
                }
                
                parts.Add(new VendorGoogleChatRequestMessagePart
                {
                    InlineData = new VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartInlineData
                    {
                        MimeType = image.MimeType,
                        Data = image.Url
                    }
                });
            }
        }
        
        Contents =
        [
            new VendorGoogleChatRequest.VendorGoogleChatRequestMessage
            {
                Role = "user",
                Parts = parts
            }
        ];
        
        GenerationConfig = new VendorGoogleMusicGenerationConfig
        {
            ResponseModalities = ["AUDIO", "TEXT"]
        };
        
        if (request.AudioSetting?.Format is MusicAudioFormat.Wav && model.Name == AudioModelGoogleLyria.ModelLyria3ProPreview.Name)
        {
            GenerationConfig.ResponseFormat = new VendorGoogleMusicResponseFormat
            {
                Audio = new VendorGoogleMusicAudioFormat
                {
                    MimeType = "audio/wav"
                }
            };
        }
    }
    
    static string BuildPrompt(MusicGenerationRequest request)
    {
        bool hasPrompt = !string.IsNullOrWhiteSpace(request.Prompt);
        bool hasLyrics = !string.IsNullOrWhiteSpace(request.Lyrics);
        
        if (hasPrompt && hasLyrics)
        {
            return $"{request.Prompt}\n\n{request.Lyrics}";
        }
        
        if (hasPrompt)
        {
            return request.Prompt!;
        }
        
        if (hasLyrics)
        {
            return request.Lyrics;
        }
        
        throw new ArgumentException("Music generation requires a non-empty Prompt and/or Lyrics.");
    }
}

internal class VendorGoogleMusicGenerationConfig
{
    [JsonProperty("responseModalities")]
    public List<string>? ResponseModalities { get; set; }
    
    [JsonProperty("responseFormat")]
    public VendorGoogleMusicResponseFormat? ResponseFormat { get; set; }
}

internal class VendorGoogleMusicResponseFormat
{
    [JsonProperty("audio")]
    public VendorGoogleMusicAudioFormat? Audio { get; set; }
}

internal class VendorGoogleMusicAudioFormat
{
    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }
}
