using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Known multimodal embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyageMultimodal : BaseVendorModelProvider
{
    /// <summary>
    /// Voyage Multimodal Gen 3 models.
    /// </summary>
    public readonly EmbeddingModelVoyageMultimodalGen3 Gen3 = new EmbeddingModelVoyageMultimodalGen3();
    
    /// <summary>
    /// All owned models.
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
    public static readonly HashSet<string> AllModelsMap;
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ..EmbeddingModelVoyageMultimodalGen3.ModelsAll
    ];
    
    static EmbeddingModelVoyageMultimodal()
    {
        AllModelsMap = new HashSet<string>(ModelsAll.Select(x => x.Name));
    }
    
    internal EmbeddingModelVoyageMultimodal()
    {
        
    }
}