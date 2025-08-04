using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models.Voyage;
using LlmTornado.Models;

namespace LlmTornado.Embedding.Models;

/// <summary>
/// Models supporting multimodal embeddings generation.
/// </summary>
public class MultimodalEmbeddingModel : ModelEmbeddingBase
{
    /// <summary>
    /// Models from Voyage.
    /// </summary>
    public static readonly EmbeddingModelVoyageMultimodal Voyage = new EmbeddingModelVoyageMultimodal();
    
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
    public MultimodalEmbeddingModel(string name, LLmProviders provider, int contextTokens, int outputDimensions)
    {
        Name = name;
        Provider = provider;
        ContextTokens = contextTokens;
        OutputDimensions = outputDimensions;
    }
    
    /// <summary>
    /// Allows a model to be implicitly cast to the string of its <see cref="ModelBase.Name" />
    /// </summary>
    public static implicit operator string(MultimodalEmbeddingModel model)
    {
        return model.Name;
    }
    
    /// <summary>
    /// Allows a string to be implicitly cast as an <see cref="Model" /> with that <see cref="IModel.Name" />
    /// </summary>
    public static implicit operator MultimodalEmbeddingModel(string name)
    {
        AllModelsMap.TryGetValue(name, out IModel? model);
        return (MultimodalEmbeddingModel)model!;
    }
}