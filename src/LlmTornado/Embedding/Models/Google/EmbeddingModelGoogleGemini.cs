using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding.Models.Google;

/// <summary>
/// Gemini embedding models from Google.
/// </summary>
public class EmbeddingModelGoogleGemini : IVendorModelClassProvider
{
    /// <summary>
    /// State-of-the-art performance across English, multilingual and code tasks. It unifies the previously specialized models like text-embedding-005 and text-multilingual-embedding-002 and achieves better performance in their respective domains.
    /// </summary>
    public static readonly EmbeddingModel ModelGeminiEmbedding001 = new EmbeddingModel("gemini-embedding-001", LLmProviders.Google, 2_048, 3072, [ 1536, 768 ]);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiEmbedding001"/>
    /// </summary>
    public readonly EmbeddingModel GeminiEmbedding001 = ModelGeminiEmbedding001;

    /// <summary>
    /// First natively multimodal embedding model. Maps text, images, video, audio, and PDFs into a unified embedding space.
    /// Use with File Search stores via <see cref="FileSearchMultimodalEmbeddingModelResource"/> for multimodal RAG.
    /// </summary>
    public static readonly EmbeddingModel ModelGeminiEmbedding2 = new EmbeddingModel("gemini-embedding-2", LLmProviders.Google, 8_192, 3072, [ 3072, 1536, 768, 512, 256, 128 ]);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiEmbedding2"/>
    /// </summary>
    public readonly EmbeddingModel GeminiEmbedding2 = ModelGeminiEmbedding2;

    /// <summary>
    /// Preview of the natively multimodal embedding model. Same modalities and limits as <see cref="ModelGeminiEmbedding2"/>.
    /// </summary>
    public static readonly EmbeddingModel ModelGeminiEmbedding2Preview = new EmbeddingModel("gemini-embedding-2-preview", LLmProviders.Google, 8_192, 3072, [ 3072, 1536, 768, 512, 256, 128 ]);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiEmbedding2Preview"/>
    /// </summary>
    public readonly EmbeddingModel GeminiEmbedding2Preview = ModelGeminiEmbedding2Preview;

    /// <summary>
    /// API resource name for <see cref="ModelGeminiEmbedding2"/> when configuring multimodal File Search stores.
    /// </summary>
    public const string FileSearchMultimodalEmbeddingModelResource = "models/gemini-embedding-2";
    
    /// <summary>
    /// The Text Embedding model is optimized for creating embeddings with 768 dimensions for text of up to 2,048 tokens. Text Embedding offers elastic embedding sizes under 768. You can use elastic embeddings to generate smaller output dimensions and potentially save computing and storage costs with minor performance loss.
    /// </summary>
    [Obsolete("Shut down January 14, 2026. Use ModelGeminiEmbedding001 instead.")]
    public static readonly EmbeddingModel ModelEmbedding4 = new EmbeddingModel("text-embedding-004", LLmProviders.Google, 2_048, 768);

    /// <summary>
    /// <inheritdoc cref="ModelEmbedding4"/>
    /// </summary>
    [Obsolete("Shut down January 14, 2026. Use GeminiEmbedding001 instead.")]
    public readonly EmbeddingModel Embedding4 = ModelEmbedding4;
    
    /// <summary>
    /// All known Gemini Embedding models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelEmbedding4,
        ModelGeminiEmbedding001,
        ModelGeminiEmbedding2,
        ModelGeminiEmbedding2Preview
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal EmbeddingModelGoogleGemini()
    {
        
    }
}