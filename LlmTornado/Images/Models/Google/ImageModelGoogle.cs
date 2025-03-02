using System.Collections.Generic;
using LlmTornado.Code.Models;
using LlmTornado.Images.Models.OpenAi;

namespace LlmTornado.Images.Models.Google;

/// <summary>
/// Known image models from Google.
/// </summary>
public class ImageModelGoogle : BaseVendorModelProvider
{
    /// <summary>
    /// Dalle models.
    /// </summary>
    public readonly ImageModelGoogleImagen Imagen = new ImageModelGoogleImagen();
    
    /// <summary>
    /// All known image models from OpenAI.
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
        ..ImageModelGoogleImagen.ModelsAll
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
        AllModels = ModelsAll;
    }
}