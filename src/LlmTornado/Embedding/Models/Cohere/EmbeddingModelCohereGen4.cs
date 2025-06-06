using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models.OpenAi;

namespace LlmTornado.Embedding.Models.Cohere;

/// <summary>
/// Generation 4 embedding models from Cohere.
/// </summary>
public class EmbeddingModelCohereGen4 : BaseVendorModelProvider
{
    /// <summary>
    /// A model that allows for text and images to be classified or turned into embeddings.
    /// </summary>
    public static readonly EmbeddingModel ModelV4 = new EmbeddingModel("embed-v4.0", LLmProviders.Cohere, 128_000, 1_536, [ 256, 512, 1024, 1536 ]);

    /// <summary>
    /// <inheritdoc cref="ModelV4"/>
    /// </summary>
    public readonly EmbeddingModel V4 = ModelV4;

    /// <summary>
    /// All known embedding models from Cohere Gen 4.
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
        ModelV4
    ];
    
    static EmbeddingModelCohereGen4()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelCohereGen4()
    {
        AllModels = ModelsAll;
    }
}