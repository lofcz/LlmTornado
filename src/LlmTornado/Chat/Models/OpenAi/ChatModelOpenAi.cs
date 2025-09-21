using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from OpenAI.
/// </summary>
public class ChatModelOpenAi : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.OpenAi;
    
    /// <summary>
    /// GPT 3.5 (Turbo) models.
    /// </summary>
    public readonly ChatModelOpenAiGpt35 Gpt35 = new ChatModelOpenAiGpt35();

    /// <summary>
    /// GPT 4 (Turbo) models & O1 Models.
    /// </summary>
    public readonly ChatModelOpenAiGpt4 Gpt4 = new ChatModelOpenAiGpt4();
    
    /// <summary>
    /// GPT-4.1 models.
    /// </summary>
    public readonly ChatModelOpenAiGpt41 Gpt41 = new ChatModelOpenAiGpt41();
    
    /// <summary>
    /// GPT-5 models.
    /// </summary>
    public readonly ChatModelOpenAiGpt5 Gpt5 = new ChatModelOpenAiGpt5();
    
    /// <summary>
    /// O3 models.
    /// </summary>
    public readonly ChatModelOpenAiO3 O3 = new ChatModelOpenAiO3();
    
    /// <summary>
    /// O4 models.
    /// </summary>
    public readonly ChatModelOpenAiO4 O4 = new ChatModelOpenAiO4();
    
    /// <summary>
    /// Codex models.
    /// </summary>
    public readonly ChatModelOpenAiCodex Codex = new ChatModelOpenAiCodex();

    /// <summary>
    /// All known chat models from OpenAI.
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

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [..ChatModelOpenAiGpt35.ModelsAll, ..ChatModelOpenAiGpt4.ModelsAll, ..ChatModelOpenAiO3.ModelsAll, ..ChatModelOpenAiO4.ModelsAll, ..ChatModelOpenAiGpt41.ModelsAll, ..ChatModelOpenAiGpt5.ModelsAll, ..ChatModelOpenAiCodex.ModelsAll]);

    /// <summary>
    /// All reasoning models. Requests for these models are serialized differently.
    /// </summary>
    public static List<IModel> ReasoningModelsAll => LazyReasoningModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyReasoningModelsAll = new Lazy<List<IModel>>(() => [..ChatModelOpenAiGpt4.ReasoningModels, ..ChatModelOpenAiO3.ModelsAll, ..ChatModelOpenAiO4.ModelsAll, ..ChatModelOpenAiGpt5.ModelsAll]);

    /// <summary>
    /// All models compatible with web_search. Requests for these models are serialized differently.
    /// </summary>
    public static List<IModel> WebSearchCompatibleModelsAll => LazyWebSearchCompatibleModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyWebSearchCompatibleModelsAll = new Lazy<List<IModel>>(() => [ChatModelOpenAiGpt4.ModelOSearchPreview, ChatModelOpenAiGpt4.ModelOMiniSearchPreview, ..ChatModelOpenAiGpt5.ModelsAll]);

    internal static HashSet<IModel> TempIncompatibleModels => LazyTempIncompatibleModels.Value;

    private static readonly Lazy<HashSet<IModel>> LazyTempIncompatibleModels = new Lazy<HashSet<IModel>>(() => new HashSet<IModel>(WebSearchCompatibleModelsAll.Concat(ChatModelOpenAiO3.ModelsAll).Concat(ChatModelOpenAiO4.ModelsAll).Concat(ChatModelOpenAiGpt5.ModelsAll)));
    
    internal ChatModelOpenAi()
    {
        
    }
}