using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models.Voyage;
using LlmTornado.Models;

namespace LlmTornado.Embedding.Models;

/// <summary>
/// Models supporting contextual embeddings generation.
/// </summary>
public class ContextualEmbeddingModel : ModelEmbeddingBase
{
    /// <summary>
    /// Models from Voyage.
    /// </summary>
    public static readonly EmbeddingModelVoyageContextual Voyage = new EmbeddingModelVoyageContextual();
    
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static Dictionary<string, IModel> AllModelsMap => AllModelsMapLazy.Value;

    /// <summary>
    /// All known chat models.
    /// </summary>
    public static List<IModel> AllModels => AllModelsLazy.Value;
    
    private static readonly Lazy<Dictionary<string, IModel>> AllModelsMapLazy = new Lazy<Dictionary<string, IModel>>(() =>
    {
        return AllModels.ToDictionary(x => x.Name, x => x);
    });
    
    private static readonly Lazy<List<IModel>> AllModelsLazy = new Lazy<List<IModel>>(() =>
    [
        ..Voyage.AllModels
    ]);
    
    /// <summary>
    /// Represents a Model with the given name.
    /// </summary>
    /// <param name="name">The id/name of the model.</param>
    /// <param name="ownedBy"></param>
    /// <param name="provider"></param>
    public ContextualEmbeddingModel(string name, string? ownedBy = null, LLmProviders? provider = null)
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
    public ContextualEmbeddingModel(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
    }
    
    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    public ContextualEmbeddingModel(string name, LLmProviders provider, int contextTokens, int outputDimensions)
    {
        Name = name;
        Provider = provider;
        ContextTokens = contextTokens;
        OutputDimensions = outputDimensions;
    }
    
    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    public ContextualEmbeddingModel(string name, LLmProviders provider, int contextTokens, int outputDimensions, List<int> matryoshkaDimensions)
    {
        Name = name;
        Provider = provider;
        ContextTokens = contextTokens;
        OutputDimensions = outputDimensions;
        MatryoshkaDimensions = matryoshkaDimensions;
    }
    
    /// <summary>
    /// Creates a new model identified by name. The provider of the model is inferred automatically.
    /// </summary>
    /// <param name="name"></param>
    public ContextualEmbeddingModel(string name)
    {
        Name = name;
        Provider = GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Represents a generic model.
    /// </summary>
    public ContextualEmbeddingModel()
    {
    }
    
    /// <summary>
    /// Allows a model to be implicitly cast to the string of its <see cref="ModelBase.Name" />
    /// </summary>
    /// <param name="model">The <see cref="ContextualEmbeddingModel" /> to cast to a string.</param>
    public static implicit operator string(ContextualEmbeddingModel model)
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
    public static implicit operator ContextualEmbeddingModel(string? name)
    {
        return new ContextualEmbeddingModel(name ?? string.Empty, name is null ? LLmProviders.OpenAi : GetProvider(name) ?? LLmProviders.OpenAi);
    }

    /// <summary>
    /// Returns name and information about the model.
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Name);
        
        if (OutputDimensions > 0)
        {
            sb.Append($", dims: {OutputDimensions}");
        }

        if (MatryoshkaDimensions?.Count > 0)
        {
            sb.Append($", possible dims: [{MatryoshkaDimensions.ToCsv(", ")}]");
        }
        
        return Name;
    }
}