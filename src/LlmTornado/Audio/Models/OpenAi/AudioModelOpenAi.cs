using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.OpenAi;

/// <summary>
/// Known audio models from OpenAI.
/// </summary>
public class AudioModelOpenAi : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.OpenAi;
    
    /// <summary>
    /// Whisper models.
    /// </summary>
    public readonly AudioModelOpenAiWhisper Whisper = new AudioModelOpenAiWhisper();
    
    /// <summary>
    /// Tts models.
    /// </summary>
    public readonly AudioModelOpenAiTts Tts = new AudioModelOpenAiTts();
    
    /// <summary>
    /// Gpt4o models.
    /// </summary>
    public readonly AudioModelOpenAiGpt4 Gpt4 = new AudioModelOpenAiGpt4();

    /// <summary>
    /// All known chat models from OpenAI.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;
    
    /// <summary>
    /// Checks whether the model is owned by the provider.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public override bool OwnsModel(string model)
    {
        return AllModelsMap.Contains(model);
    }

    /// <summary>
    /// Map of models owned by the provider.
    /// </summary>
    public static readonly HashSet<string> AllModelsMap = [];
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ..AudioModelOpenAiWhisper.ModelsAll,
        ..AudioModelOpenAiTts.ModelsAll,
        ..AudioModelOpenAiGpt4.ModelsAll
    ];

    /// <summary>
    /// Models supporting "verbose_json" output & "timestamp_granularities"
    /// </summary>
    public static readonly List<IModel> VerboseJsonCompatibleModels = [
        ..AudioModelOpenAiWhisper.ModelsAll
    ];
    
    /// <summary>
    /// Models supporting streaming.
    /// </summary>
    public static readonly List<IModel> StreamingCompatibleModels = [
        ..AudioModelOpenAiTts.ModelsAll,
        ..AudioModelOpenAiGpt4.ModelsAll
    ];
    
    /// <summary>
    /// Models supporting "include".
    /// </summary>
    public static readonly List<IModel> IncludeCompatibleModels = [
        ..AudioModelOpenAiGpt4.ModelsAll
    ];
    
    static AudioModelOpenAi()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal AudioModelOpenAi()
    {
        
    }
}