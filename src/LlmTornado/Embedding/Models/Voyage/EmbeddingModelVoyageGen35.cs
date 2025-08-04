using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Voyage 3.5 embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyageGen35 : BaseVendorModelProvider
{
    /// <summary>
    /// Optimized for general-purpose and multilingual retrieval quality.
    /// </summary>
    public static readonly EmbeddingModel ModelDefault = new EmbeddingModel("voyage-3.5", LLmProviders.Voyage, 32_000, 1_024, [ 2048, 1042, 512, 256 ]);

    /// <summary>
    /// <inheritdoc cref="ModelDefault"/>
    /// </summary>
    public readonly EmbeddingModel Default = ModelDefault;
    
    /// <summary>
    /// Optimized for latency and cost.
    /// </summary>
    public static readonly EmbeddingModel ModelLite = new EmbeddingModel("voyage-3.5-lite", LLmProviders.Voyage, 32_000, 1_024, [ 2048, 1042, 512, 256 ]);

    /// <summary>
    /// <inheritdoc cref="ModelLite"/>
    /// </summary>
    public readonly EmbeddingModel Lite = ModelLite;
    
    /// <summary>
    /// All known embedding models.
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
    /// All known Voyage 3.5 models.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelDefault,
        ModelLite,
    ];

    static EmbeddingModelVoyageGen35()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelVoyageGen35()
    {
        
    }
}