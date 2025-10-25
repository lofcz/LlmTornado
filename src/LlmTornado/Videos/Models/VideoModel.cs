using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Videos.Models.Google;
using LlmTornado.Models;

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
    public VideoModel(string name, string? ownedBy = null, LLmProviders? provider = null)
    {
        Name = name;
        OwnedBy = ownedBy ?? "google";
        Provider = provider ?? GetProvider(name) ?? LLmProviders.Google;
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