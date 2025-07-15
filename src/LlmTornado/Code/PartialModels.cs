using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Images;
using LlmTornado.Audio;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code.Models;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Code;

public class Ref<T>
{
    public T? Ptr { get; set; }
}

/// <summary>
/// Endpoints with which a model is compatible.
/// </summary>
public enum ChatModelEndpointCapabilities
{
    /// <summary>
    /// /chat
    /// </summary>
    Chat,
    /// <summary>
    /// /responses
    /// </summary>
    Responses,
    /// <summary>
    /// /batch
    /// </summary>
    Batch
}

internal class StreamResponse
{
    public Stream Stream { get; set; }
    public ApiResultBase Headers { get; set; }
    public HttpResponseMessage Response { get; set; }
}

/// <summary>
///     A failed HTTP request.
/// </summary>
public class HttpFailedRequest
{
    /// <summary>
    ///     The exception with details what went wrong.
    /// </summary>
    public Exception Exception { get; set; }
    
    /// <summary>
    ///     The request that failed.
    /// </summary>
    public HttpCallRequest? Request { get; set; }
    
    /// <summary>
    ///     Result of the failed request.
    /// </summary>
    public IHttpCallResult? Result { get; set; }
    
    /// <summary>
    ///     Raw message of the failed request. Do not dispose this, it will be disposed automatically by Tornado.
    /// </summary>
    public HttpResponseMessage RawMessage { get; set; }
    
    /// <summary>
    ///     Body of the request.
    /// </summary>
    public TornadoRequestContent Body { get; set; }
}

/// <summary>
///     Streaming HTTP request.
/// </summary>
public class TornadoStreamRequest : IAsyncDisposable
{
    public Stream? Stream { get; set; }
    public HttpResponseMessage? Response { get; set; }
    public StreamReader? StreamReader { get; set; }
    public Exception? Exception { get; set; }
    public HttpCallRequest? CallRequest { get; set; }
    public IHttpCallResult? CallResponse { get; set; }

    /// <summary>
    ///     Disposes the underlying stream.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Stream is not null)
        {
#if MODERN
            await Stream.DisposeAsync().ConfigureAwait(false);   
#else
            Stream.Dispose();
#endif
        }
        
        Response?.Dispose();
        StreamReader?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
///     Roles of chat participants.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatMessageRoles
{
    /// <summary>
    ///     Unknown role.
    /// </summary>
    [EnumMember(Value = "unknown")]
    Unknown,
    /// <summary>
    ///     System prompt / preamble / developer message.
    /// </summary>
    [EnumMember(Value = "system")]
    System,
    /// <summary>
    ///     Messages written by user.
    /// </summary>
    [EnumMember(Value = "user")]
    User,
    /// <summary>
    ///     Assistant messages.
    /// </summary>
    [EnumMember(Value = "assistant")]
    Assistant,
    /// <summary>
    ///     Messages representing tool/function/connector usage.
    /// </summary>
    [EnumMember(Value = "tool")]
    Tool
}

/// <summary>
/// The type of the predicted content you want to provide. 
/// </summary>
public enum ChatRequestPredictionTypes
{
    /// <summary>
    /// Static predicted output content.
    /// </summary>
    Content
}

/// <summary>
/// Configuration for a Predicted Output, which can greatly improve response times when large parts of the model response are known ahead of time. This is most common when you are regenerating a file with only minor changes to most of the content.
/// </summary>
public class ChatRequestPrediction
{
    /// <summary>
    /// The type of the predicted content you want to provide. This type is currently always "content".
    /// </summary>
    [JsonProperty("type")]
    public ChatRequestPredictionTypes Type { get; set; }

    /// <summary>
    /// Serialized content, either from <see cref="Parts"/> or <see cref="Text"/>
    /// </summary>
    [JsonProperty("content")]
    public object? Content => Parts?.Count > 0 ? Parts : Text;

    /// <summary>
    /// The content used for a Predicted Output. This is often the text of a file you are regenerating with minor changes.
    /// </summary>
    [JsonIgnore]
    public string? Text { get; set; }
    
    /// <summary>
    /// An array of content parts with a defined type. Supported options differ based on the model being used to generate the response. Can contain text inputs.
    /// </summary>
    [JsonIgnore]
    public List<ChatRequestPredictionPart>? Parts { get; set; }
}

/// <summary>
/// High level guidance for the amount of context window space to use for the search. One of low, medium, or high. medium is the default.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestWebSearchContextSize
{
    /// <summary>
    /// Low context, lowest cost.
    /// </summary>
    [EnumMember(Value = "low")]
    Low,
    
    /// <summary>
    /// Balanced cost/performance.
    /// </summary>
    [EnumMember(Value = "medium")]
    Medium,
    
    /// <summary>
    /// Highest budget, best performance.
    /// </summary>
    [EnumMember(Value = "high")]
    High
}

/// <summary>
/// Types of the user location.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestWebSearchUserLocationTypes
{
    /// <summary>
    /// Approximate location.
    /// </summary>
    [EnumMember(Value = "approximate")]
    Approximate
}

/// <summary>
/// Configuration of the user location, aids search relevancy.
/// </summary>
public class ChatRequestWebSearchUserLocation
{
    /// <summary>
    /// The type of location approximation. Always approximate.
    /// </summary>
    [JsonIgnore]
    public ChatRequestWebSearchUserLocationTypes Type { get; set; } = ChatRequestWebSearchUserLocationTypes.Approximate;
    
