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
    /// Latest models by performance tier (Max/Large/Medium/Small). Pointers repointed as newer models release.
    /// </summary>
    public readonly ChatModelGoogleLatest Latest = new ChatModelGoogleLatest();

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
    /// Models that support reasoning.
    /// </summary>
    public static readonly HashSet<IModel> ReasoningModels = [
        ChatModelGoogleGemini.ModelGemini25Pro,
        ChatModelGoogleGemini.ModelGemini25Flash,
        ChatModelGoogleGemini.ModelGemini25FlashLite,
        ChatModelGoogleGemini.ModelGemini31FlashLite,
        ChatModelGoogleGeminiPreview.ModelGemini25FlashLitePreview0617,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashLitePreview,
        ChatModelGoogleGeminiPreview.ModelGemini3ProPreview,
        ChatModelGoogleGeminiPreview.ModelGemini3FlashPreview,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreview,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreviewCustomtools,
        ChatModelGoogleGemini.ModelGemini35Flash,
        ChatModelGoogleGeminiPreview.ModelGeminiRoboticsRe16Preview,
        ChatModelGoogleGemma.Model426BA4BIt,
        ChatModelGoogleGemma.Model431BIt
    ];
    
    /// <summary>
    /// Models that support image modality.
    /// </summary>
    public static readonly HashSet<IModel> ImageModalitySupportingModels = [
        ChatModelGoogleGemini.ModelGemini25Pro,
        ChatModelGoogleGemini.ModelGemini25Flash,
        ChatModelGoogleGemini.ModelGemini25FlashLite,
        ChatModelGoogleGemini.ModelGemini31FlashLite,
        ChatModelGoogleGeminiPreview.ModelGemini25FlashLitePreview0617,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashLitePreview,
        ChatModelGoogleGeminiPreview.ModelGemini3ProPreview,
        ChatModelGoogleGemini.ModelGemini3ProImage,
        ChatModelGoogleGeminiPreview.ModelGemini3ProImagePreview,
        ChatModelGoogleGeminiPreview.ModelGemini3FlashPreview,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreview,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreviewCustomtools,
        ChatModelGoogleGemini.ModelGemini31FlashImage,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashImagePreview
    ];
    
    /// <summary>
    /// Models that support Gemini 3 features.
    /// </summary>
    public static readonly HashSet<IModel> Gemini3Models = [
        ChatModelGoogleGeminiPreview.ModelGemini3ProPreview,
        ChatModelGoogleGemini.ModelGemini3ProImage,
        ChatModelGoogleGeminiPreview.ModelGemini3ProImagePreview,
        ChatModelGoogleGeminiPreview.ModelGemini3FlashPreview,
        ChatModelGoogleGemini.ModelGemini35Flash
    ];

    /// <summary>
    /// Models that support Gemini text-to-speech generation via the Chat API.
    /// </summary>
    public static readonly HashSet<IModel> TtsModels = [
        ChatModelGoogleGeminiPreview.ModelGemini25FlashPreviewTts,
        ChatModelGoogleGeminiPreview.ModelGemini25ProPreviewTts,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashTtsPreview
    ];
    
    /// <summary>
    /// Models that support Gemini 3.1 features. These models are backwards-compatible with <see cref="Gemini3Models"/> features.
    /// </summary>
    public static readonly HashSet<IModel> Gemini31Models = [
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreview,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreviewCustomtools,
        ChatModelGoogleGemini.ModelGemini31FlashLite,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashLitePreview,
        ChatModelGoogleGemini.ModelGemini31FlashImage,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashImagePreview
    ];

    /// <summary>
    /// Gemini 3.5 Flash models (GA). Supports minimal/low/medium (default)/high thinking levels. Computer Use is not supported.
    /// </summary>
    public static readonly HashSet<IModel> Gemini35Models = [
        ChatModelGoogleGemini.ModelGemini35Flash
    ];

    /// <summary>
    /// Gemini 3.1 Flash-Lite models (GA and preview). Supports minimal/low/medium/high thinking levels.
    /// </summary>
    public static readonly HashSet<IModel> Gemini31FlashLiteModels = [
        ChatModelGoogleGemini.ModelGemini31FlashLite,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashLitePreview
    ];

    /// <summary>
    /// Models that support Grounding with Google Maps.
    /// </summary>
    public static readonly HashSet<IModel> GoogleMapsGroundingModels = [
        ChatModelGoogleGemini.ModelGemini35Flash,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreview,
        ChatModelGoogleGeminiPreview.ModelGemini31ProPreviewCustomtools,
        ChatModelGoogleGemini.ModelGemini31FlashLite,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashLitePreview,
        ChatModelGoogleGeminiPreview.ModelGemini3FlashPreview,
        ChatModelGoogleGemini.ModelGemini25Pro,
        ChatModelGoogleGemini.ModelGemini25Flash,
        ChatModelGoogleGemini.ModelGemini25FlashLite,
        ChatModelGoogleGemini.ModelGemini2Flash001,
        ChatModelGoogleGemini.ModelGemini2FlashLatest,
        ChatModelGoogleGemini.ModelGeminiFlashLatest,
        ChatModelGoogleGemini.ModelGeminiFlashLiteLatest
    ];
    
    /// <summary>
    /// Models that do not support developer message.
    /// </summary>
    public static readonly HashSet<IModel> ModelsWithDisabledDeveloperMessage = [
        ChatModelGoogleGemini.ModelGemini15Pro,
        ChatModelGoogleGemini.ModelGemini15Flash8B
    ];

    /// <summary>
    /// Computer Use capable models from Google.
    /// </summary>
    public static List<IModel> ComputerUseModels => LazyComputerUseModels.Value;

    private static readonly Lazy<List<IModel>> LazyComputerUseModels = new Lazy<List<IModel>>(() => [ChatModelGoogleGeminiPreview.ModelGemini25ComputerUsePreview102025, ChatModelGoogleGeminiPreview.ModelGemini3FlashPreview, ChatModelGoogleGeminiPreview.ModelGemini31ProPreview, ChatModelGoogleGeminiPreview.ModelGemini31ProPreviewCustomtools]);

    /// <summary>
    /// Gemini Robotics-ER capable models from Google.
    /// </summary>
    public static List<IModel> RoboticsModels => LazyRoboticsModels.Value;

    private static readonly Lazy<List<IModel>> LazyRoboticsModels = new Lazy<List<IModel>>(() => [ChatModelGoogleGeminiPreview.ModelGeminiRoboticsRe16Preview]);
    
    /// <summary>
    /// Models capable of reasoning.
    /// </summary>
    public static List<IModel>? ReasoningModelsList => LazyReasoningModels.Value;

    private static readonly Lazy<List<IModel>> LazyReasoningModels = new Lazy<List<IModel>>(() => [ChatModelGoogleGeminiPreview.ModelGemini25FlashPreview0417, ChatModelGoogleGeminiPreview.ModelGemini25ProPreview0325, ChatModelGoogleGeminiPreview.ModelGemini25FlashPreview0520, ChatModelGoogleGeminiPreview.ModelGemini25ProPreview0506, ChatModelGoogleGeminiPreview.ModelGemini25ProPreview0605, ChatModelGoogleGemini.ModelGemini25Pro, ChatModelGoogleGemini.ModelGemini25Flash, ChatModelGoogleGeminiPreview.ModelGemini25FlashLitePreview0617, ChatModelGoogleGemini.ModelGemini31FlashLite, ChatModelGoogleGeminiPreview.ModelGemini31FlashLitePreview, ChatModelGoogleGemini.ModelGemini35Flash, ChatModelGoogleGeminiPreview.ModelGemini31ProPreview, ChatModelGoogleGeminiPreview.ModelGemini31ProPreviewCustomtools, ChatModelGoogleGeminiPreview.ModelGeminiRoboticsRe16Preview, ChatModelGoogleGemma.Model426BA4BIt, ChatModelGoogleGemma.Model431BIt]);

    /// <summary>
    /// Models capable of generating images.
    /// </summary>
    public static List<IModel> ImageModalitySupportingModelsList => LazyImageModalitySupportingModels.Value;

    private static readonly Lazy<List<IModel>> LazyImageModalitySupportingModels = new Lazy<List<IModel>>(() => [
        ChatModelGoogleGeminiExperimental.ModelGemini2FlashImageGeneration,
        ChatModelGoogleGeminiPreview.ModelGemini2FlashPreviewImageGeneration,
        ChatModelGoogleGemini.ModelGemini25FlashImage,
        ChatModelGoogleGeminiPreview.ModelGemini25FlashImagePreview,
        ChatModelGoogleGemini.ModelGemini3ProImage,
        ChatModelGoogleGeminiPreview.ModelGemini3ProImagePreview,
        ChatModelGoogleGemini.ModelGemini31FlashImage,
        ChatModelGoogleGeminiPreview.ModelGemini31FlashImagePreview
    ]);

    /// <summary>
    /// Models listed don't support system prompt.
    /// </summary>
    public static List<IModel> ModelsWithDisabledDeveloperMessageList => LazyModelsWithDisabledDeveloperMessage.Value;

    private static readonly Lazy<List<IModel>> LazyModelsWithDisabledDeveloperMessage = new Lazy<List<IModel>>(() => [ChatModelGoogleGemma.Model3Ne4B, ChatModelGoogleGemma.ModelV327B]);
    
    internal ChatModelGoogle()
    {
       
    }
}
