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
    public static readonly ChatModel ModelGemini15FlashLatest = new ChatModel("gemini-1.5-flash-latest", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15FlashLatest"/>
    /// </summary>
    public readonly ChatModel Gemini15FlashLatest = ModelGemini15FlashLatest;
    
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks.
    /// </summary>
    public static readonly ChatModel ModelGemini15Flash = new ChatModel("gemini-1.5-flash", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash"/>
    /// </summary>
    public readonly ChatModel Gemini15Flash = ModelGemini15Flash;
    
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks.
    /// </summary>
    public static readonly ChatModel ModelGemini15Flash001 = new ChatModel("gemini-1.5-flash-001", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash001"/>
    /// </summary>
    public readonly ChatModel Gemini15Flash001 = ModelGemini15Flash001;
    
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks.
    /// </summary>
    public static readonly ChatModel ModelGemini15Flash002 = new ChatModel("gemini-1.5-flash-002", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash002"/>
    /// </summary>
    public readonly ChatModel Gemini15Flash002 = ModelGemini15Flash002;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    public static readonly ChatModel ModelGemini15ProLatest = new ChatModel("gemini-1.5-pro-latest", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15ProLatest"/>
    /// </summary>
    public readonly ChatModel Gemini15ProLatest = ModelGemini15ProLatest;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    public static readonly ChatModel ModelGemini15Pro = new ChatModel("gemini-1.5-pro", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro"/>
    /// </summary>
    public readonly ChatModel Gemini15Pro = ModelGemini15Pro;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    public static readonly ChatModel ModelGemini15Pro001 = new ChatModel("gemini-1.5-pro-001", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro001"/>
    /// </summary>
    public readonly ChatModel Gemini15Pro001 = ModelGemini15Pro001;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    public static readonly ChatModel ModelGemini15Pro002 = new ChatModel("gemini-1.5-pro-002", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro002"/>
    /// </summary>
    public readonly ChatModel Gemini15Pro002 = ModelGemini15Pro002;
    
    /// <summary>
    /// Gemini 1.5 Flash-8B is a small model designed for lower intelligence tasks.
    /// </summary>
    public static readonly ChatModel ModelGemini15Flash8BLatest = new ChatModel("gemini-1.5-flash-8b-latest", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash8BLatest"/>
    /// </summary>
    public readonly ChatModel Gemini15Flash8BLatest = ModelGemini15Flash8BLatest;
    
    /// <summary>
    /// Gemini 1.5 Flash-8B is a small model designed for lower intelligence tasks.
    /// </summary>
    public static readonly ChatModel ModelGemini15Flash8B = new ChatModel("gemini-1.5-flash-8b", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash8B"/>
    /// </summary>
    public readonly ChatModel Gemini15Flash8B = ModelGemini15Flash8B;
    
    /// <summary>
    /// All known Gemini models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGemini15FlashLatest,
        ModelGemini15Flash,
        ModelGemini15Flash001,
        ModelGemini15Flash002,
        
        ModelGemini15ProLatest,
        ModelGemini15Pro,
        ModelGemini15Pro001,
        ModelGemini15Pro002,
        
        ModelGemini15Flash8B,
        ModelGemini15Flash8BLatest
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGemini()
    {

    }
}