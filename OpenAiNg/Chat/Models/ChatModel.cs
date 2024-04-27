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
    public static readonly Dictionary<string, IModel> AllModelsMap = new Dictionary<string, IModel>();

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
    /// Represents an Model with the given id/<see cref="ModelID" />
    /// </summary>
    /// <param name="name">The id/<see cref="ModelID" /> to use.</param>
    /// <param name="ownedBy">Either</param>
    /// <param name="provider">Either</param>
    public ChatModel(string name, string? ownedBy = null, LLmProviders provider = LLmProviders.OpenAi)
    {
        Name = name;
        OwnedBy = ownedBy ?? "openai";
        Provider = provider;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public ChatModel(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
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
    /// Allows a string to be implicitly cast as an <see cref="Model" /> with that <see cref="IModel.Name" />
    /// </summary>
    /// <param name="name">The id/<see cref="IModel.Name" /> to use</param>
    public static implicit operator ChatModel(string? name)
    {
        LLmProviders provider = LLmProviders.OpenAi; 
        
        if (name is not null && AllModelsMap.TryGetValue(name, out IModel? protoModel))
        {
            provider = protoModel.Provider;
        }
        
        return new ChatModel(name ?? string.Empty, provider);
    }
}