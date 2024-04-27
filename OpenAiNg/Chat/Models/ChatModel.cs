using System.Collections.Generic;
using OpenAiNg.Code;
using OpenAiNg.Code.Models;
using OpenAiNg.Models;

namespace OpenAiNg.Chat.Models;

/// <summary>
/// Models supporting text based inference, such as chat or completions
/// </summary>
public class ChatModel : ModelBase
{
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    /// <summary>
    /// All known chat models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    static ChatModel()
    {
        AllModels = [
            ..OpenAi.AllModels,
            ..Anthropic.AllModels
        ];
        
        foreach (IModel x in AllModels)
        {
            AllModelsMap.TryAdd(x.Name, x);
        }
    }
    
    /// <summary>
    /// Models from OpenAI.
    /// </summary>
    public static readonly ChatModelOpenAi OpenAi = new ChatModelOpenAi();

    /// <summary>
    /// Models from Anthropic.
    /// </summary>
    public static readonly ChatModelAnthropic Anthropic = new ChatModelAnthropic();
    
    /// <summary>
    /// Represents an Model with the given name.
    /// </summary>
    /// <param name="name">The id/name of the model.</param>
    /// <param name="ownedBy"></param>
    /// <param name="provider"></param>
    public ChatModel(string name, string? ownedBy = null, LLmProviders? provider = null)
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
    public ChatModel(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
    }
    
    /// <summary>
    /// Creates a new model identified by name. The provider of the model is inferred automatically.
    /// </summary>
    /// <param name="name"></param>
    public ChatModel(string name)
    {
        Name = name;
        Provider = GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Represents a generic model.
    /// </summary>
    public ChatModel()
    {
    }
    
    /// <summary>
    /// Allows an model to be implicitly cast to the string of its <see cref="ModelBase.Name" />
    /// </summary>
    /// <param name="model">The <see cref="ChatModel" /> to cast to a string.</param>
    public static implicit operator string(ChatModel model)
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
    public static implicit operator ChatModel(string? name)
    {
        return new ChatModel(name ?? string.Empty, name is null ? LLmProviders.OpenAi : GetProvider(name) ?? LLmProviders.OpenAi);
    }
}