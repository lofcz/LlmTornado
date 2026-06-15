using System;
using System.Collections.Generic;
using System.Text;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Google;
using Newtonsoft.Json;

namespace LlmTornado.Audio.Vendors.Google;

/// <summary>
/// Maps Google generateContent responses to harmonized music generation results.
/// </summary>
internal static class VendorGoogleMusicResult
{
    /// <summary>
    /// Lyria 3 outputs 44.1 kHz stereo audio per the Gemini API docs.
    /// </summary>
    private const int LyriaSampleRateHz = 44100;
    
    private const int LyriaChannels = 2;
    
    public static MusicGenerationResult? Deserialize(string jsonData)
    {
        VendorGoogleChatResult? response = JsonConvert.DeserializeObject<VendorGoogleChatResult>(jsonData);
        return response is null ? null : ToResult(response);
    }
    
    public static MusicGenerationResult ToResult(VendorGoogleChatResult response)
    {
        MusicGenerationResult result = new MusicGenerationResult
        {
            Status = 2,
            SampleRate = LyriaSampleRateHz,
            Channels = LyriaChannels
        };
        
        StringBuilder lyrics = new StringBuilder();
        
        foreach (VendorGoogleChatResult.VendorGoogleChatResultMessage candidate in response.Candidates)
        {
            if (candidate.Content.Parts is null)
            {
                continue;
            }
            
            foreach (VendorGoogleChatRequestMessagePart part in candidate.Content.Parts)
            {
                if (part.Text is not null)
                {
                    if (lyrics.Length > 0)
                    {
                        lyrics.Append('\n');
                    }
                    
                    lyrics.Append(part.Text);
                }
                else if (part.InlineData is not null && part.InlineData.MimeType.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
                {
                    result.Audio = part.InlineData.Data;
                    result.MimeType = part.InlineData.MimeType;
                    
                    if (!string.IsNullOrEmpty(part.InlineData.Data))
                    {
                        result.Size = Convert.FromBase64String(part.InlineData.Data).LongLength;
                    }
                }
            }
        }
        
        if (lyrics.Length > 0)
        {
            result.Lyrics = lyrics.ToString();
        }
        
        return result;
    }
}
