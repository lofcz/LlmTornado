using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Anthropic.
/// </summary>
public class ChatModelAnthropic : BaseVendorModelProvider
{
    /// <summary>
    /// Latest models by performance tier (Max/Large/Medium/Small). Pointers repointed as newer models release.
    /// </summary>
    public readonly ChatModelAnthropicLatest Latest = new ChatModelAnthropicLatest();

    /// <summary>
    /// Claude 3 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude3 Claude3 = new ChatModelAnthropicClaude3();
    
    /// <summary>
    /// Claude 3.5 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude35 Claude35 = new ChatModelAnthropicClaude35();
    
    /// <summary>
    /// Claude 4 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude4 Claude4 = new ChatModelAnthropicClaude4();
    
    /// <summary>
    /// Claude 4.1 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude41 Claude41 = new ChatModelAnthropicClaude41();
    
    /// <summary>
    /// Claude 4.5 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude45 Claude45 = new ChatModelAnthropicClaude45();
    
    /// <summary>
    /// Claude 4.6 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude46 Claude46 = new ChatModelAnthropicClaude46();
    
    /// <summary>
    /// Claude 4.7+ models (NextOpus generation).
    /// </summary>
    public readonly ChatModelAnthropicClaude47 Claude47 = new ChatModelAnthropicClaude47();

    /// <summary>
    /// Claude 4.8 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude48 Claude48 = new ChatModelAnthropicClaude48();

    /// <summary>
    /// Claude 5 models (Fable 5, Sonnet 5).
    /// </summary>
    public readonly ChatModelAnthropicClaude5 Claude5 = new ChatModelAnthropicClaude5();

    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Anthropic;

    /// <summary>
    /// All known chat models from Anthropic.
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

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [..ChatModelAnthropicClaude3.ModelsAll, ..ChatModelAnthropicClaude35.ModelsAll, ..ChatModelAnthropicClaude4.ModelsAll, ..ChatModelAnthropicClaude41.ModelsAll, ..ChatModelAnthropicClaude45.ModelsAll, ..ChatModelAnthropicClaude46.ModelsAll, ..ChatModelAnthropicClaude47.ModelsAll, ..ChatModelAnthropicClaude48.ModelsAll, ..ChatModelAnthropicClaude5.ModelsAll]);
    
    internal ChatModelAnthropic()
    {
      
    }
}