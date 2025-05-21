using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Preview Gemini class models from Google.
/// Similar to <see cref="ChatModelGoogleGeminiExperimental"/> but billing-enabled.
/// </summary>
public class ChatModelGoogleGeminiPreview : IVendorModelClassProvider
{
    /// <summary>
    /// Gemini 2.5 Flash Preview TTS is our price-performant text-to-speech model, delivering high control and transparency for structured workflows like podcast generation, audiobooks, customer support, and more. Gemini 2.5 Flash rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreviewTts = new ChatModel("gemini-2.5-flash-preview-tts", LLmProviders.Google, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreviewTts"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreviewTts = ModelGemini25FlashPreviewTts;
    
    /// <summary>
    /// Gemini 2.5 Pro Preview TTS is our most powerful text-to-speech model, delivering high control and transparency for structured workflows like podcast generation, audiobooks, customer support, and more. Gemini 2.5 Pro rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreviewTts = new ChatModel("gemini-2.5-pro-preview-tts", LLmProviders.Google, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreviewTts"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreviewTts = ModelGemini25ProPreviewTts;
    
    /// <summary>
    /// Our best model in terms of price-performance, offering well-rounded capabilities. Gemini 2.5 Flash rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreview0520 = new ChatModel("gemini-2.5-flash-preview-05-20", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreview0520"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreview0520 = ModelGemini25FlashPreview0520;
    
    /// <summary>
    /// Our best model in terms of price-performance, offering well-rounded capabilities. Gemini 2.5 Flash rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreview0417 = new ChatModel("gemini-2.5-flash-preview-04-17", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreview0417"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreview0417 = ModelGemini25FlashPreview0417;
    
    /// <summary>
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview0506 = new ChatModel("gemini-2.5-pro-preview-05-06", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview0506"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview0506 = ModelGemini25ProPreview0506;
    
    /// <summary>
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview0325 = new ChatModel("gemini-2.5-pro-preview-03-25", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview0325"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview0325 = ModelGemini25ProPreview0325;
    
    /// <summary>
    /// Gemini 2.0 Flash Preview Image Generation delivers improved image generation features, including generating and editing images conversationally.
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashPreviewImageGeneration = new ChatModel("gemini-2.0-flash-preview-image-generation", LLmProviders.Google, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashPreviewImageGeneration"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashPreviewImageGeneration = ModelGemini2FlashPreviewImageGeneration;
    
    /// <summary>
    /// All known Preview Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini25ProPreview0506,
        ModelGemini25ProPreview0325,
        ModelGemini25FlashPreview0417,
        ModelGemini25FlashPreview0520,
        ModelGemini2FlashPreviewImageGeneration,
        ModelGemini25FlashPreviewTts,
        ModelGemini25ProPreviewTts
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGeminiPreview()
    {

    }
}