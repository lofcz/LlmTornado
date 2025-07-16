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
    /// The Text Embedding model is optimized for creating embeddings with 768 dimensions for text of up to 2,048 tokens. Text Embedding offers elastic embedding sizes under 768. You can use elastic embeddings to generate smaller output dimensions and potentially save computing and storage costs with minor performance loss.
    /// </summary>
    public static readonly EmbeddingModel ModelEmbedding4 = new EmbeddingModel("text-embedding-004", LLmProviders.Google, 2_048, 768);

    /// <summary>
    /// <inheritdoc cref="ModelEmbedding4"/>
    /// </summary>
    public readonly EmbeddingModel Embedding4 = ModelEmbedding4;
    
    /// <summary>
    /// All known Gemini Embedding models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelEmbedding4,
        ModelGeminiEmbedding001
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal EmbeddingModelGoogleGemini()
    {
        
    }
}