using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.MiniMax;

/// <summary>
/// Known audio models from MiniMax.
/// </summary>
public class AudioModelMiniMax : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.MiniMax;
    
    /// <summary>
    /// Music generation models.
    /// </summary>
    public readonly AudioModelMiniMaxMusic Music = new AudioModelMiniMaxMusic();

    /// <summary>
    /// All known audio models from MiniMax.
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
    public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;

    private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
    {
        HashSet<string> map = [];
        ModelsAll.ForEach(x => { map.Add(x.Name); });
        return map;
    });
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ..AudioModelMiniMaxMusic.ModelsAll
    ]);
    
    internal AudioModelMiniMax()
    {
        
    }
}
