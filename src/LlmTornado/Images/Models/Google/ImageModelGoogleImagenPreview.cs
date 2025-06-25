using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.Google;

/// <summary>
/// Imagen Preview class models from Google.
/// </summary>
public class ImageModelGoogleImagenPreview : IVendorModelClassProvider
{
    /// <summary>
    /// Imagen 4: Support Generation with 1:1, 9:16, 16:9, 3:4, 4:3 aspect ratios and watermarking (SynthID).
    /// </summary>
    public static readonly ImageModel ModelV4Preview250606 = new ImageModel("imagen-4.0-generate-preview-06-06", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV4Preview250606"/>
    /// </summary>
    public readonly ImageModel V4Preview250606 = ModelV4Preview250606;
    
    /// <summary>
    /// Imagen 4 Ultra: Support Generation with 1:1, 9:16, 16:9, 3:4, 4:3 aspect ratios and watermarking (SynthID).
    /// </summary>
    public static readonly ImageModel ModelV4UltraPreview250606 = new ImageModel("imagen-4.0-ultra-generate-preview-06-06", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV4UltraPreview250606"/>
    /// </summary>
    public readonly ImageModel V4UltraPreview250606 = ModelV4UltraPreview250606;
    
    /// <summary>
    /// Imagen 4 Fast: Support Generation with 1:1, 9:16, 16:9, 3:4, 4:3 aspect ratios and watermarking (SynthID).
    /// </summary>
    public static readonly ImageModel ModelV4FastPreview250606 = new ImageModel("imagen-4.0-fast-generate-preview-06-06", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV4FastPreview250606"/>
    /// </summary>
    public readonly ImageModel V4FastPreview250606 = ModelV4FastPreview250606;
    
    /// <summary>
    /// All known Imagen Preview models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelV4Preview250606,
        ModelV4UltraPreview250606,
        ModelV4FastPreview250606
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelGoogleImagenPreview()
    {
        
    }
}