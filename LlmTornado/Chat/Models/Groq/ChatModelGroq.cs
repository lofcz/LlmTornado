using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models provided by Groq.
/// </summary>
public class ChatModelGroq : BaseVendorModelProvider
{
    /// <summary>
    /// Models by Meta.
    /// </summary>
    public readonly ChatModelGroqMeta Meta = new ChatModelGroqMeta();
    
    /// <summary>
    /// Models by Meta.
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
    /// All known chat models hosted by Groq.
    /// </summary>
    public override List<IModel> AllModels { get; }

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
        ..ChatModelGroqMistral.ModelsAll
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
        AllModels = ModelsAll;
    }
}