    /// <summary>
    /// Free text input for the city of the user, e.g. San Francisco.
    /// </summary>
    [JsonProperty("city")]
    public string? City { get; set; }
    
    /// <summary>
    /// The two-letter ISO country code of the user, e.g. US.
    /// </summary>
    [JsonProperty("country")]
    public string? Country { get; set; }
    
    /// <summary>
    /// Free text input for the region of the user, e.g. California.
    /// </summary>
    [JsonProperty("region")]
    public string? Region { get; set; }
    
    /// <summary>
    /// The IANA timezone of the user, e.g. America/Los_Angeles.
    /// </summary>
    [JsonProperty("timezone")]
    public string? Timezone { get; set; }
}


/// <summary>
/// Thinking blocks for Anthropic Claude 3.7+ models.
/// </summary>
public class ChatChoiceAnthropicThinkingBlock
{
    /// <summary>
    /// Content of the thinking block.
    /// </summary>
    public string Content { get; set; }
	
    /// <summary>
    /// This field holds a cryptographic token which verifies that the thinking block was generated by Claude, and is verified when thinking blocks are passed back to the API. When streaming responses, the signature is added via a signature_delta inside a content_block_delta event just before the content_block_stop event. It is only strictly necessary to send back thinking blocks when using tool use with extended thinking. Otherwise you can omit thinking blocks from previous turns, or let the API strip them for you if you pass them back.
    /// </summary>
    public string Signature { get; set; }
}

/// <summary>
/// Anthropic extensions to chat choices.
/// </summary>
public class ChatChoiceVendorExtensionsAnthropic : IChatChoiceVendorExtensions
{
    /// <summary>
    /// Thinking blocks.
    /// </summary>
    public List<ChatChoiceAnthropicThinkingBlock>? Thinking { get; set; }
}

/// <summary>
/// A custom JSON converter for handling different response formats.
/// </summary>
internal class ChatMessageFinishReasonsConverter : JsonConverter<ChatMessageFinishReasons>
{
    internal static readonly FrozenDictionary<string, ChatMessageFinishReasons> Map = new Dictionary<string, ChatMessageFinishReasons>
    {
        { "stop", ChatMessageFinishReasons.EndTurn },
        { "end_turn", ChatMessageFinishReasons.EndTurn },
        { "STOP", ChatMessageFinishReasons.EndTurn },
        { "COMPLETE", ChatMessageFinishReasons.EndTurn },

        { "stop_sequence", ChatMessageFinishReasons.StopSequence },
        { "STOP_SEQUENCE", ChatMessageFinishReasons.StopSequence },

        { "length", ChatMessageFinishReasons.Length },
        { "max_tokens", ChatMessageFinishReasons.Length },
        { "MAX_TOKENS", ChatMessageFinishReasons.Length },
        { "ERROR_LIMIT", ChatMessageFinishReasons.Length },

        { "content_filter", ChatMessageFinishReasons.ContentFilter },
        { "SAFETY", ChatMessageFinishReasons.ContentFilter },
        { "ERROR_TOXIC", ChatMessageFinishReasons.ContentFilter },

        { "RECITATION", ChatMessageFinishReasons.Recitation },
        { "LANGUAGE", ChatMessageFinishReasons.UnsupportedLanguage },
        { "BLOCKLIST", ChatMessageFinishReasons.Blocklist },
        { "PROHIBITED_CONTENT", ChatMessageFinishReasons.ProhibitedContent },
        { "SPII", ChatMessageFinishReasons.SensitivePersonalInformation },
        { "MALFORMED_FUNCTION_CALL", ChatMessageFinishReasons.MalformedToolCall },
        { "IMAGE_SAFETY", ChatMessageFinishReasons.ImageSafety },
        { "USER_CANCEL", ChatMessageFinishReasons.Cancel },
        { "ERROR", ChatMessageFinishReasons.Error },

        { "tool_use", ChatMessageFinishReasons.ToolCalls },
        { "tool_calls", ChatMessageFinishReasons.ToolCalls },
        { "function_call", ChatMessageFinishReasons.ToolCalls },

    }.ToFrozenDictionary();
    
