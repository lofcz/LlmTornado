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
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini2ProExp0325 = new ChatModel("gemini-2.5-pro-exp-03-25", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2ProExp0325"/>
    /// </summary>
    public readonly ChatModel Gemini2ProExp0325 = ModelGemini2ProExp0325;
    
    /// <summary>
    /// An experimental public preview version of Gemini 2.0 Flash capable of image generation.
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashImageGeneration = new ChatModel("gemini-2.0-flash-exp-image-generation", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashImageGeneration"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashImageGeneration = ModelGemini2FlashImageGeneration;
    
    /// <summary>
    /// An experimental public preview version of Gemini 2.0 Pro.
    /// </summary>
    public static readonly ChatModel ModelGemini2ProExp250205 = new ChatModel("gemini-2.0-pro-exp-02-05", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2ProExp250205"/>
    /// </summary>
    public readonly ChatModel GeminiModelGemini2ProExp250205 = ModelGemini2ProExp250205;
    
    /// <summary>
    /// LearnLM 1.5 Pro Experimental
    /// </summary>
    public static readonly ChatModel ModelLearnLlm15ProExperimental = new ChatModel("learnlm-1.5-pro-experimental", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLearnLlm15ProExperimental"/>
    /// </summary>
    public readonly ChatModel LearnLlm15ProExperimental = ModelLearnLlm15ProExperimental;
    
    /// <summary>
    /// Reasoning for complex problems, features new thinking capabilities
    /// Released: January 21, 2025
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashThinkingExp250121 = new ChatModel("gemini-2.0-flash-thinking-exp-01-21", LLmProviders.Google, 1_000_000);
    
    
    /// <summary>
    /// All known Experimental Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini2FlashThinkingExp250121,
        ModelLearnLlm15ProExperimental,
        ModelGemini2ProExp250205,
        ModelGemini2FlashImageGeneration,
        ModelGemini2ProExp0325
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGeminiExperimental()
    {

    }
}