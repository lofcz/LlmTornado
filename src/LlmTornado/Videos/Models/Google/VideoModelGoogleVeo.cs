using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Videos.Models.Google;

/// <summary>
/// Google Veo video models.
/// </summary>
public class VideoModelGoogleVeo : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Google;

    public override List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// Veo 3.1 Preview model.
    /// </summary>
    public static readonly VideoModel ModelV31 = new VideoModel("veo-3.1-generate-preview", "google", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelV31"/>
    /// </summary>
    public readonly VideoModel V31 = ModelV31;
    
    /// <summary>
    /// Veo 3.1 Fast Preview model.
    /// </summary>
    public static readonly VideoModel ModelV31Fast = new VideoModel("veo-3.1-fast-generate-preview", "google", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelV31Fast"/>
    /// </summary>
    public readonly VideoModel V31Fast = ModelV31Fast;
    
    /// <summary>
    /// Veo 3.0 model.
    /// </summary>
    public static readonly VideoModel ModelV3 = new VideoModel("veo-3.0-generate-001", "google", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelV3"/>
    /// </summary>
    public readonly VideoModel V3 = ModelV3;
    
    /// <summary>
    /// Veo 3.0 Fast model.
    /// </summary>
    public static readonly VideoModel ModelV3Fast = new VideoModel("veo-3.0-fast-generate-001", "google", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelV3Fast"/>
    /// </summary>
    public readonly VideoModel V3Fast = ModelV3Fast;
    
    /// <summary>
    /// Veo 2.0 model.
    /// </summary>
    public static readonly VideoModel ModelV2 = new VideoModel("veo-2.0-generate-001", "google", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelV2"/>
    /// </summary>
    public readonly VideoModel V2 = ModelV2;
    
    /// <summary>
    /// All known Veo models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV31, ModelV31Fast, ModelV3, ModelV3Fast, ModelV2
    ]);
    
    /// <summary>
    /// Checks whether a model is owned by the provider.
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
    
    internal VideoModelGoogleVeo()
    {
        
    }
}