    /// <summary>
    /// Reads and converts JSON input into the appropriate ResponseFormat object.
    /// </summary>
    public override ChatMessageFinishReasons ReadJson(JsonReader reader, Type objectType, ChatMessageFinishReasons existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.String)
        {
            string str = reader.Value as string ?? string.Empty;
            return Map.GetValueOrDefault(str, ChatMessageFinishReasons.Unknown);   
        }

        return ChatMessageFinishReasons.Unknown;
    }

    /// <summary>
    /// Writes the ResponseFormat object as JSON output.
    /// </summary>
    public override void WriteJson(JsonWriter writer, ChatMessageFinishReasons value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}

/// <summary>
/// Types of reasons for ending the response.
/// </summary>
public enum ChatMessageFinishReasons
{
    /// <summary>
    /// Unknown reason, used as a fallback.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// The model hit a natural stop point. Most providers report hitting stop sequences as "stop".
    /// </summary>
    EndTurn,
    
    /// <summary>
    /// The model hit your stop sequence. Currently reported only by Anthropic, Cohere.
    /// </summary>
    StopSequence,
    
    /// <summary>
    /// The maximum number of tokens specified in the request was reached.
    /// </summary>
    Length,
    
    /// <summary>
    /// Content was omitted due to a flag from content filters.
    /// </summary>
    ContentFilter,
    
    /// <summary>
    /// The model called tools.
    /// </summary>
    ToolCalls,
    
    /// <summary>
    /// The response candidate content was flagged for recitation reasons.
    /// </summary>
    Recitation,
    
    /// <summary>
    /// The response candidate content was flagged for using an unsupported language.
    /// </summary>
    UnsupportedLanguage,
    
    /// <summary>
    /// Token generation stopped because the content contains forbidden terms.
    /// </summary>
    Blocklist,
    
    /// <summary>
    /// Token generation stopped for potentially containing prohibited content.
    /// </summary>
    ProhibitedContent,
    
    /// <summary>
    /// The generation stopped because the content potentially contains Sensitive Personally Identifiable Information (SPII).
    /// </summary>
    SensitivePersonalInformation,
    
    /// <summary>
    /// The tool call generated by the model is invalid.
    /// </summary>
    MalformedToolCall,
    
    /// <summary>
    /// Token generation stopped because generated images contain safety violations.
    /// </summary>
    ImageSafety,
    
    /// <summary>
    /// The request was cancelled.
    /// </summary>
    Cancel,
    
    /// <summary>
    /// There was an error while processing the request.
    /// </summary>
    Error
}

/// <summary>
/// Shared interface for vendor extensions to chat choices.
/// </summary>
public interface IChatChoiceVendorExtensions
{
	
}

/// <summary>
/// Configuration of the web search options.
/// </summary>
[JsonConverter(typeof(ChatRequestWebSearchOptionsJsonConverter))]
public class ChatRequestWebSearchOptions
{
    /// <summary>
    /// High level guidance for the amount of context window space to use for the search. One of low, medium, or high. medium is the default.
    /// </summary>
    [JsonProperty("search_context_size")]
    public ChatRequestWebSearchContextSize? SearchContextSize { get; set; }
    
