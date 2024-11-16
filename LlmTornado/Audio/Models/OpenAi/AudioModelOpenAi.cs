using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.OpenAi;

/// <summary>
/// Known chat models from OpenAI.
/// </summary>
public class AudioModelOpenAi : BaseVendorModelProvider
{
    /// <summary>
    /// Whisper models.
    /// </summary>
    public readonly AudioModelOpenAiWhisper Whisper = new AudioModelOpenAiWhisper();

    /// <summary>
    /// All known chat models from OpenAI.
    /// </summary>
    public override List<IModel> AllModels { get; }
    
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
        ..AudioModelOpenAiWhisper.ModelsAll
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
        AllModels = ModelsAll;
    }
}