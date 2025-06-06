using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.OpenAi;

/// <summary>
/// Gpt4 class models from OpenAI.
/// </summary>
public class AudioModelOpenAiGpt4 : IVendorModelClassProvider
{
    /// <summary>
    /// gpt-4o-mini-tts
    /// </summary>
    public static readonly AudioModel Model4OMiniTts = new AudioModel("gpt-4o-mini-tts", LLmProviders.OpenAi, 16_385);

    /// <summary>
    /// <inheritdoc cref="Model4OMiniTts"/>
    /// </summary>
    public readonly AudioModel Gpt4OMiniTts = Model4OMiniTts;
    
    /// <summary>
    /// gpt-4o-transcribe
    /// </summary>
    public static readonly AudioModel Model4OTranscribe = new AudioModel("gpt-4o-transcribe", LLmProviders.OpenAi, 16_385);

    /// <summary>
    /// <inheritdoc cref="Model4OTranscribe"/>
    /// </summary>
    public readonly AudioModel Gpt4OTranscribe = Model4OTranscribe;
    
    /// <summary>
    /// gpt-4o-mini-transcribe
    /// </summary>
    public static readonly AudioModel Model4OMiniTranscribe = new AudioModel("gpt-4o-mini-transcribe", LLmProviders.OpenAi, 16_385);

    /// <summary>
    /// <inheritdoc cref="Model4OMiniTranscribe"/>
    /// </summary>
    public readonly AudioModel Gpt4OMiniTranscribe = Model4OMiniTranscribe;
    
    /// <summary>
    /// All known Gpt4 models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        Model4OMiniTts,
        Model4OTranscribe,
        Model4OMiniTranscribe
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal AudioModelOpenAiGpt4()
    {
        
    }
}