using System.Collections.Generic;
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
    public static readonly EmbeddingModelVoyageMultimodal Voyage = new();
    
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    /// <summary>
    /// All known multimodal embedding models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    static MultimodalEmbeddingModel()
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