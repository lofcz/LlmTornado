using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models provided by Groq.
/// </summary>
public class ChatModelGroq : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Groq;
    
    /// <summary>
    /// Models by Meta.
    /// </summary>
    public readonly ChatModelGroqMeta Meta = new ChatModelGroqMeta();
    
    /// <summary>
    /// Models by Groq.
    /// </summary>
    public readonly ChatModelGroqGroq Groq = new ChatModelGroqGroq();
    
    /// <summary>
    /// Models by Mistral.
    /// </summary>
    public readonly ChatModelGroqMistral Mistral = new ChatModelGroqMistral();
    
    /// <summary>
    /// Models by Google.
    /// </summary>
    public readonly ChatModelGroqGoogle Google = new ChatModelGroqGoogle();
    
    /// <summary>
    /// Models by Alibaba (Qwen).
    /// </summary>
    public readonly ChatModelGroqAlibaba Alibaba = new ChatModelGroqAlibaba();
    
    /// <summary>
    /// Models by Moonshot AI.
    /// </summary>
    public readonly ChatModelGroqMoonshotAi MoonshotAi = new ChatModelGroqMoonshotAi();
    
    /// <summary>
    /// Models by OpenAI.
    /// </summary>
    public readonly ChatModelGroqOpenAi OpenAi = new ChatModelGroqOpenAi();

    /// <summary>
    /// All known chat models hosted by Groq.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// Checks whether the model is owned by the provider.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public override bool OwnsModel(string model)
    {
        return AllModelsMap.Contains(model);
    }

    /// <summary>
    /// Map of models owned by the provider.
    /// </summary>
    public static readonly HashSet<string> AllModelsMap = [];
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ..ChatModelGroqMeta.ModelsAll,
        ..ChatModelGroqGoogle.ModelsAll,
        ..ChatModelGroqGroq.ModelsAll,
        ..ChatModelGroqMistral.ModelsAll,
        ..ChatModelGroqAlibaba.ModelsAll,
        ..ChatModelGroqMoonshotAi.ModelsAll,
        ..ChatModelGroqOpenAi.ModelsAll
    ];
    
    static ChatModelGroq()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelGroq()
    {
      
    }
}