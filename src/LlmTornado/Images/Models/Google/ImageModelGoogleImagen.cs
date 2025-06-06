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
        ModelV3Generate002
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelGoogleImagen()
    {
        
    }
}