using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Voyage Contextual Gen 3 embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyageContextualGen3 : BaseVendorModelProvider
{
    /// <summary>
    /// A novel contextualized chunk embedding model, where chunk embedding encodes not only the chunk’s own content, but also captures the contextual information from the full document.
    /// </summary>
    public static readonly ContextualEmbeddingModel ModelContext3 = new ContextualEmbeddingModel("voyage-context-3", LLmProviders.Voyage, 32_000, 1024, [ 256, 512, 1024, 2048 ]);

    /// <summary>
    /// <inheritdoc cref="ModelContext3"/>
    /// </summary>
    public readonly ContextualEmbeddingModel Context3 = ModelContext3;
    
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
    public static readonly HashSet<string> AllModelsMap;
    
    /// <summary>
    /// All known Voyage Contextual Gen 3 models.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelContext3
    ];

    static EmbeddingModelVoyageContextualGen3()
    {
        AllModelsMap = new HashSet<string>(ModelsAll.Select(x => x.Name));
    }
    
    internal EmbeddingModelVoyageContextualGen3()
    {
       
    }
}