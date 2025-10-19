using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Known chat models from DeepInfra.
/// </summary>
public class ChatModelDeepInfra : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.DeepInfra;
    
    /// <summary>
    /// Models from DeepSeek.
    /// </summary>
    public readonly ChatModelDeepInfraDeepSeek DeepSeek = new ChatModelDeepInfraDeepSeek();
    
    /// <summary>
    /// Models from Qwen.
    /// </summary>
    public readonly ChatModelDeepInfraQwen Qwen = new ChatModelDeepInfraQwen();
    
    /// <summary>
    /// Models from Meta.
    /// </summary>
    public readonly ChatModelDeepInfraMeta Meta = new ChatModelDeepInfraMeta();
    
    /// <summary>
    /// Models from Microsoft.
    /// </summary>
    public readonly ChatModelDeepInfraMicrosoft Microsoft = new ChatModelDeepInfraMicrosoft();
    
    /// <summary>
    /// Models from Google.
    /// </summary>
    public readonly ChatModelDeepInfraGoogle Google = new ChatModelDeepInfraGoogle();
    
    /// <summary>
    /// Models from Mistral.
    /// </summary>
    public readonly ChatModelDeepInfraMistral Mistral = new ChatModelDeepInfraMistral();
    
    /// <summary>
    /// Models from NVIDIA Nemotron.
    /// </summary>
    public readonly ChatModelDeepInfraNemotron Nemotron = new ChatModelDeepInfraNemotron();
    
    /// <summary>
    /// Models from Anthropic.
    /// </summary>
    public readonly ChatModelDeepInfraAnthropic Anthropic = new ChatModelDeepInfraAnthropic();
    
    /// <summary>
    /// All known chat models from DeepInfra.
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

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [..ChatModelDeepInfraDeepSeek.ModelsAll, ..ChatModelDeepInfraQwen.ModelsAll, ..ChatModelDeepInfraMeta.ModelsAll, ..ChatModelDeepInfraMicrosoft.ModelsAll, ..ChatModelDeepInfraGoogle.ModelsAll, ..ChatModelDeepInfraMistral.ModelsAll, ..ChatModelDeepInfraNemotron.ModelsAll, ..ChatModelDeepInfraAnthropic.ModelsAll]);
    
    internal ChatModelDeepInfra()
    {
        
    }
}