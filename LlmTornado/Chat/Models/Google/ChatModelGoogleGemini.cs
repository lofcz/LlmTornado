using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Gemini class models from Google.
/// </summary>
public class ChatModelGoogleGemini : IVendorModelClassProvider
{
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks.
    /// </summary>
    public static readonly ChatModel ModelGemini15Flash = new ChatModel("gemini-1.5-flash", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash"/>
    /// </summary>
    public readonly ChatModel Gemini15Flash = ModelGemini15Flash;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem solving, data extraction and generation.
    /// </summary>
    public static readonly ChatModel ModelGemini15Pro = new ChatModel("gemini-1.5-pro", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro"/>
    /// </summary>
    public readonly ChatModel Gemini15Pro = ModelGemini15Pro;
    
    /// <summary>
    /// All known Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini15Flash,
        ModelGemini15Pro
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGemini()
    {

    }
}