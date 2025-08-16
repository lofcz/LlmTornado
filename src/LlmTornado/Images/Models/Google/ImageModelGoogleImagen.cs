using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.Google;

/// <summary>
/// Imagen class models from Google.
/// </summary>
public class ImageModelGoogleImagen : IVendorModelClassProvider
{
    /// <summary>
    /// imagen-4.0-fast-generate-001
    /// </summary>
    public static readonly ImageModel ModelV4FastGenerate001 = new ImageModel("imagen-4.0-fast-generate-001", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV4FastGenerate001"/>
    /// </summary>
    public readonly ImageModel V4FastGenerate001 = ModelV4FastGenerate001;
    
    /// <summary>
    /// imagen-4.0-ultra-generate-001
    /// </summary>
    public static readonly ImageModel ModelV4UltraGenerate001 = new ImageModel("imagen-4.0-ultra-generate-001", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV4UltraGenerate001"/>
    /// </summary>
    public readonly ImageModel V4UltraGenerate001 = ModelV4UltraGenerate001;
    
    /// <summary>
    /// imagen-4.0-generate-001
    /// </summary>
    public static readonly ImageModel ModelV4Generate001 = new ImageModel("imagen-4.0-generate-001", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV4Generate001"/>
    /// </summary>
    public readonly ImageModel V4Generate001 = ModelV4Generate001;
    
    /// <summary>
    /// The imagen-3.0-generate-002 model is Google's highest quality text-to-image model, featuring a number of new and improved capabilities.
    /// </summary>
    public static readonly ImageModel ModelV3Generate002 = new ImageModel("imagen-3.0-generate-002", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelV3Generate002"/>
    /// </summary>
    public readonly ImageModel V3Generate002 = ModelV3Generate002;
    
    /// <summary>
    /// All known Imagen models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelV3Generate002,
        ModelV4FastGenerate001,
        ModelV4UltraGenerate001,
        ModelV4Generate001
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelGoogleImagen()
    {
        
    }
}