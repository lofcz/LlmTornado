using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Videos.Models.Google;
using LlmTornado.Videos.Models.MiniMax;
using LlmTornado.Videos.Models.OpenAi;
using LlmTornado.Videos.Models.XAi;
using LlmTornado.Videos.Models.Zai;

namespace LlmTornado.Videos.Models;

/// <summary>
/// Models supporting video inference.
/// </summary>
public class VideoModel : ModelBase
{
    /// <summary>
    /// Models from Google.
    /// </summary>
    public static readonly VideoModelGoogle Google = new VideoModelGoogle();
    
    /// <summary>
    /// Models from OpenAI.
    /// </summary>
    public static readonly VideoModelOpenAi OpenAi = new VideoModelOpenAi();
    
    /// <summary>
    /// Models from xAI.
    /// </summary>
    public static readonly VideoModelXAi XAi = new VideoModelXAi();
    
    /// <summary>
    /// Models from Z.AI.
    /// </summary>
    public static readonly VideoModelZai Zai = new VideoModelZai();
    
    /// <summary>
    /// Models from MiniMax.
    /// </summary>
    public static readonly VideoModelMiniMax MiniMax = new VideoModelMiniMax();
    
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    /// <summary>
    /// All known video models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    static VideoModel()
    {
        AllModels = [
            ..Google.AllModels,
            ..OpenAi.AllModels,
            ..XAi.AllModels,
            ..Zai.AllModels,
            ..MiniMax.AllModels
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
    public VideoModel(string name, string? ownedBy = null, LLmProviders? provider = null)
    {
        Name = name;
        OwnedBy = ownedBy;
        Provider = provider ?? GetProvider(name) ?? LLmProviders.Google;
    }

    /// <summary>
    /// Represents a Model with the given name.
    /// </summary>
    /// <param name="name">The id/name of the model.</param>
    /// <param name="ownedBy"></param>
    /// <param name="provider"></param>
    /// <param name="aliases"></param>
    public VideoModel(string name, string? ownedBy, LLmProviders? provider, List<string> aliases)
    {
        Name = name;
        OwnedBy = ownedBy;
        Provider = provider ?? GetProvider(name) ?? LLmProviders.Google;
        Aliases = aliases;
    }

    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public VideoModel(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
    }
    
    /// <summary>
    /// Creates a new model identified by name, provider with a list of aliases.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    /// <param name="aliases"></param>
    public VideoModel(string name, LLmProviders provider, List<string> aliases)
    {
        Name = name;
        Provider = provider;
        Aliases = aliases;
    }
    
    /// <summary>
    /// Gets the provider for a model name.
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
    /// Implicit conversion from string to VideoModel
    /// </summary>
    /// <param name="name"></param>
    public static implicit operator VideoModel(string name) => new VideoModel(name);
}