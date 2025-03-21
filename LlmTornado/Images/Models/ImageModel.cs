using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Images.Models.Google;
using LlmTornado.Images.Models.OpenAi;
using LlmTornado.Images.Models.XAi;
using LlmTornado.Models;

namespace LlmTornado.Images.Models;

/// <summary>
/// Models supporting image inference.
/// </summary>
public class ImageModel : ModelBase
{
    /// <summary>
    /// Models from OpenAI.
    /// </summary>
    public static readonly ImageModelOpenAi OpenAi = new ImageModelOpenAi();
    
    /// <summary>
    /// Models from Google.
    /// </summary>
    public static readonly ImageModelGoogle Google = new ImageModelGoogle();
    
    /// <summary>
    /// Models from xAI.
    /// </summary>
    public static readonly ImageModelXAi XAi = new ImageModelXAi();
     
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    /// <summary>
    /// All known chat models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    static ImageModel()
    {
        AllModels = [
            ..OpenAi.AllModels,
            ..Google.AllModels,
            ..XAi.AllModels
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
    public ImageModel(string name, string? ownedBy = null, LLmProviders? provider = null)
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
    public ImageModel(string name, LLmProviders provider)
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
    public ImageModel(string name, LLmProviders provider, List<string> aliases)
    {
        Name = name;
        Provider = provider;
        Aliases = aliases;
    }
    
    /// <summary>
    /// Creates a new model identified by name. The provider of the model is inferred automatically.
    /// </summary>
    /// <param name="name"></param>
    public ImageModel(string name)
    {
        Name = name;
        Provider = GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Represents a generic model.
    /// </summary>
    public ImageModel()
    {
    }
    
    /// <summary>
    /// Allows a model to be implicitly cast to the string of its <see cref="ModelBase.Name" />
    /// </summary>
    /// <param name="model">The <see cref="ImageModel" /> to cast to a string.</param>
    public static implicit operator string(ImageModel model)
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
    public static implicit operator ImageModel(string? name)
    {
        return new ImageModel(name ?? string.Empty, name is null ? LLmProviders.OpenAi : GetProvider(name) ?? LLmProviders.OpenAi);
    }
}