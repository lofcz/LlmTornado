using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models.OpenAi;

namespace LlmTornado.Embedding.Models.Cohere;

/// <summary>
/// Generation 2 embedding models from Cohere.
/// </summary>
public class EmbeddingModelCohereGen2 : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Cohere;
    
    /// <summary>
    /// Older embeddings model that allows for text to be classified or turned into embeddings. English only.
    /// </summary>
    public static readonly EmbeddingModel ModelEnglish = new EmbeddingModel("embed-english-v2.0", LLmProviders.Cohere, 512, 4_096);

    /// <summary>
    /// <inheritdoc cref="ModelEnglish"/>
    /// </summary>
    public readonly EmbeddingModel English = ModelEnglish;
    
    /// <summary>
    /// A smaller, faster version of embed-english-v2.0. Almost as capable, but a lot faster. English only.
    /// </summary>
    public static readonly EmbeddingModel ModelEnglishLight = new EmbeddingModel("embed-english-light-v2.0", LLmProviders.Cohere, 512, 1_024);

    /// <summary>
    /// <inheritdoc cref="ModelEnglishLight"/>
    /// </summary>
    public readonly EmbeddingModel EnglishLight = ModelEnglishLight;
    
    /// <summary>
    /// Provides multilingual classification and embedding support. Supported languages: https://docs.cohere.com/docs/supported-languages
    /// </summary>
    public static readonly EmbeddingModel ModelMultilingual = new EmbeddingModel("embed-multilingual-v2.0", LLmProviders.Cohere, 256, 768);

    /// <summary>
    /// <inheritdoc cref="ModelMultilingual"/>
    /// </summary>
    public readonly EmbeddingModel Multilingual = ModelMultilingual;
    
    /// <summary>
    /// All known embedding models from Cohere Gen 2.
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
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelEnglish,
        ModelEnglishLight,
        ModelMultilingual
    ];
    
    static EmbeddingModelCohereGen2()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelCohereGen2()
    {
        
    }
}