    /// <summary>
    /// Approximate location parameters for the search.
    /// </summary>
    [JsonProperty("user_location")]
    public ChatRequestWebSearchUserLocation? UserLocation { get; set; }
}

internal class ChatRequestWebSearchOptionsJsonConverter : JsonConverter<ChatRequestWebSearchOptions>
{
    public override void WriteJson(JsonWriter writer, ChatRequestWebSearchOptions? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        if (value.SearchContextSize.HasValue)
        {
            writer.WritePropertyName("search_context_size");
            serializer.Serialize(writer, value.SearchContextSize.Value);
        }

        if (value.UserLocation != null)
        {
            writer.WritePropertyName("user_location");
            writer.WriteStartObject();
        
            writer.WritePropertyName("type");
            writer.WriteValue("approximate");
            
            writer.WritePropertyName("approximate");
            writer.WriteStartObject();
        
            if (!string.IsNullOrEmpty(value.UserLocation.City))
            {
                writer.WritePropertyName("city");
                writer.WriteValue(value.UserLocation.City);
            }
        
            if (!string.IsNullOrEmpty(value.UserLocation.Country))
            {
                writer.WritePropertyName("country");
                writer.WriteValue(value.UserLocation.Country);
            }
        
            if (!string.IsNullOrEmpty(value.UserLocation.Region))
            {
                writer.WritePropertyName("region");
                writer.WriteValue(value.UserLocation.Region);
            }
        
            if (!string.IsNullOrEmpty(value.UserLocation.Timezone))
            {
                writer.WritePropertyName("timezone");
                writer.WriteValue(value.UserLocation.Timezone);
            }
        
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    public override ChatRequestWebSearchOptions? ReadJson(JsonReader reader, Type objectType, ChatRequestWebSearchOptions? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return existingValue;
    }
}

/// <summary>
/// A prediction part.
/// </summary>
public class ChatRequestPredictionPart 
{
    /// <summary>
    /// The text content.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; }
    
    /// <summary>
    /// The type of the content part.
    /// </summary>
    [JsonProperty("type")]
    public ChatRequestPredictionTypes Type { get; set; }

    /// <summary>
    /// Creates a new text prediction part.
    /// </summary>
    /// <param name="text"></param>
    public ChatRequestPredictionPart(string text)
    {
        Text = text;
        Type = ChatRequestPredictionTypes.Content;
    }
}

/// <summary>
/// Flex processing provides significantly lower costs for Chat Completions or Responses requests in exchange for slower response times and occasional resource unavailability. It is ideal for non-production or lower-priority tasks such as model evaluations, data enrichment, or asynchronous workloads.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestServiceTiers
{
    /// <summary>
    /// If set to 'auto', and the Project is Scale tier enabled, the system will utilize scale tier credits until they are exhausted.
    /// If set to 'auto', and the Project is not Scale tier enabled, the request will be processed using the default service tier with a lower uptime SLA and no latency guarantee.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,
    
    /// <summary>
    /// If set to 'default', the request will be processed using the default service tier with a lower uptime SLA and no latency guarantee.
    /// </summary>
    [EnumMember(Value = "default")]
    Default,
    
    /// <summary>
    /// If set to 'flex', the request will be processed with the Flex Processing service tier. Learn more.
    /// </summary>
    [EnumMember(Value = "flex")]
    Flex
}

/// <summary>
/// Represents one token/chunk of a streamed response
/// </summary>
public class StreamedMessageToken
{
    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Index of the token.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Text representation
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Content ?? string.Empty;
    }
}

/// <summary>
///     Level of reasoning suggested.
/// </summary>
public enum ChatReasoningEfforts
{
    /// <summary>
    ///     Low reasoning - fast responses (O1, O1 Mini, Grok 3)
    /// </summary>
    [JsonProperty("low")]
    Low,
    /// <summary>
    ///     Balanced reasoning (O1, O1 Mini)
    /// </summary>
    [JsonProperty("medium")]
    Medium,
    /// <summary>
    ///     High reasoning - slow responses (O1, O1 Mini, Grok 3)
    /// </summary>
    [JsonProperty("high")]
    High
}

internal enum ChatResultStreamInternalKinds
{
    Unknown,
    None,
    
    /// <summary>
    /// Appends the message to the conversation.
    /// </summary>
    AppendAssistantMessage,
    
    /// <summary>
    /// Similar to <see cref="AppendAssistantMessage"/> but doesn't append the message. Used for reasoning blocks.
    /// </summary>
    AssistantMessageTransientBlock,
    
    /// <summary>
    /// Usage, finish_reason, and other metadata
    /// </summary>
    FinishData
}

/// <summary>
/// Data returned after streaming stops.
/// </summary>
public class ChatStreamFinishedData
{
    /// <summary>
    /// The bill.
    /// </summary>
    public ChatUsage Usage { get; set; }
    
    /// <summary>
    /// Reason why the streaming stopped.
    /// </summary>
    public ChatMessageFinishReasons FinishReason { get; set; }

    internal ChatStreamFinishedData(ChatUsage usage, ChatMessageFinishReasons finishReason)
    {
        Usage = usage;
        FinishReason = finishReason;
    }
}

/// <summary>
/// Wrapper for tool arguments.
/// </summary>
public class ChatFunctionParamsGetter
{
    internal Dictionary<string, object?>? Source { get; set; }

    /// <summary>
    /// Creates an argument getter from a dictionary.
    /// </summary>
    /// <param name="pars"></param>
    public ChatFunctionParamsGetter(Dictionary<string, object?>? pars)
    {
        Source = pars;
    }
}

internal class ToolCallInboundAccumulator
{
    public ToolCall ToolCall { get; set; }
    public StringBuilder ArgumentsBuilder { get; set; } = new StringBuilder();
}

/// <summary>
///     Audio block content.
/// </summary>
public class ChatMessageAudio
{
    /// <summary>
    ///     Unique identifier for this audio response.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
    
    /// <summary>
    ///     The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    /// </summary>
    [JsonProperty("expires_at")]
    public long ExpiresAt { get; set; }
    
    /// <summary>
    ///     Base64 encoded audio bytes generated by the model, in the format specified in the request.
    /// </summary>
    [JsonProperty("data")]
    public string? Data { get; set; }

    /// <summary>
    ///     Converts <see cref="Data"/> from base64 to a byte array.
    /// </summary>
    public byte[] ByteData => Data is null ? [] : Convert.FromBase64String(Data);
    
    /// <summary>
    ///     Transcript of the audio generated by the model.
    /// </summary>
    [JsonProperty("transcript")]
    public string Transcript { get; set; }
    
