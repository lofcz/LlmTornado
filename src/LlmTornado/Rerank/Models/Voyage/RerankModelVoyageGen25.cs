using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Rerank.Models.Voyage;

/// <summary>
/// Voyage Rerank Gen 2.5 models.
/// </summary>
public class RerankModelVoyageGen25 : BaseVendorModelProvider
{
    /// <summary>
    /// Rerank 2.5 model.
    /// </summary>
    public static readonly RerankModel ModelRerank25 = new RerankModel("rerank-2.5", LLmProviders.Voyage);

    /// <summary>
    /// <inheritdoc cref="ModelRerank25"/>
    /// </summary>
    public readonly RerankModel Rerank25 = ModelRerank25;
    
    /// <summary>
    /// Rerank 2.5 lite model.
    /// </summary>
    public static readonly RerankModel ModelRerank25Lite = new RerankModel("rerank-2.5-lite", LLmProviders.Voyage);
    
    /// <summary>
    /// <inheritdoc cref="ModelRerank25Lite"/>
    /// </summary>
    public readonly RerankModel Rerank25Lite = ModelRerank25Lite;
    
    /// <summary>
    /// All known embedding models.
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
    /// All known Voyage Rerank Gen 2.5 models.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelRerank25,
        ModelRerank25Lite
    ];

    static RerankModelVoyageGen25()
    {
        AllModelsMap = new HashSet<string>(ModelsAll.Select(x => x.Name));
    }
    
    internal RerankModelVoyageGen25()
    {
        
    }
}