using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding.Models.OpenAi;

/// <summary>
/// Generation 3 embedding models from OpenAI.
/// </summary>
public class EmbeddingModelOpenAiGen3 : IVendorModelClassProvider
{
    /// <summary>
    /// New and highly efficient embedding model providing a significant upgrade over its predecessor, the text-embedding-ada-002 model released in December 2022. 
    /// </summary>
    public static readonly EmbeddingModel ModelLarge = new EmbeddingModel("text-embedding-3-large", LLmProviders.OpenAi, 8_190, 3_072);

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly EmbeddingModel Large = ModelLarge;
    
    /// <summary>
    /// Substantially more efficient than previous generation text-embedding-ada-002 model.
    /// </summary>
    public static readonly EmbeddingModel ModelSmall = new EmbeddingModel("text-embedding-3-small", LLmProviders.OpenAi, 8_190, 1_536);

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly EmbeddingModel Small = ModelSmall;
    
    /// <summary>
    /// All known Generation 2 models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelLarge,
        ModelSmall
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal EmbeddingModelOpenAiGen3()
    {
        
    }
}