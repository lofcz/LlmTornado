using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.DeepInfra;

/// <summary>
/// Known image models from DeepInfra.
/// </summary>
public class ImageModelDeepInfra : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.DeepInfra;
    
    /// <summary>
    /// Flux models.
    /// </summary>
    public readonly ImageModelDeepInfraFlux Flux = new ImageModelDeepInfraFlux();
    
    /// <summary>
    /// All known image models from DeepInfra.
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
        ..ImageModelDeepInfraFlux.ModelsAll
    ];
    
    static ImageModelDeepInfra()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ImageModelDeepInfra()
    {
        
    }
}