    /// <summary>
    /// MIME type of the audio.
    /// </summary>
    [JsonIgnore]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// Format of the audio.
    /// </summary>
    [JsonIgnore]
    public ChatAudioFormats? Format { get; set; }
}

/// <summary>
///     Strategies for reducing audio modality pricing.
/// </summary>
public enum ChatAudioCompressionStrategies
{
    /// <summary>
    ///     Audio encoding is preferred both for input and output.
    /// </summary>
    Native,
    
    /// <summary>
    ///     Output is encoded as text when possible.
    /// </summary>
    OutputAsText,
    
    /// <summary>
    ///     Output is encoded as previous audio id when not expired; falls to <see cref="OutputAsText"/> otherwise.
    /// </summary>
    PreferNative
}

/// <summary>
///     Audio settings.
/// </summary>
public class ChatRequestAudio
{
    /// <summary>
    ///     The voice to use.
    /// </summary>
    [JsonProperty("voice")]
    [JsonConverter(typeof(StringEnumConverter), true)]
    public ChatAudioRequestKnownVoices Voice { get; set; }
    
    /// <summary>
    ///     The output audio format.
    /// </summary>
    [JsonProperty("format")]
    [JsonConverter(typeof(StringEnumConverter), true)]
    public ChatRequestAudioFormats Format { get; set; }

    /// <summary>
    ///     The compression strategy to use when serializing requests.
    /// </summary>
    [JsonIgnore] 
    public ChatAudioCompressionStrategies CompressionStrategy { get; set; } = ChatAudioCompressionStrategies.PreferNative;
    
    /// <summary>
    ///     Creates a new audio settings from a known voice.
    /// </summary>
    /// <param name="voice"></param>
    /// <param name="format"></param>
    public ChatRequestAudio(ChatAudioRequestKnownVoices voice, ChatRequestAudioFormats format)
    {
        Voice = voice;
        Format = format;
    }
}

/// <summary>
///     Formats in which the transcription can be returned.
/// </summary>
public enum AudioTranscriptionResponseFormats
{
    /// <summary>
    ///     JSON.
    /// </summary>
    [JsonProperty("json")]
    Json,
    
    /// <summary>
    ///     Plaintext.
    /// </summary>
    [JsonProperty("text")]
    Text,
    
    /// <summary>
    ///     SubRip Subtitle.
    /// </summary>
    [JsonProperty("srt")]
    Srt,
    
    /// <summary>
    ///     Json with details.
    /// </summary>
    [JsonProperty("verbose_json")]
    VerboseJson,
    
    /// <summary>
    ///     Video Text to Track.
    /// </summary>
    [JsonProperty("vtt")]
    Vtt
}

/// <summary>
///     Output audio formats.
/// </summary>
public enum ChatRequestAudioFormats
{
    /// <summary>
    ///     Waveform
    /// </summary>
    [JsonProperty("wav")]
    Wav,
    /// <summary>
    ///     MP3
    /// </summary>
    [JsonProperty("mp3")]
    Mp3,
    /// <summary>
    ///     Flac
    /// </summary>
    [JsonProperty("flac")]
    Flac,
    /// <summary>
    ///     Opus
    /// </summary>
    [JsonProperty("opus")]
    Opus,
    /// <summary>
    ///    Pulse-code modulation. Supported in streaming mode.
    /// </summary>
    [JsonProperty("pcm16")]
    Pcm16
}

/// <summary>
///     Known chat request audio settings voices.
/// </summary>
public enum ChatAudioRequestKnownVoices
{
    /// <summary>
    ///     Male voice, deep.
    /// </summary>
    [JsonProperty("ash")]
    
    Ash,
    /// <summary>
    ///     Male voice, younger.
    /// </summary>
    [JsonProperty("ballad")]
    Ballad,
    
    [JsonProperty("coral")]
    Coral,
    [JsonProperty("sage")]
    Sage,
    [JsonProperty("verse")]
    Verse,
    
    /// <summary>
    ///     Not recommended by OpenAI, less expressive.
    /// </summary>
    [JsonProperty("alloy")]
    Alloy,
    /// <summary>
    ///     Not recommended by OpenAI, less expressive.
    /// </summary>
    [JsonProperty("echo")]
    Echo,
    /// <summary>
    ///     Not recommended by OpenAI, less expressive.
    /// </summary>
    [JsonProperty("summer")]
    Summer
}

/// <summary>
///     Represents modalities of chat models.
/// </summary>
public enum ChatModelModalities
{
    /// <summary>
    ///     Model is capable of generating text.
    /// </summary>
    Text,
    /// <summary>
    ///     Model is capable of generating audio.
    /// </summary>
    Audio,
    /// <summary>
    ///     Model is capable of generating images (currently only Gemini 2.0+)
    /// </summary>
    Image
}

/// <summary>
///     Represents an audio part of a chat message.
/// </summary>
public class ChatAudio
{
    /// <summary>
    ///     Base64 encoded audio data.
    /// </summary>
    public string Data { get; set; }
    
