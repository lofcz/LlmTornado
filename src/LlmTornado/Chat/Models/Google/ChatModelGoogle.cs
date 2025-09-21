using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Google.
/// </summary>
public class ChatModelGoogle : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Google;
    
    /// <summary>
    /// Gemini models.
    /// </summary>
    public readonly ChatModelGoogleGemini Gemini = new ChatModelGoogleGemini();
    
    /// <summary>
    /// Gemma models.
    /// </summary>
    public readonly ChatModelGoogleGemma Gemma = new ChatModelGoogleGemma();
    
    /// <summary>
    /// Experimental Gemini models.
    /// </summary>
    public readonly ChatModelGoogleGeminiExperimental GeminiExperimental = new ChatModelGoogleGeminiExperimental();
    
    /// <summary>
    /// Preview Gemini models.
    /// </summary>
    public readonly ChatModelGoogleGeminiPreview GeminiPreview = new ChatModelGoogleGeminiPreview();

    /// <summary>
    /// All known chat models from Google.
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

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [..ChatModelGoogleGemini.ModelsAll, ..ChatModelGoogleGeminiExperimental.ModelsAll, ..ChatModelGoogleGemma.ModelsAll, ..ChatModelGoogleGeminiPreview.ModelsAll]);
    
    /// <summary>
    /// Models capable of reasoning.
    /// </summary>
    public static List<IModel> ReasoningModels => LazyReasoningModels.Value;

    private static readonly Lazy<List<IModel>> LazyReasoningModels = new Lazy<List<IModel>>(() => [ChatModelGoogleGeminiPreview.ModelGemini25FlashPreview0417, ChatModelGoogleGeminiPreview.ModelGemini25ProPreview0325, ChatModelGoogleGeminiPreview.ModelGemini25FlashPreview0520, ChatModelGoogleGeminiPreview.ModelGemini25ProPreview0506, ChatModelGoogleGeminiPreview.ModelGemini25ProPreview0605, ChatModelGoogleGemini.ModelGemini25Pro, ChatModelGoogleGemini.ModelGemini25Flash, ChatModelGoogleGeminiPreview.ModelGemini25FlashLitePreview0617]);

    /// <summary>
    /// Models capable of generating images.
    /// </summary>
    public static List<IModel> ImageModalitySupportingModels => LazyImageModalitySupportingModels.Value;

    private static readonly Lazy<List<IModel>> LazyImageModalitySupportingModels = new Lazy<List<IModel>>(() => [ChatModelGoogleGeminiExperimental.ModelGemini2FlashImageGeneration, ChatModelGoogleGeminiPreview.ModelGemini2FlashPreviewImageGeneration]);

    /// <summary>
    /// Models listed don't support system prompt.
    /// </summary>
    public static List<IModel> ModelsWithDisabledDeveloperMessage => LazyModelsWithDisabledDeveloperMessage.Value;

    private static readonly Lazy<List<IModel>> LazyModelsWithDisabledDeveloperMessage = new Lazy<List<IModel>>(() => [..ChatModelGoogleGemma.ModelsAll]);
    
    internal ChatModelGoogle()
    {
       
    }
}