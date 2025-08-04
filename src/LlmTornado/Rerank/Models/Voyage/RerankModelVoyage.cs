using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code.Models;

namespace LlmTornado.Rerank.Models.Voyage;

/// <summary>
/// Known rerank models from Voyage.
/// </summary>
public class RerankModelVoyage : BaseVendorModelProvider
{
    /// <summary>
    /// Voyage Rerank Gen 2.5 models.
    /// </summary>
    public readonly RerankModelVoyageGen25 Gen25 = new();
    
    /// <summary>
    /// All known rerank models from Voyage.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;
    
    /// <summary>
    /// Checks whether the model is owned by the provider.
    /// </summary>
    public override bool OwnsModel(string model)
    {
        return AllModelsMap.Contains(model);
    }

    /// <summary>
    /// Map of models owned by the provider.
    /// </summary>
    public static readonly HashSet<string> AllModelsMap;
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ..RerankModelVoyageGen25.ModelsAll
    ];
    
    static RerankModelVoyage()
    {
        AllModelsMap = new HashSet<string>(ModelsAll.Select(x => x.Name));
    }
    
    internal RerankModelVoyage()
    {
        
    }
}