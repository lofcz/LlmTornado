using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding.Models.OpenAi;

/// <summary>
/// Generation 2 embedding models from OpenAI.
/// </summary>
public class EmbeddingModelOpenAiGen2 : IVendorModelClassProvider
{
    /// <summary>
    /// Old embedding model. It is recommended to migrate to <see cref="EmbeddingModelOpenAiGen3"/>.
    /// </summary>
    public static readonly EmbeddingModel ModelAda = new EmbeddingModel("text-embedding-ada-002", LLmProviders.OpenAi, 4_096, 1_536);

    /// <summary>
    /// <inheritdoc cref="ModelAda"/>
    /// </summary>
    public readonly EmbeddingModel Ada = ModelAda;
    
    /// <summary>
    /// All known Generation 2 models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelAda
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal EmbeddingModelOpenAiGen2()
    {
        
    }
}