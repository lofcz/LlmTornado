using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Known chat models from xAI.
/// </summary>
public class ChatModelXAi : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.XAi;
    
    /// <summary>
    /// Grok Code models.
    /// </summary>
    public readonly ChatModelXAiGrokCode GrokCode = new ChatModelXAiGrokCode();
    
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
    public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;

    private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
    {
        HashSet<string> map = [];

        ModelsAll.ForEach(x => { map.Add(x.Name); });

        return map;
    });
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [..ChatModelXAiGrok.ModelsAll, ..ChatModelXAiGrok3.ModelsAll, ..ChatModelXAiGrok4.ModelsAll, ..ChatModelXAiGrokCode.ModelsAll]);
    
    internal ChatModelXAi()
    {
       
    }
}