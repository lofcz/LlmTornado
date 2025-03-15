using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models.Cohere;
using LlmTornado.Embedding.Models.Google;
using LlmTornado.Embedding.Models.OpenAi;
using LlmTornado.Embedding.Models.Voyage;
using LlmTornado.Models;

namespace LlmTornado.Embedding.Models;

/// <summary>
/// Models supporting embeddings generation.
/// </summary>
public class EmbeddingModel : ModelEmbeddingBase
{
     /// <summary>
    /// Models from OpenAI.
    /// </summary>
    public static readonly EmbeddingModelOpenAi OpenAi = new EmbeddingModelOpenAi();

    /// <summary>
    /// Models from Voyage.
    /// </summary>
    public static readonly EmbeddingModelVoyage Voyage = new EmbeddingModelVoyage();
    
    /// <summary>
    /// Models from Cohere.
    /// </summary>
    public static readonly EmbeddingModelCohere Cohere = new EmbeddingModelCohere();
    
    /// <summary>
    /// Models from Google.
    /// </summary>
    public static readonly EmbeddingModelGoogle Google = new EmbeddingModelGoogle();
    
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    /// <summary>
    /// All known chat models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    static EmbeddingModel()
    {
        AllModels = [
            ..OpenAi.AllModels,
            ..Voyage.AllModels,
            ..Cohere.AllModels,
            ..Google.AllModels
        ];
        
        AllModels.ForEach(x =>
        {
            AllModelsMap.TryAdd(x.Name, x);
        });
    }
    
    /// <summary>
    /// Represents a Model with the given name.
    /// </summary>
    /// <param name="name">The id/name of the model.</param>
    /// <param name="ownedBy"></param>
    /// <param name="provider"></param>
    public EmbeddingModel(string name, string? ownedBy = null, LLmProviders? provider = null)
    {
        Name = name;
        OwnedBy = ownedBy ?? "openai";
        Provider = provider ?? GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public EmbeddingModel(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
    }
    
    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    /// <param name="contextTokens"></param>
    /// <param name="outputDimensions"></param>
    public EmbeddingModel(string name, LLmProviders provider, int contextTokens, int outputDimensions)
    {
        Name = name;
        Provider = provider;
        ContextTokens = contextTokens;
        OutputDimensions = outputDimensions;
    }
    
    /// <summary>
    /// Creates a new model identified by name. The provider of the model is inferred automatically.
    /// </summary>
    /// <param name="name"></param>
    public EmbeddingModel(string name)
    {
        Name = name;
        Provider = GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Represents a generic model.
    /// </summary>
    public EmbeddingModel()
    {
    }
    
    /// <summary>
    /// Allows a model to be implicitly cast to the string of its <see cref="ModelBase.Name" />
    /// </summary>
    /// <param name="model">The <see cref="EmbeddingModel" /> to cast to a string.</param>
    public static implicit operator string(EmbeddingModel model)
    {
        return model.Name;
    }

    /// <summary>
    /// Looks up the model provider. Only works for known models.
    /// </summary>
    /// <param name="modelName"></param>
    /// <returns></returns>
    public static LLmProviders? GetProvider(string? modelName)
    {
        if (modelName is not null && AllModelsMap.TryGetValue(modelName, out IModel? protoModel))
        {
            return protoModel.Provider;
        }

        return null;
    }
    
    /// <summary>
    /// Allows a string to be implicitly cast as an <see cref="Model" /> with that <see cref="IModel.Name" />
    /// </summary>
    /// <param name="name">The id/<see cref="IModel.Name" /> to use</param>
    public static implicit operator EmbeddingModel(string? name)
    {
        return new EmbeddingModel(name ?? string.Empty, name is null ? LLmProviders.OpenAi : GetProvider(name) ?? LLmProviders.OpenAi);
    }

    /// <summary>
    /// Returns name of the model.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }
}