    /// <summary>
    ///     MimeType of the audio.
    /// </summary>
    public string? MimeType { get; set; }
    
    /// <summary>
    ///     Format of the encoded audio data.
    /// </summary>
    public ChatAudioFormats Format { get; set; }

    /// <summary>
    ///     Creates an empty audio instance.
    /// </summary>
    public ChatAudio()
    {
        
    }

    /// <summary>
    ///     Creates an audio instance from data and format.
    /// </summary>
    /// <param name="data">Base64 encoded audio data</param>
    /// <param name="format">Format of the audio</param>
    public ChatAudio(string data, ChatAudioFormats format)
    {
        Data = data;
        Format = format;
    }
    
    /// <summary>
    ///     Creates an audio instance from data and format.
    /// </summary>
    /// <param name="data">Base64 encoded audio data</param>
    /// <param name="format">Format of the audio</param>
    /// <param name="mimeType">Mime type</param>
    public ChatAudio(string data, ChatAudioFormats format, string mimeType)
    {
        Data = data;
        Format = format;
        MimeType = mimeType;
    }
}

/// <summary>
///     Supported audio formats.
/// </summary>
public enum ChatAudioFormats
{
    /// <summary>
    /// Wavelet (input, conversion)
    /// </summary>
    Wav,
    /// <summary>
    /// MP3 (input, conversion)
    /// </summary>
    Mp3,
    /// <summary>
    /// L16/PCM (output only)
    /// </summary>
    L16
}

/// <summary>
/// Ways to link a document to a <see cref="ChatMessagePart"/>
/// </summary>
public enum DocumentLinkTypes
{
    /// <summary>
    /// Publicly reachable, absolute URL.
    /// </summary>
    Url,
    
    /// <summary>
    /// Base64 encoded document.
    /// </summary>
    Base64
}

/// <summary>
///     Represents a chat document (PDF).
/// </summary>
public class ChatDocument
{
    /// <summary>
    /// Base64 encoded data.
    /// </summary>
    public string? Base64 { get; set; }
    
    /// <summary>
    /// Publicly reachable URL serving the document.
    /// </summary>
    public Uri? Uri { get; set; }
    
    /// <summary>
    ///     Creates a new chat document
    /// </summary>
    /// <param name="base64">Base64 encoded data.</param>
    public ChatDocument(string base64)
    {
        Base64 = base64;
    }
    
    /// <summary>
    ///     Creates a new chat document
    /// </summary>
    /// <param name="uri">Publicly reachable URL serving the document.</param>
    public ChatDocument(Uri uri)
    {
        Uri = uri;
    }
}

/// <summary>
///     Represents a chat image
/// </summary>
public class ChatImage
{
    /// <summary>
    ///     Creates a new chat image
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    public ChatImage(string content)
    {
        Url = content;
    }

    /// <summary>
    ///     Creates a new chat image
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    /// <param name="detail">The detail level to use, defaults to <see cref="ImageDetail.Auto" /></param>
    public ChatImage(string content, ImageDetail? detail)
    {
        Url = content;
        Detail = detail;
    }
    
    /// <summary>
    ///     When using base64 encoding in <see cref="Url"/>, certain providers such as Google require <see cref="MimeType"/> to be set.
    ///     Values supported by Google: image/png, image/jpeg
    /// </summary>
    [JsonIgnore]
    public string? MimeType { get; set; }

    /// <summary>
    ///     Publicly available URL to the image or base64 encoded content
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; }

