using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Known chat models from xAI.
/// </summary>
public class ChatModelXAi : BaseVendorModelProvider
{
    /// <summary>
    /// Grok 4 models.
    /// </summary>
    public readonly ChatModelXAiGrok4 Grok4 = new ChatModelXAiGrok4();
    
    /// <summary>
    /// Grok 3 models.
    /// </summary>
    public readonly ChatModelXAiGrok3 Grok3 = new ChatModelXAiGrok3();
    
    /// <summary>
    /// Grok 1 & 2 models.
    /// </summary>
    public readonly ChatModelXAiGrok Grok = new ChatModelXAiGrok();

    /// <summary>
    /// All known chat models from xAI.
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
        ..ChatModelXAiGrok.ModelsAll,
        ..ChatModelXAiGrok3.ModelsAll,
        ..ChatModelXAiGrok4.ModelsAll
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
       
    }
}