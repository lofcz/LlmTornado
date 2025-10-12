using System;
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
    /// Gemini 2.5 Pro is our state-of-the-art thinking model, capable of reasoning over complex problems in code, math, and STEM, as well as analyzing large datasets, codebases, and documents using long context.
    /// </summary>
    public static readonly ChatModel ModelGemini25Pro = new ChatModel("gemini-2.5-pro", LLmProviders.Google, 1_000_000)
    {
        ReasoningTokensMin = 128,
        ReasoningTokensMax = 32_768,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25Pro"/>
    /// </summary>
    public readonly ChatModel Gemini25Pro = ModelGemini25Pro;
    
    /// <summary>
    /// gemini-2.5-flash-preview-09-2025
    /// </summary>
    public static readonly ChatModel ModelGeminiFlashLatest = new ChatModel("gemini-flash-latest", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGeminiFlashLatest"/>
    /// </summary>
    public readonly ChatModel GeminiFlashLatest = ModelGeminiFlashLatest;
    
    /// <summary>
    /// Our best model in terms of price-performance, offering well-rounded capabilities. 2.5 Flash is best for large scale processing, low-latency, high volume tasks that require thinking, and agentic use cases.
    /// </summary>
    public static readonly ChatModel ModelGemini25Flash = new ChatModel("gemini-2.5-flash", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25Flash"/>
    /// </summary>
    public readonly ChatModel Gemini25Flash = ModelGemini25Flash;
    
    /// <summary>
    /// gemini-2.5-flash-lite-preview-09-2025
    /// </summary>
    public static readonly ChatModel ModelGeminiFlashLiteLatest = new ChatModel("gemini-flash-lite-latest", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 512,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ 0, -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGeminiFlashLiteLatest"/>
    /// </summary>
    public readonly ChatModel GeminiFlashLiteLatest = ModelGeminiFlashLiteLatest;
    
    /// <summary>
    /// A Gemini 2.5 Flash model optimized for cost-efficiency and high throughput.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLite = new ChatModel("gemini-2.5-flash-lite", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 512,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ 0, -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLite"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLite = ModelGemini25FlashLite;

    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks (stable).
    /// </summary>
    public static readonly ChatModel ModelGemini2Flash001 = new ChatModel("gemini-2.0-flash-001", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2Flash001"/>
    /// </summary>
    public readonly ChatModel Gemini2Flash001 = ModelGemini2Flash001;
    
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks (latest).
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashLatest = new ChatModel("gemini-2.0-flash", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashLatest"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashLatest = ModelGemini2FlashLatest;
    
    /// <summary>
    /// A Gemini 2.0 Flash model optimized for cost efficiency and low latency (stable).
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashLite001 = new ChatModel("gemini-2.0-flash-lite-001", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashLite001"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashLite001 = ModelGemini2FlashLite001;
    
    /// <summary>
    /// A Gemini 2.0 Flash model optimized for cost efficiency and low latency (latest).
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashLiteLatest = new ChatModel("gemini-2.0-flash-lite", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashLiteLatest"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashLiteLatest = ModelGemini2FlashLiteLatest;
    
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
    /// Gemini 2.5 Flash Image is our latest, fastest, and most efficient natively multimodal model that lets you generate and edit images conversationally.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashImage = new ChatModel("gemini-2.5-flash-image", LLmProviders.Google, 32_768);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashImage"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashImage = ModelGemini25FlashImage;
    
    /// <summary>
    /// All known Gemini models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelGemini15FlashLatest, ModelGemini15Flash, ModelGemini15Flash001, ModelGemini15Flash002, ModelGemini15ProLatest, 
        ModelGemini15Pro, ModelGemini15Pro001, ModelGemini15Pro002, ModelGemini15Flash8B, ModelGemini15Flash8BLatest, ModelGemini2Flash001,
        ModelGemini2FlashLatest, ModelGemini2FlashLite001, ModelGemini2FlashLiteLatest, ModelGemini25Pro, ModelGemini25Flash, 
        ModelGemini25FlashLite, ModelGeminiFlashLiteLatest, ModelGeminiFlashLatest, ModelGemini25FlashImage
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGemini()
    {

    }
}