    /// <summary>
    ///     Publicly available URL to the image or base64 encoded content
    /// </summary>
    [JsonProperty("detail")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ImageDetail? Detail { get; set; }
}

/// <summary>
/// Known LLM providers.
/// </summary>
public enum LLmProviders
{
    /// <summary>
    /// Provider not resolved.
    /// </summary>
    Unknown,
    /// <summary>
    /// OpenAI.
    /// </summary>
    OpenAi,
    /// <summary>
    /// Anthropic.
    /// </summary>
    Anthropic,
    /// <summary>
    /// Azure OpenAI.
    /// </summary>
    AzureOpenAi,
    /// <summary>
    /// Cohere.
    /// </summary>
    Cohere,
    /// <summary>
    /// Ollama, vLLM, KoboldCpp and other (self-hosted) providers.
    /// </summary>
    Custom,
    /// <summary>
    /// Google.
    /// </summary>
    Google,
    /// <summary>
    /// Groq.
    /// </summary>
    Groq,
    /// <summary>
    /// DeepSeek.
    /// </summary>
    DeepSeek,
    /// <summary>
    /// Mistral.
    /// </summary>
    Mistral,
    /// <summary>
    /// xAI.
    /// </summary>
    XAi,
    /// <summary>
    /// Perplexity.
    /// </summary>
    Perplexity,
    /// <summary>
    /// Voyage.
    /// </summary>
    Voyage,
    /// <summary>
    /// DeepInfra.
    /// </summary>
    DeepInfra,
    /// <summary>
    /// Open Router.
    /// </summary>
    OpenRouter,
    /// <summary>
    /// Internal value.
    /// </summary>
    Length
}

/// <summary>
/// Capability endpoints - each provider supports a subset of these.
/// </summary>
public enum CapabilityEndpoints
{
    /// <summary>
    /// Returns input url
    /// </summary>
    None,
    /// <summary>
    /// Special value returning the base url
    /// </summary>
    BaseUrl,
    /// <summary>
    /// Special value returning even shorter shared url prefix
    /// </summary>
    BaseUrlStripped,
    /// <summary>
    /// Chat endpoint.
    /// </summary>
    Chat,
    /// <summary>
    /// Moderation endpoint.
    /// </summary>
    Moderation,
    /// <summary>
    /// Legacy.
    /// </summary>
    Completions,
    /// <summary>
    /// Embeddings endpoint.
    /// </summary>
    Embeddings,
    /// <summary>
    /// Models endpoint.
    /// </summary>
    Models,
    /// <summary>
    /// Files endpoint.
    /// </summary>
    Files,
    /// <summary>
    /// Uploads endpoint.
    /// </summary>
    Uploads,
    /// <summary>
    /// Images endpoint.
    /// </summary>
    ImageGeneration,
    /// <summary>
    /// Audio endpoint.
    /// </summary>
    Audio,
    /// <summary>
    /// Assistants endpoint.
    /// </summary>
    Assistants,
    /// <summary>
    /// Image editing endpoint.
    /// </summary>
    ImageEdit,
    /// <summary>
    /// Threads endpoint.
    /// </summary>
    Threads,
    /// <summary>
    /// Fine tuning endpoint.
    /// </summary>
    FineTuning,
    /// <summary>
    /// Vector stores endpoint.
    /// </summary>
    VectorStores,
    /// <summary>
    /// Caching endpoint.
    /// </summary>
    Caching,
    /// <summary>
    /// Responses endpoint.
    /// </summary>
    Responses
}

/// <summary>
/// Shared interface by all chat usages.
/// </summary>
public interface IChatUsage
{
    
}

/// <summary>
/// Represents authentication to a single provider.
/// </summary>
public class ProviderAuthentication
{
    /// <summary>
    /// The provider.
    /// </summary>
    public LLmProviders Provider { get; set; }
    
    /// <summary>
    /// API key, if any.
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Organization, if any.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Crates a new authentication.
    /// </summary>
    public ProviderAuthentication(LLmProviders provider, string apiKey, string? organization = null)
    {
        Provider = provider;
        ApiKey = apiKey;
        Organization = organization;
    }
    
    /// <summary>
    /// Creates a new authentication. This constructor can be used when creating authentication if context of an existing <see cref="IEndpointProvider"/> instance.
    /// </summary>
    public ProviderAuthentication(string apiKey, string? organization = null)
    {
        ApiKey = apiKey;
        Organization = organization;
    }
}

/// <summary>
/// Types of inbound streams.
/// </summary>
public enum StreamRequestTypes
{
    /// <summary>
    /// Unrecognized stream.
    /// </summary>
    Unknown,
    /// <summary>
    /// Chat/completion stream.
    /// </summary>
    Chat
}

internal interface IModelRequest
{
    internal IModel? RequestModel { get; }
}

/// <summary>
///  A Tornado HTTP request.
/// </summary>
public class TornadoRequestContent
{
    /// <summary>
    /// Content of the request.
    /// </summary>
    public object Body { get; set; }
    
    /// <summary>
    /// Model associated with this request.
    /// </summary>
    public IModel? Model { get; set; }
    
    /// <summary>
    /// Forces the URl to differ from the one inferred further down the pipeline.
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// The provider this request is outbound to.
    /// </summary>
    [JsonIgnore]
    public IEndpointProvider? Provider { get; set; }

    /// <summary>
    /// The endpoint this request corresponds to.
    /// </summary>
    [JsonIgnore]
    public CapabilityEndpoints? CapabilityEndpoint { get; set; }
    
    /// <summary>
    /// Headers are normally not set as they often contain secrets (API keys). For debugging, use <see cref="ChatRequestSerializeOptions.IncludeHeaders"/>
    /// </summary>
    public Dictionary<string, IEnumerable<string>>? Headers { get; set; }
    
    internal TornadoRequestContent(object body, IModel? model, string? url, IEndpointProvider provider, CapabilityEndpoints endpoint)
    {
        Body = body;
        Url = url;
        Provider = provider;
        CapabilityEndpoint = endpoint;
        Model = model;
    }

