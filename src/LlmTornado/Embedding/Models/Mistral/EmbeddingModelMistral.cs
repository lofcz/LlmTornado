using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Mistral;

/// <summary>
/// Known embedding models from Mistral.
/// </summary>
public class EmbeddingModelMistral : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Mistral;
    
    /// <summary>
    /// Premier models.
    /// </summary>
    public readonly EmbeddingModelMistralPremier Premier = new EmbeddingModelMistralPremier();
    
    /// <summary>
    /// All known embedding models from Mistral.
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
        ..EmbeddingModelMistralPremier.ModelsAll
    ];
    
    static EmbeddingModelMistral()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelMistral()
    {
        
    }
}