using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.OpenAi;

/// <summary>
/// Known embedding models from OpenAI.
/// </summary>
public class EmbeddingModelOpenAi : BaseVendorModelProvider
{
    /// <summary>
    /// Generation 2 models (Ada).
    /// </summary>
    public readonly EmbeddingModelOpenAiGen2 Gen2 = new EmbeddingModelOpenAiGen2();

    /// <summary>
    /// Generation 3 models.
    /// </summary>
    public readonly EmbeddingModelOpenAiGen3 Gen3 = new EmbeddingModelOpenAiGen3();

    /// <summary>
    /// All known embedding models from OpenAI.
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
        ..EmbeddingModelOpenAiGen2.ModelsAll,
        ..EmbeddingModelOpenAiGen3.ModelsAll
    ];
    
    static EmbeddingModelOpenAi()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }

    internal EmbeddingModelOpenAi()
    {
        AllModels = ModelsAll;
    }
}