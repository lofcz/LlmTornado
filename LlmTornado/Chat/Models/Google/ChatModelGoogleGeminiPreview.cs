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
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini2ProPreview0325 = new ChatModel("gemini-2.5-pro-preview-03-25", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2ProPreview0325"/>
    /// </summary>
    public readonly ChatModel Gemini2ProPreview0325 = ModelGemini2ProPreview0325;
    
    /// <summary>
    /// All known Preview Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini2ProPreview0325
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGeminiPreview()
    {

    }
}