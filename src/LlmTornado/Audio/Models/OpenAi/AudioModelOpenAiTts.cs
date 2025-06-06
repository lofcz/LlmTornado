using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.OpenAi;

/// <summary>
/// Tts class models from OpenAI.
/// </summary>
public class AudioModelOpenAiTts : IVendorModelClassProvider
{
    /// <summary>
    /// Tts 1
    /// </summary>
    public static readonly AudioModel ModelTts1 = new AudioModel("tts-1", LLmProviders.OpenAi, 16_385);

    /// <summary>
    /// <inheritdoc cref="ModelTts1"/>
    /// </summary>
    public readonly AudioModel Tts1 = ModelTts1;
    
    /// <summary>
    /// Tts 1 HD
    /// </summary>
    public static readonly AudioModel ModelTts1Hd = new AudioModel("tts-1-hd", LLmProviders.OpenAi, 16_385);

    /// <summary>
    /// <inheritdoc cref="ModelTts1Hd"/>
    /// </summary>
    public readonly AudioModel Tts1Hd = ModelTts1Hd;

    /// <summary>
    /// All known Tts models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelTts1,
        ModelTts1Hd
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal AudioModelOpenAiTts()
    {
        
    }
}