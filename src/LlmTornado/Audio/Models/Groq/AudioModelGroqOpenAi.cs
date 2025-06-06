using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.Groq;

/// <summary>
/// Google models hosted by Groq.
/// </summary>
public class AudioModelGroqOpenAi : IVendorModelClassProvider
{
    /// <summary>
    /// A fine-tuned version of a pruned Whisper Large V3 designed for fast, multilingual transcription tasks.
    /// </summary>
    public static readonly AudioModel ModelWhisperV3Turbo = new AudioModel("groq-whisper-large-v3-turbo", LLmProviders.Groq)
    {
        ApiName = "whisper-large-v3-turbo"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelWhisperV3Turbo"/>
    /// </summary>
    public readonly AudioModel WhisperV3Turbo = ModelWhisperV3Turbo;
    
    /// <summary>
    /// A distilled, or compressed, version of OpenAI's Whisper model, designed to provide faster, lower cost English speech recognition while maintaining comparable accuracy.
    /// </summary>
    public static readonly AudioModel ModelWhisperV3Distill = new AudioModel("groq-distil-whisper-large-v3-en", LLmProviders.Groq)
    {
        ApiName = "distil-whisper-large-v3-en"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelWhisperV3Distill"/>
    /// </summary>
    public readonly AudioModel WhisperV3Distill = ModelWhisperV3Distill;
    
    /// <summary>
    /// Provides state-of-the-art performance with high accuracy for multilingual transcription and translation tasks.
    /// </summary>
    public static readonly AudioModel ModelWhisperV3 = new AudioModel("groq-whisper-large-v3", LLmProviders.Groq)
    {
        ApiName = "whisper-large-v3"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelWhisperV3"/>
    /// </summary>
    public readonly AudioModel WhisperV3 = ModelWhisperV3;
    
    /// <summary>
    /// All known Google models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelWhisperV3Turbo,
        ModelWhisperV3Distill,
        ModelWhisperV3
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal AudioModelGroqOpenAi()
    {

    }
}