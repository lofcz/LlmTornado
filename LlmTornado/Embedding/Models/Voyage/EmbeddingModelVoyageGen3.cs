using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Voyage 3 embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyageGen3 : BaseVendorModelProvider
{
    /// <summary>
    /// The best general-purpose and multilingual retrieval quality.
    /// </summary>
    public static readonly EmbeddingModel ModelLarge = new EmbeddingModel("voyage-3-large", LLmProviders.Voyage, 32_000, 1_024, [ 2048, 1042, 512, 256 ]);

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly EmbeddingModel Large = ModelLarge;
    
    /// <summary>
    /// Optimized for code retrieval.
    /// </summary>
    public static readonly EmbeddingModel ModelCode = new EmbeddingModel("voyage-code-3", LLmProviders.Voyage, 32_000, 1_024, [ 2048, 1042, 512, 256 ]);

    /// <summary>
    /// <inheritdoc cref="ModelCode"/>
    /// </summary>
    public readonly EmbeddingModel Code = ModelCode;
    
    /// <summary>
    /// All known embedding models.
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
    /// All known Voyage 3 models.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelLarge,
        ModelCode,
    ];

    static EmbeddingModelVoyageGen3()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelVoyageGen3()
    {
        AllModels = ModelsAll;
    }
}