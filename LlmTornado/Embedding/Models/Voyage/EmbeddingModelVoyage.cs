using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Known embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyage : BaseVendorModelProvider
{
    /// <summary>
    /// Voyage 2 models.
    /// </summary>
    public readonly EmbeddingModelVoyageGen2 Gen2 = new EmbeddingModelVoyageGen2();
    
    /// <summary>
    /// All known embedding models from Antjropic.
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
        ..EmbeddingModelVoyageGen2.ModelsAll
    ];
    
    static EmbeddingModelVoyage()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelVoyage()
    {
        AllModels = ModelsAll;
    }
}