    /// <summary>
    /// This method can be used to see the final outbound url before executing the request.
    /// </summary>
    /// <returns></returns>
    public string? BuildFinalUrl()
    {
        if (Provider is null || CapabilityEndpoint is null)
        {
            return Url;
        }

        return EndpointBase.BuildRequestUrl(Url, Provider, CapabilityEndpoint.Value, Model);
    }

    internal static TornadoRequestContent Dummy => new TornadoRequestContent(new { }, null, null, new OpenAiEndpointProvider(LLmProviders.OpenAi), CapabilityEndpoints.Chat);

    internal TornadoRequestContent()
    {
        
    }

    /// <summary>
    /// Textual representation of the request - URL & Body.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Url: {Url}");

        if (Headers is not null)
        {
            sb.AppendLine("Headers:");
            sb.AppendLine("-------------------");

            foreach (KeyValuePair<string, IEnumerable<string>> header in Headers.OrderBy(x => x.Key, StringComparer.InvariantCulture))
            {
                sb.AppendLine($"{header.Key}: {string.Join(";", header.Value)}");
            }
        }

        sb.AppendLine("Body:");
        sb.AppendLine("-------------------");
        sb.AppendLine(Body.ToString());

        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Options for serializing chat requests.
/// </summary>
public class ChatRequestSerializeOptions
{
    /// <summary>
    /// Whether the request is streamed.
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// Forces headers to be included. Warning: headers contain secrets, such as API keys.
    /// </summary>
    public bool IncludeHeaders { get; set;}
    
    /// <summary>
    /// Prettifies the request's body.
    /// </summary>
    public bool Pretty { get; set; }
}

/// <summary>
/// Options for serializing responses requests.
/// </summary>
public class ResponseRequestSerializeOptions
{
    /// <summary>
    /// Whether the request is streamed.
    /// </summary>
    public bool? Stream { get; set; }

    /// <summary>
    /// Forces headers to be included. Warning: headers contain secrets, such as API keys.
    /// </summary>
    public bool IncludeHeaders { get; set;}
    
    /// <summary>
    /// Prettifies the request's body.
    /// </summary>
    public bool Pretty { get; set; }
}

/// <summary>
/// Represents the counts of files in different processing states within a vector store.
/// </summary>
public class VectorStoreFileCountInfo
{
    /// <summary>
    /// The number of files that are currently being processed.
    /// </summary>
    [JsonProperty("in_progress")]
    public int InProgress { get; set; }

    /// <summary>
    /// The number of files that have been successfully processed.
    /// </summary>
    [JsonProperty("completed")]
    public int Completed { get; set; }

    /// <summary>
    /// The number of files that have failed to process.
    /// </summary>
    [JsonProperty("failed")]
    public int Failed { get; set; }

    /// <summary>
    /// The number of files that were cancelled.
    /// </summary>
    [JsonProperty("cancelled")]
    public int Cancelled { get; set; }

    /// <summary>
    /// The total number of files.
    /// </summary>
    [JsonProperty("total")]
    public int Total { get; set; }
}

/// <summary>
/// Tornado input file
/// </summary>
public class TornadoInputFile
{
    /// <summary>
    /// Base64 data
    /// </summary>
    public string? Base64 { get; set; }
    
    /// <summary>
    /// Mime type
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Creates an empty file
    /// </summary>
    public TornadoInputFile()
    {
        
    }

    /// <summary>
    /// Creates a file from base64 & mimetype
    /// </summary>
    public TornadoInputFile(string base64, string mimeType)
    {
        Base64 = base64;
        MimeType = mimeType;
    }
}

internal class TranscriptionSerializedRequest : IDisposable
{
    public readonly MultipartFormDataContent Content = new MultipartFormDataContent();
    public MemoryStream? Ms = null;
    public StreamContent? Sc = null;

    public void Dispose()
    {
        Content.Dispose();
        Ms?.Dispose();
        Sc?.Dispose();
    }
}

internal class AudioStreamEvent
{
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("delta")]
    public string? Delta { get; set; } 
    
    [JsonProperty("logprobs")]
    public List<TranscriptionLogprob>? Logprobs { get; set; }
    
    [JsonProperty("text")]
    public string? Text { get; set; }

    public static readonly Dictionary<string, AudioStreamEventTypes> Map = new Dictionary<string, AudioStreamEventTypes>(2)
    {
        { "transcript.text.delta", AudioStreamEventTypes.TranscriptDelta },
        { "transcript.text.done", AudioStreamEventTypes.TranscriptDone }
    };
}

internal enum AudioStreamEventTypes
{
    Unknown,
    TranscriptDelta,
    TranscriptDone
}

public static class ResponseOutputTypes
{
    public static string Reasoning => "reasoning";
}