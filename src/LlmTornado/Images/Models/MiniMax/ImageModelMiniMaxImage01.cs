using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.MiniMax;

/// <summary>
/// MiniMax Image-01 class models.
/// </summary>
public class ImageModelMiniMaxImage01 : IVendorModelClassProvider
{
    /// <summary>
    /// image-01 - MiniMax's image generation model. Supports text-to-image and image-to-image with subject reference.
    /// Aspect ratios: 1:1, 16:9, 4:3, 3:2, 2:3, 3:4, 9:16, 21:9. Custom width/height in [512, 2048] divisible by 8.
    /// Supports up to 9 images per request.
    /// </summary>
    public static readonly ImageModel ModelImage01 = new ImageModel("image-01", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelImage01"/>
    /// </summary>
    public readonly ImageModel V1 = ModelImage01;
    
    /// <summary>
    /// image-01-live - MiniMax's image generation model optimized for live/portrait generation with subject reference.
    /// Supports image-to-image with character references.
    /// </summary>
    public static readonly ImageModel ModelImage01Live = new ImageModel("image-01-live", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelImage01Live"/>
    /// </summary>
    public readonly ImageModel Live = ModelImage01Live;
    
    /// <summary>
    /// All known Image-01 models from MiniMax.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;
    
    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelImage01,
        ModelImage01Live
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelMiniMaxImage01()
    {
        
    }
}
