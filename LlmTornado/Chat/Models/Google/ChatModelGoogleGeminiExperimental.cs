using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Experimental Gemini class models from Google.
/// Warning: these models break often and shouldn't be used in production environment.
/// </summary>
public class ChatModelGoogleGeminiExperimental : IVendorModelClassProvider
{
    /// <summary>
    /// Reasoning for complex problems, features new thinking capabilities
    /// Released: January 21, 2025
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashThinkingExp250121 = new ChatModel("gemini-2.0-flash-thinking-exp-01-21", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashThinkingExp250121"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashThinkingExp250121 = ModelGemini2FlashThinkingExp250121;
    
    /// <summary>
    /// Currently points to <see cref="ModelGemini2FlashThinkingExp250121"/>.
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashThinkingExp = new ChatModel("gemini-2.0-flash-thinking-exp", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashThinkingExp"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashThinkingExp = ModelGemini2FlashThinkingExp;
    
    /// <summary>
    /// Next generation features, superior speed, native tool use, and multimodal generation
    /// Released: December 11, 2024
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashExp = new ChatModel("gemini-2.0-flash-exp", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashExp"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashExp = ModelGemini2FlashExp;
    
    /// <summary>
    /// All known Experimental Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini2FlashThinkingExp250121,
        ModelGemini2FlashExp,
        ModelGemini2FlashThinkingExp
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGeminiExperimental()
    {

    }
}