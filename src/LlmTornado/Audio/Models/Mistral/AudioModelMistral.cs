using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.Mistral;

/// <summary>
/// Known audio models from Mistral.
/// </summary>
public class AudioModelMistral : BaseVendorModelProvider
{
    /// <summary>
    /// Free models.
    /// </summary>
    public readonly AudioModelMistralFree Free = new AudioModelMistralFree();

    /// <summary>
    /// All known audio models from Mistral.
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
        ..AudioModelMistralFree.ModelsAll
    ];
    
    static AudioModelMistral()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal AudioModelMistral()
    {
        
    }
}