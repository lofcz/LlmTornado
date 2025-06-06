using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Mistral;

/// <summary>
/// Premier embedding models from Mistral.
/// </summary>
public class EmbeddingModelMistralPremier : BaseVendorModelProvider
{
    /// <summary>
    /// Our state-of-the-art semantic for extracting representation of code extracts
    /// </summary>
    public static readonly EmbeddingModel ModelCodestralEmbed = new EmbeddingModel("codestral-embed", LLmProviders.Mistral, 8_192, 1_546, [ 1546, 1024, 512, 256 ]);

    /// <summary>
    /// <inheritdoc cref="ModelCodestralEmbed"/>
    /// </summary>
    public readonly EmbeddingModel CodestralEmbed = ModelCodestralEmbed;
    
    /// <summary>
    /// Our state-of-the-art semantic for extracting representation of text extracts
    /// </summary>
    public static readonly EmbeddingModel ModelMistralEmbed = new EmbeddingModel("mistral-embed", LLmProviders.Voyage, 32_000, 1_024);

    /// <summary>
    /// <inheritdoc cref="ModelMistralEmbed"/>
    /// </summary>
    public readonly EmbeddingModel MistralEmbed = ModelMistralEmbed;
    
    /// <summary>
    /// All known embedding models.
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
    /// All known Mistral Premier models.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelCodestralEmbed,
        ModelMistralEmbed,
    ];

    static EmbeddingModelMistralPremier()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal EmbeddingModelMistralPremier()
    {
        AllModels = ModelsAll;
    }
}