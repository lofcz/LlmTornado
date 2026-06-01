using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Google;

/// <summary>
/// Google multimodal embedding models.
/// </summary>
public class EmbeddingModelGoogleMultimodal : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Google;

    /// <summary>
    /// Maps text, images, video, audio, and PDFs into a unified embedding space.
    /// </summary>
    public static readonly MultimodalEmbeddingModel ModelGeminiEmbedding2 = new MultimodalEmbeddingModel("gemini-embedding-2", LLmProviders.Google, 8_192, 3072);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiEmbedding2"/>
    /// </summary>
    public readonly MultimodalEmbeddingModel GeminiEmbedding2 = ModelGeminiEmbedding2;

    /// <summary>
    /// Preview of the natively multimodal embedding model.
    /// </summary>
    public static readonly MultimodalEmbeddingModel ModelGeminiEmbedding2Preview = new MultimodalEmbeddingModel("gemini-embedding-2-preview", LLmProviders.Google, 8_192, 3072);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiEmbedding2Preview"/>
    /// </summary>
    public readonly MultimodalEmbeddingModel GeminiEmbedding2Preview = ModelGeminiEmbedding2Preview;

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
    public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;

    private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
    {
        HashSet<string> map = [];
        ModelsAll.ForEach(x => { map.Add(x.Name); });
        return map;
    });

    /// <summary>
    /// All known Google multimodal embedding models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelGeminiEmbedding2,
        ModelGeminiEmbedding2Preview
    ]);

    internal EmbeddingModelGoogleMultimodal()
    {

    }
}
