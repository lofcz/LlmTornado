using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba embedding models - multilingual text embedding models for semantic analysis.
/// </summary>
public class ChatModelAlibabaEmbeddings : IVendorModelClassProvider
{
    /// <summary>
    /// Text-Embedding-v4 - General Text Vector V4 version with improved performance
    /// </summary>
    public static readonly ChatModel ModelTextEmbeddingV4 = new ChatModel("text-embedding-v4", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelTextEmbeddingV4"/>
    /// </summary>
    public readonly ChatModel TextEmbeddingV4 = ModelTextEmbeddingV4;

    /// <summary>
    /// Text-Embedding-v3 - General text vectorization model
    /// </summary>
    public static readonly ChatModel ModelTextEmbeddingV3 = new ChatModel("text-embedding-v3", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelTextEmbeddingV3"/>
    /// </summary>
    public readonly ChatModel TextEmbeddingV3 = ModelTextEmbeddingV3;

    /// <summary>
    /// All known embedding models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelTextEmbeddingV4, ModelTextEmbeddingV3
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAlibabaEmbeddings()
    {
    }
}
