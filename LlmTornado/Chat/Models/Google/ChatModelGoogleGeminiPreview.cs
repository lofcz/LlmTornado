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
    /// All known Preview Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini25ProPreview0506,
        ModelGemini25ProPreview0325,
        ModelGemini25FlashPreview0417
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGeminiPreview()
    {

    }
}