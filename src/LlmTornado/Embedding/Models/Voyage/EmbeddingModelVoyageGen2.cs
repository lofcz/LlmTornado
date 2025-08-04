using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding.Models.Voyage;

/// <summary>
/// Voyage 2 embedding models from Voyage.
/// </summary>
public class EmbeddingModelVoyageGen2 : BaseVendorModelProvider
{
    /// <summary>
    /// Voyage AI’s most powerful generalist embedding model.
    /// </summary>
    public static readonly EmbeddingModel ModelLarge = new EmbeddingModel("voyage-large-2", LLmProviders.Voyage, 4_096, 1_536);

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly EmbeddingModel Large = ModelLarge;
    
    /// <summary>
    /// Optimized for code retrieval (17% better than alternatives), and also SoTA on general-purpose corpora.
    /// </summary>
    public static readonly EmbeddingModel ModelCode = new EmbeddingModel("voyage-code-2", LLmProviders.Voyage, 4_096, 1_536);

    /// <summary>
    /// <inheritdoc cref="ModelCode"/>
    /// </summary>
    public readonly EmbeddingModel Code = ModelCode;
    
    /// <summary>
    /// Base generalist embedding model optimized for both latency and quality.
    /// </summary>
    public static readonly EmbeddingModel ModelDefault = new EmbeddingModel("voyage-2", LLmProviders.Voyage, 4_096, 1_024);

    /// <summary>
    /// <inheritdoc cref="ModelCode"/>
    /// </summary>
    public readonly EmbeddingModel Default = ModelDefault;
    
    /// <summary>
    /// 	Instruction-tuned for classification, clustering, and sentence textual similarity tasks, which are the only recommended use cases for this model.
    /// </summary>
    public static readonly EmbeddingModel ModelLiteInstruct = new EmbeddingModel("voyage-lite-02-instruct", LLmProviders.Voyage, 4_096, 1_024);

    /// <summary>
    /// <inheritdoc cref="ModelLiteInstruct"/>
    /// </summary>
    public readonly EmbeddingModel LiteInstruct = ModelLiteInstruct;
    
    /// <summary>
    /// All known embedding models from Voyage 2.
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
    /// All known Voyage 2 models from Anthropic.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelLarge,
        ModelCode,
        ModelDefault,
        ModelLiteInstruct
    ];

    static EmbeddingModelVoyageGen2()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelVoyageGen2()
    {
        
    }
}