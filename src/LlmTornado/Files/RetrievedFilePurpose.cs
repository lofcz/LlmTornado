using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Files;

/// <summary>
///     Represents the retrieved purpose of a file
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum RetrievedFilePurpose
{
    /// <summary>
    ///     Finetuning
    /// </summary>
    [EnumMember(Value = "fine-tune")]
    Finetune,

    /// <summary>
    ///     Finetuning results
    /// </summary>
    [EnumMember(Value = "fine-tune-results")]
    FinetuneResults,

    /// <summary>
    ///     Assistants input file
    /// </summary>
    [EnumMember(Value = "assistants")]
    Assistants,

    /// <summary>
    ///     Assistants output file
    /// </summary>
    [EnumMember(Value = "assistants_output")]
    AssistantsOutput,
    
    /// <summary>
    ///     User data.
    /// </summary>
    [EnumMember(Value = "user_data")]
    UserData,

    /// <summary>
    ///     Agent file for ZAI
    /// </summary>
    [EnumMember(Value = "agent")]
    Agent,
    
    /// <summary>
    ///     Batch API input/output file.
    /// </summary>
    [EnumMember(Value = "batch")]
    Batch,
    
    /// <summary>
    ///     Batch API output file.
    /// </summary>
    [EnumMember(Value = "batch_output")]
    BatchOutput,
    
    /// <summary>
    ///     OCR.
    /// </summary>
    [EnumMember(Value = "ocr")]
    Ocr,
    
    /// <summary>
    ///     Voice clone audio file. MiniMax only.
    /// </summary>
    [EnumMember(Value = "voice_clone")]
    VoiceClone,
    
    /// <summary>
    ///     Sample audio for voice cloning. MiniMax only.
    /// </summary>
    [EnumMember(Value = "prompt_audio")]
    PromptAudio,
    
    /// <summary>
    ///     Text file for async TTS. MiniMax only.
    /// </summary>
    [EnumMember(Value = "t2a_async_input")]
    TextToAudioAsyncInput,
    
    /// <summary>
    ///     Async TTS output. MiniMax only.
    /// </summary>
    [EnumMember(Value = "t2a_async")]
    TextToAudioAsync,
    
    /// <summary>
    ///     Video generation output. MiniMax only.
    /// </summary>
    [EnumMember(Value = "video_generation")]
    VideoGeneration
}

/// <summary>
///     Extension methods for RetrievedFilePurpose enum
/// </summary>
public static class RetrievedFilePurposeExtensions
{
    /// <summary>
    ///     Converts <see cref="FilePurpose" /> into <see cref="RetrievedFilePurpose" />
    /// </summary>
    /// <param name="purpose"></param>
    /// <returns></returns>
    public static RetrievedFilePurpose ToRetrievedFilePurpose(this FilePurpose purpose)
    {
        return purpose switch
        {
            FilePurpose.Assistants => RetrievedFilePurpose.Assistants,
            FilePurpose.Agent => RetrievedFilePurpose.Agent,
            FilePurpose.Batch => RetrievedFilePurpose.Batch,
            FilePurpose.Ocr => RetrievedFilePurpose.Ocr,
            FilePurpose.VoiceClone => RetrievedFilePurpose.VoiceClone,
            FilePurpose.PromptAudio => RetrievedFilePurpose.PromptAudio,
            FilePurpose.TextToAudioAsyncInput => RetrievedFilePurpose.TextToAudioAsyncInput,
            _ => RetrievedFilePurpose.Finetune
        };
    }
}