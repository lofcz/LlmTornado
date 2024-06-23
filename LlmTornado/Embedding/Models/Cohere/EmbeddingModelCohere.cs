using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Cohere;

/// <summary>
/// Known embedding models from Cohere.
/// </summary>
public class EmbeddingModelCohere : BaseVendorModelProvider
{
    /// <summary>
    /// Generation 2 models.
    /// </summary>
    public readonly EmbeddingModelCohereGen2 Gen2 = new EmbeddingModelCohereGen2();
    
    /// <summary>
    /// Generation 2 models.
    /// </summary>
    public readonly EmbeddingModelCohereGen3 Gen3 = new EmbeddingModelCohereGen3();
    
    /// <summary>
    /// All known embedding models from Cohere.
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
        ..EmbeddingModelCohereGen2.ModelsAll,
        ..EmbeddingModelCohereGen3.ModelsAll
    ];

    static EmbeddingModelCohere()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelCohere()
    {
        AllModels = ModelsAll;
    }
}