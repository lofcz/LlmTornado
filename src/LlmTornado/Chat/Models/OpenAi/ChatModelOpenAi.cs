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
    /// GPT-5.1 models.
    /// </summary>
    public readonly ChatModelOpenAiGpt51 Gpt51 = new ChatModelOpenAiGpt51();
    
    /// <summary>
    /// GPT-5.2 models.
    /// </summary>
    public readonly ChatModelOpenAiGpt52 Gpt52 = new ChatModelOpenAiGpt52();
    
    /// <summary>
    /// GPT-5.4 models.
    /// </summary>
    public readonly ChatModelOpenAiGpt54 Gpt54 = new ChatModelOpenAiGpt54();
    
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

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [..ChatModelOpenAiGpt35.ModelsAll, ..ChatModelOpenAiGpt4.ModelsAll, ..ChatModelOpenAiO3.ModelsAll, ..ChatModelOpenAiO4.ModelsAll, ..ChatModelOpenAiGpt41.ModelsAll, ..ChatModelOpenAiGpt5.ModelsAll, ..ChatModelOpenAiGpt51.ModelsAll, ..ChatModelOpenAiGpt52.ModelsAll, ..ChatModelOpenAiGpt54.ModelsAll, ..ChatModelOpenAiCodex.ModelsAll]);

    /// <summary>
    /// All reasoning models. Requests for these models are serialized differently.
    /// </summary>
    public static List<IModel> ReasoningModelsAll => LazyReasoningModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyReasoningModelsAll = new Lazy<List<IModel>>(() => [..ChatModelOpenAiGpt4.ReasoningModels, ..ChatModelOpenAiO3.ModelsAll, ..ChatModelOpenAiO4.ModelsAll, ..ChatModelOpenAiGpt5.ModelsAll, ..ChatModelOpenAiGpt51.ModelsAll, ..ChatModelOpenAiGpt52.ModelsAll, ..ChatModelOpenAiGpt54.ModelsAll, ChatModelOpenAiCodex.ModelGpt53Codex]);
    
    /// <summary>
    /// HashSet version of ReasoningModelsAll.
    /// </summary>
    internal static HashSet<IModel> ReasoningModelsAllSet => LazyReasoningModelsAllSet.Value;
    
    private static readonly Lazy<HashSet<IModel>> LazyReasoningModelsAllSet = new Lazy<HashSet<IModel>>(() => [..ReasoningModelsAll]);

    /// <summary>
    /// All models compatible with web_search. Requests for these models are serialized differently.
    /// </summary>
    public static List<IModel> WebSearchCompatibleModelsAll => LazyWebSearchCompatibleModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyWebSearchCompatibleModelsAll = new Lazy<List<IModel>>(() => [ChatModelOpenAiGpt4.ModelOSearchPreview, ChatModelOpenAiGpt4.ModelOMiniSearchPreview, ..ChatModelOpenAiGpt5.ModelsAll, ..ChatModelOpenAiGpt51.ModelsAll, ..ChatModelOpenAiGpt52.ModelsAll, ..ChatModelOpenAiGpt54.ModelsAll]);

    internal static HashSet<IModel> TempIncompatibleModels => LazyTempIncompatibleModels.Value;

    private static readonly Lazy<HashSet<IModel>> LazyTempIncompatibleModels = new Lazy<HashSet<IModel>>(() => [
        ..WebSearchCompatibleModelsAll.Concat(ChatModelOpenAiO3.ModelsAll).Concat(ChatModelOpenAiO4.ModelsAll).Concat(ChatModelOpenAiGpt5.ModelsAll)
    ]);

    /// <summary>
    /// Models that never support temperature/top_p/logprobs (older GPT-5 models).
    /// </summary>
    internal static HashSet<IModel> SamplingParamsNeverSupported => LazySamplingParamsNeverSupported.Value;
    
    private static readonly Lazy<HashSet<IModel>> LazySamplingParamsNeverSupported = new Lazy<HashSet<IModel>>(() => [
        ChatModelOpenAiGpt5.ModelV5, ChatModelOpenAiGpt5.ModelV5Mini, ChatModelOpenAiGpt5.ModelV5Nano, ChatModelOpenAiGpt5.ModelV5Pro, ChatModelOpenAiGpt5.ModelV5Codex, ChatModelOpenAiGpt54.ModelV54Pro, ChatModelOpenAiCodex.ModelGpt53Codex
    ]);
    
    /// <summary>
    /// Models that conditionally support temperature/top_p/logprobs only when reasoning effort is none (GPT-5.4, GPT-5.2, GPT-5.1).
    /// </summary>
    internal static HashSet<IModel> SamplingParamsConditionallySupported => LazySamplingParamsConditionallySupported.Value;
    
    private static readonly Lazy<HashSet<IModel>> LazySamplingParamsConditionallySupported = new Lazy<HashSet<IModel>>(() => [
        ..ChatModelOpenAiGpt51.ModelsAll, ..ChatModelOpenAiGpt52.ModelsAll, ChatModelOpenAiGpt54.ModelV54
    ]);
    
    /// <summary>
    /// Determines whether sampling parameters (temperature, top_p, logprobs) should be cleared for GPT-5.x models.
    /// GPT-5.4 parameter compatibility:
    /// - Older GPT-5 models (gpt-5, gpt-5-mini, gpt-5-nano) never support these parameters
    /// - GPT-5.4 only supports these on gpt-5.4 when reasoning effort is none, while gpt-5.4-pro never supports them
    /// - GPT-5.3-Codex never supports these because it only exposes reasoning modes low, medium, high, and xhigh
    /// - GPT-5.2 and GPT-5.1 only support these when reasoning effort is none
    /// </summary>
    /// <param name="model">The model being used.</param>
    /// <param name="hasNonNoneReasoningEffort">True if reasoning effort is set to something other than none.</param>
    /// <returns>True if sampling parameters should be cleared.</returns>
    internal static bool ShouldClearSamplingParams(IModel? model, bool hasNonNoneReasoningEffort)
    {
        if (model is null)
        {
            return false;
        }
        
        if (SamplingParamsNeverSupported.Contains(model))
        {
            return true;
        }
        
        if (SamplingParamsConditionallySupported.Contains(model) && hasNonNoneReasoningEffort)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// All audio models. Requests for these models are serialized differently.
    /// </summary>
    public static List<IModel> AudioModelsAll => LazyAudioModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyAudioModelsAll = new Lazy<List<IModel>>(() => [
        ChatModelOpenAiGpt5.ModelAudio, ChatModelOpenAiGpt5.ModelAudioMini, ChatModelOpenAiGpt5.ModelAudio15, ChatModelOpenAiGpt4.ModelAudioPreview, ChatModelOpenAiGpt4.ModelAudioPreview241001, ChatModelOpenAiGpt4.ModelAudioPreview241217,
        ChatModelOpenAiGpt4.ModelAudioPreview250603
    ]);
    
    /// <summary>
    /// HashSet version of AudioModelsAll.
    /// </summary>
    internal static HashSet<IModel> AudioModelsAllSet => LazyAudioModelsAllSet.Value;
    
    private static readonly Lazy<HashSet<IModel>> LazyAudioModelsAllSet = new Lazy<HashSet<IModel>>(() => [..AudioModelsAll]);

    
    internal ChatModelOpenAi()
    {
        
    }
}