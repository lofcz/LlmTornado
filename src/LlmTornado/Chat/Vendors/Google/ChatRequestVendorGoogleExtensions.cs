using System.Collections.Generic;
using LlmTornado.Caching;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Google;

/// <summary>
/// Chat features supported only by Google.
/// </summary>
public class ChatRequestVendorGoogleExtensions
{
    /// <summary>
    /// The name of the content cached to use as context to serve the prediction. Format: cachedContents/{cachedContent}
    /// </summary>
    [JsonProperty("cachedContent")]
    public string? CachedContent { get; set; }
    
    [JsonIgnore]
    internal CachedContentInformation? CachedContentInformation { get; set; }
    
    /// <summary>
    /// Forces given response schema. Normally, use strict functions to automatically set this. Manually setting this is required for cached functions.
    /// </summary>
    [JsonIgnore]
    public Tool? ResponseSchema { get; set; }
    
    /// <summary>
    /// The speech generation config.
    /// </summary>
    [JsonIgnore]
    public ChatRequestVendorGoogleSpeechConfig? SpeechConfig { get; set; }
    
    /// <summary>
    /// Empty Google extensions.
    /// </summary>
    public ChatRequestVendorGoogleExtensions()
    {
        
    }

    /// <summary>
    /// Cached content will be used for responses.
    /// </summary>
    /// <param name="cachedContent"></param>
    public ChatRequestVendorGoogleExtensions(string cachedContent)
    {
        CachedContent = cachedContent;
    }
    
    /// <summary>
    /// Cached content will be used for responses.
    /// </summary>
    /// <param name="cachedContent"></param>
    public ChatRequestVendorGoogleExtensions(CachedContentInformation cachedContent)
    {
        CachedContent = cachedContent.Name;
        CachedContentInformation = cachedContent;
    }
}

/// <summary>
/// Configuration of speech.
/// </summary>
public class ChatRequestVendorGoogleSpeechConfig
{
    /// <summary>
    /// The name of the preset voice to use for single-speaker output.
    /// </summary>
    public ChatRequestVendorGoogleSpeechConfigPrebuiltVoice? VoiceName { get; set; }
    
    /// <summary>
    /// One of: de-DE, en-AU, en-GB, en-IN, en-US, es-US, fr-FR<br/>
    /// hi-IN, pt-BR, ar-XA, es-ES, fr-CA, id-ID, it-IT<br/>
    /// ja-JP, tr-TR, vi-VN, bn-IN, gu-IN, kn-IN, ml-IN<br/>
    /// mr-IN, ta-IN, te-IN, nl-NL, ko-KR, cmn-CN, pl-PL, ru-RU, and th-TH.
    /// </summary>
    public string? LanguageCode { get; set; }
    
    public ChatRequestVendorGoogleSpeechConfigMultiSpeaker? MultiSpeaker { get; set; }
}

/// <summary>
/// Configuration for multiple speakers.
/// </summary>
public class ChatRequestVendorGoogleSpeechConfigMultiSpeaker
{
    public List<ChatRequestVendorGoogleSpeechConfigMultiSpeakerSpeaker> Speakers { get; set; } = [];
}

/// <summary>
/// Configuration of a speaker.
/// </summary>
public class ChatRequestVendorGoogleSpeechConfigMultiSpeakerSpeaker
{
    /// <summary>
    /// Handle of the speaker, this must be referenced in the script to read.
    /// </summary>
    public string Speaker { get; set; }
    
    public ChatRequestVendorGoogleSpeechConfigPrebuiltVoice? Voice { get; set; }

    /// <summary>
    /// Creates a new configuration for a speaker.
    /// </summary>
    /// <param name="speaker"></param>
    public ChatRequestVendorGoogleSpeechConfigMultiSpeakerSpeaker(string speaker)
    {
        Speaker = speaker;
    }
    
    /// <summary>
    /// Creates a new configuration for a speaker.
    /// </summary>
    /// <param name="speaker"></param>
    /// <param name="voice"></param>
    public ChatRequestVendorGoogleSpeechConfigMultiSpeakerSpeaker(string speaker, ChatRequestVendorGoogleSpeakerVoices voice)
    {
        Speaker = speaker;
        Voice = new ChatRequestVendorGoogleSpeechConfigPrebuiltVoice
        {
            VoiceName = voice
        };
    }
}

/// <summary>
/// 
/// </summary>
public class ChatRequestVendorGoogleSpeechConfigPrebuiltVoice
{
    /// <summary>
    /// The name of the preset voice to use.
    /// </summary>
    public ChatRequestVendorGoogleSpeakerVoices? VoiceName { get; set; }
}

/// <summary>
/// Represents the available speaker voices.
/// </summary>
public enum ChatRequestVendorGoogleSpeakerVoices
{
    /// <summary>
    /// Bright
    /// </summary>
    Zephyr,

    /// <summary>
    /// Upbeat
    /// </summary>
    Puck,

    /// <summary>
    /// Informative
    /// </summary>
    Charon,

    /// <summary>
    /// Firm
    /// </summary>
    Kore,

    /// <summary>
    /// Excitable
    /// </summary>
    Fenrir,

    /// <summary>
    /// Youthful
    /// </summary>
    Leda,

    /// <summary>
    /// Firm
    /// </summary>
    Orus,

    /// <summary>
    /// Breezy
    /// </summary>
    Aoede,

    /// <summary>
    /// Easy-going
    /// </summary>
    Callirrhoe,

    /// <summary>
    /// Bright
    /// </summary>
    Autonoe,

    /// <summary>
    /// Breathy
    /// </summary>
    Enceladus,

    /// <summary>
    /// Clear
    /// </summary>
    Iapetus,

    /// <summary>
    /// Easy-going
    /// </summary>
    Umbriel,

    /// <summary>
    /// Smooth
    /// </summary>
    Algieba,

    /// <summary>
    /// Smooth
    /// </summary>
    Despina,

    /// <summary>
    /// Clear
    /// </summary>
    Erinome,

    /// <summary>
    /// Gravelly
    /// </summary>
    Algenib,

    /// <summary>
    /// Informative
    /// </summary>
    Rasalgethi,

    /// <summary>
    /// Upbeat
    /// </summary>
    Laomedeia,

    /// <summary>
    /// Soft
    /// </summary>
    Achernar,

    /// <summary>
    /// Firm
    /// </summary>
    Alnilam,

    /// <summary>
    /// Even
    /// </summary>
    Schedar,

    /// <summary>
    /// Mature
    /// </summary>
    Gacrux,

    /// <summary>
    /// Forward
    /// </summary>
    Pulcherrima,

    /// <summary>
    /// Friendly
    /// </summary>
    Achird,

    /// <summary>
    /// Casual
    /// </summary>
    Zubenelgenubi,

    /// <summary>
    /// Gentle
    /// </summary>
    Vindemiatrix,

    /// <summary>
    /// Lively
    /// </summary>
    Sadachbia,

    /// <summary>
    /// Knowledgeable
    /// </summary>
    Sadaltager,

    /// <summary>
    /// Warm
    /// </summary>
    Sulafat
}