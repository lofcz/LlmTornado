using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Known chat models from xAI.
/// </summary>
public class ChatModelXAi : BaseVendorModelProvider
{
    /// <summary>
    /// Grok models.
    /// </summary>
    public readonly ChatModelXAiGrok Grok = new ChatModelXAiGrok();
    
    /// <summary>
    /// All known chat models from xAI.
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
        ..ChatModelXAiGrok.ModelsAll
    ];
    
    static ChatModelXAi()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelXAi()
    {
        AllModels = ModelsAll;
    }
}