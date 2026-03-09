using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Videos.Models.MiniMax;

/// <summary>
/// MiniMax Hailuo video generation models.
/// </summary>
public class VideoModelMiniMaxHailuo : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.MiniMax;

    public override List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// MiniMax-Hailuo-2.3 - Breakthroughs in body movement, facial expressions, physical realism, and prompt adherence.
    /// Supports text-to-video and image-to-video. Resolutions: 768P (default), 1080P. Duration: 6s or 10s (768P only).
    /// </summary>
    public static readonly VideoModel ModelHailuo23 = new VideoModel("MiniMax-Hailuo-2.3", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelHailuo23"/>
    /// </summary>
    public readonly VideoModel Hailuo23 = ModelHailuo23;

    /// <summary>
    /// MiniMax-Hailuo-2.3-Fast - Image-to-video model optimized for value and efficiency.
    /// Supports image-to-video only. Resolutions: 768P (default), 1080P. Duration: 6s or 10s (768P only).
    /// </summary>
    public static readonly VideoModel ModelHailuo23Fast = new VideoModel("MiniMax-Hailuo-2.3-Fast", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelHailuo23Fast"/>
    /// </summary>
    public readonly VideoModel Hailuo23Fast = ModelHailuo23Fast;

    /// <summary>
    /// MiniMax-Hailuo-02 - Supports higher resolution (1080P), longer duration (10s), and stronger adherence to prompts.
    /// Supports text-to-video and image-to-video. Resolutions: 512P, 768P (default), 1080P. Duration: 6s or 10s.
    /// </summary>
    public static readonly VideoModel ModelHailuo02 = new VideoModel("MiniMax-Hailuo-02", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelHailuo02"/>
    /// </summary>
    public readonly VideoModel Hailuo02 = ModelHailuo02;
    
    /// <summary>
    /// T2V-01-Director - Text-to-video model with camera movement control via [command] syntax.
    /// Resolution: 720P. Duration: 6s.
    /// </summary>
    public static readonly VideoModel ModelT2v01Director = new VideoModel("T2V-01-Director", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelT2v01Director"/>
    /// </summary>
    public readonly VideoModel T2v01Director = ModelT2v01Director;
    
    /// <summary>
    /// T2V-01 - Text-to-video model. Resolution: 720P. Duration: 6s.
    /// </summary>
    public static readonly VideoModel ModelT2v01 = new VideoModel("T2V-01", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelT2v01"/>
    /// </summary>
    public readonly VideoModel T2v01 = ModelT2v01;
    
    /// <summary>
    /// I2V-01-Director - Image-to-video model with camera movement control via [command] syntax.
    /// Resolution: 720P. Duration: 6s.
    /// </summary>
    public static readonly VideoModel ModelI2v01Director = new VideoModel("I2V-01-Director", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelI2v01Director"/>
    /// </summary>
    public readonly VideoModel I2v01Director = ModelI2v01Director;
    
    /// <summary>
    /// I2V-01-live - Image-to-video model for live-style video generation.
    /// Resolution: 720P. Duration: 6s.
    /// </summary>
    public static readonly VideoModel ModelI2v01Live = new VideoModel("I2V-01-live", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelI2v01Live"/>
    /// </summary>
    public readonly VideoModel I2v01Live = ModelI2v01Live;
    
    /// <summary>
    /// I2V-01 - Image-to-video model. Resolution: 720P. Duration: 6s.
    /// </summary>
    public static readonly VideoModel ModelI2v01 = new VideoModel("I2V-01", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelI2v01"/>
    /// </summary>
    public readonly VideoModel I2v01 = ModelI2v01;

    /// <summary>
    /// All known Hailuo video models from MiniMax.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelHailuo23, ModelHailuo23Fast, ModelHailuo02,
        ModelT2v01Director, ModelT2v01,
        ModelI2v01Director, ModelI2v01Live, ModelI2v01
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
    
    internal VideoModelMiniMaxHailuo()
    {
        
    }
}
