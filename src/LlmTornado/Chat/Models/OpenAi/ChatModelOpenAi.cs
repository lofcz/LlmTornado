using System;
using System.Collections.Generic;
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
    public static readonly HashSet<string> AllModelsMap = [];
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ..ChatModelOpenAiGpt35.ModelsAll,
        ..ChatModelOpenAiGpt4.ModelsAll,
        ..ChatModelOpenAiO3.ModelsAll,
        ..ChatModelOpenAiO4.ModelsAll,
        ..ChatModelOpenAiGpt41.ModelsAll,
        ..ChatModelOpenAiGpt5.ModelsAll,
        ..ChatModelOpenAiCodex.ModelsAll,
    ];

    /// <summary>
    /// All reasoning models. Requests for these models are serialized differently.
    /// </summary>
    public static readonly List<IModel> ReasoningModelsAll =
    [
        ..ChatModelOpenAiGpt4.ReasoningModels,
        ..ChatModelOpenAiO3.ModelsAll,
        ..ChatModelOpenAiO4.ModelsAll,
        ..ChatModelOpenAiGpt5.ModelsAll
    ];

    /// <summary>
    /// All models compatible with web_search. Requests for these models are serialized differently.
    /// </summary>
    public static readonly List<IModel> WebSearchCompatibleModelsAll =
    [
        ChatModelOpenAiGpt4.ModelOSearchPreview,
        ChatModelOpenAiGpt4.ModelOMiniSearchPreview,
        ..ChatModelOpenAiGpt5.ModelsAll,
    ];

    internal static readonly HashSet<IModel> TempIncompatibleModels =
    [
        //..ReasoningModelsAll,
        ..WebSearchCompatibleModelsAll,
        ..ChatModelOpenAiO3.ModelsAll,
        ..ChatModelOpenAiO4.ModelsAll,
        ..ChatModelOpenAiGpt5.ModelsAll
    ];
    
    static ChatModelOpenAi()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelOpenAi()
    {
        
    }
}