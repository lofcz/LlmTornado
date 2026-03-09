using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Files;

/// <summary>
///     Represents the file purpose, either the file is for fine-tuning and needs to be in JSONL format or for messages &
///     assistants.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum FilePurpose
{
    /// <summary>
    ///     Expects JSONL content
    /// </summary>
    [EnumMember(Value = "finetune")]
    Finetune,

    /// <summary>
    ///     Supported content: https://platform.openai.com/docs/assistants/tools/supported-files
    /// </summary>
    [EnumMember(Value = "assistants")]
    Assistants,

    /// <summary>
    ///     Agent purpose for ZAI file uploads.
    /// </summary>
    [EnumMember(Value = "agent")]
    Agent,
    
    /// <summary>
    ///     Used in the Batch API.
    /// </summary>
    [EnumMember(Value = "batch")]
    Batch,
    
    /// <summary>
    ///     Images used for vision fine-tuning.
    /// </summary>
    [EnumMember(Value = "vision")]
    Vision,
    
    /// <summary>
    ///     Flexible file type for any purpose.
    /// </summary>
    [EnumMember(Value = "user_data")]
    UserData,
    
    /// <summary>
    ///     Flexible file type for any purpose.
    /// </summary>
    [EnumMember(Value = "evals")]
    Evals,
    
    /// <summary>
    ///     OCR.
    /// </summary>
    [EnumMember(Value = "ocr")]
    Ocr,
    
    /// <summary>
    ///     Voice clone audio file. MiniMax only. Supports mp3, m4a, wav.
    /// </summary>
    [EnumMember(Value = "voice_clone")]
    VoiceClone,
    
    /// <summary>
    ///     Sample audio for voice cloning. MiniMax only. Supports mp3, m4a, wav.
    /// </summary>
    [EnumMember(Value = "prompt_audio")]
    PromptAudio,
    
    /// <summary>
    ///     Text file for asynchronous long-text-to-speech synthesis. MiniMax only.
    /// </summary>
    [EnumMember(Value = "t2a_async_input")]
    TextToAudioAsyncInput
}