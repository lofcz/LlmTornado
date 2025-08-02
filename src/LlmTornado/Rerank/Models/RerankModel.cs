using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Rerank.Models.Voyage;

namespace LlmTornado.Rerank.Models;

/// <summary>
/// Models supporting reranking.
/// </summary>
public class RerankModel : ModelBase
{
    /// <summary>
    /// Models from Voyage.
    /// </summary>
    public static readonly RerankModelVoyage Voyage = new();
    
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    /// <summary>
    /// All known rerank models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    static RerankModel()
    {
        AllModels =
        [
            ..Voyage.AllModels
        ];
        
        AllModels.ForEach(x =>
        {
            AllModelsMap.TryAdd(x.Name, x);
        });
    }
    
    /// <summary>
    /// Represents a Model with the given name.
    /// </summary>
    public RerankModel(string name, LLmProviders? provider = null)
    {
        Name = name;
        Provider = provider ?? GetProvider(name) ?? LLmProviders.Voyage;
    }

    /// <summary>
    /// Represents a generic model.
    /// </summary>
    public RerankModel()
    {
    }
    
    /// <summary>
    /// Looks up the model provider. Only works for known models.
    /// </summary>
    public static LLmProviders? GetProvider(string? modelName)
    {
        if (modelName is not null && AllModelsMap.TryGetValue(modelName, out IModel? protoModel))
        {
            return protoModel.Provider;
        }

        return null;
    }
    
    /// <summary>
    /// Allows a string to be implicitly cast as an <see cref="RerankModel" /> with that <see cref="IModel.Name" />
    /// </summary>
    public static implicit operator RerankModel(string? name)
    {
        return new RerankModel(name ?? string.Empty, name is null ? LLmProviders.Voyage : GetProvider(name) ?? LLmProviders.Voyage);
    }
}