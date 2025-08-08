using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Voyage Multimodal Gen 3 embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyageMultimodalGen3 : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Voyage;
    
    /// <summary>
    /// A multimodal model that can embed text, images, or an interleaving of both modalities.
    /// </summary>
    public static readonly MultimodalEmbeddingModel ModelMultimodal3 = new MultimodalEmbeddingModel("voyage-multimodal-3", LLmProviders.Voyage, 32_000, 1024);

    /// <summary>
    /// <inheritdoc cref="ModelMultimodal3"/>
    /// </summary>
    public readonly MultimodalEmbeddingModel Multimodal3 = ModelMultimodal3;
    
    /// <summary>
    /// All owned models.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// Checks whether a model is owned.
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
    /// All known Voyage Multimodal Gen 3 models.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelMultimodal3
    ];

    static EmbeddingModelVoyageMultimodalGen3()
    {
        AllModelsMap = new HashSet<string>(ModelsAll.Select(x => x.Name));
    }
    
    internal EmbeddingModelVoyageMultimodalGen3()
    {
        
    }
}