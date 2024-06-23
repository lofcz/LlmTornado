using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Anthropic;

/// <summary>
/// Known embedding models from Anthropic.
/// </summary>
public class EmbeddingModelAnthropic : BaseVendorModelProvider
{
    /// <summary>
    /// Voyage 2 models.
    /// </summary>
    public readonly EmbeddingModelAnthropicVoyage2 Voyage2 = new EmbeddingModelAnthropicVoyage2();
    
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
        ..EmbeddingModelAnthropicVoyage2.ModelsAll
    ];
    
    static EmbeddingModelAnthropic()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelAnthropic()
    {
        AllModels = ModelsAll;
    }
}