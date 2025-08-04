using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.Google;

/// <summary>
/// Known image models from Google.
/// </summary>
public class ImageModelGoogle : BaseVendorModelProvider
{
    /// <summary>
    /// Imagen models.
    /// </summary>
    public readonly ImageModelGoogleImagen Imagen = new ImageModelGoogleImagen();
    
    /// <summary>
    /// Imagen preview models.
    /// </summary>
    public readonly ImageModelGoogleImagenPreview ImagenPreview = new ImageModelGoogleImagenPreview();
    
    /// <summary>
    /// All known image models from OpenAI.
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
        ..ImageModelGoogleImagen.ModelsAll,
        ..ImageModelGoogleImagenPreview.ModelsAll
    ];
    
    static ImageModelGoogle()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ImageModelGoogle()
    {
        
    }
}