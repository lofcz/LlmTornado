using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Aya class models from Anthropic.
/// </summary>
public class ChatModelCohereAya : IVendorModelClassProvider
{
    /// <summary>
    /// Aya Vision is a state-of-the-art multimodal model excelling at a variety of critical benchmarks for language, text, and image capabilities. This 8 billion parameter variant is focused on low latency and best-in-class performance. Supports 23 languages.
    /// </summary>
    public static readonly ChatModel ModelVision8B = new ChatModel("c4ai-aya-vision-8b", LLmProviders.Cohere, 16_384);
    
    /// <summary>
    /// <inheritdoc cref="ModelVision8B"/>
    /// </summary>
    public readonly ChatModel Vision8B = ModelVision8B;
    
    /// <summary>
    /// Aya Vision is a state-of-the-art multimodal model excelling at a variety of critical benchmarks for language, text, and image capabilities. Serves 23 languages. This 32 billion parameter variant is focused on state-of-art multilingual performance. Supports 23 languages.
    /// </summary>
    public static readonly ChatModel ModelVision32B = new ChatModel("c4ai-aya-vision-32b", LLmProviders.Cohere, 16_384);
    
    /// <summary>
    /// <inheritdoc cref="ModelVision32B"/>
    /// </summary>
    public readonly ChatModel Vision32B = ModelVision32B;
    
    /// <summary>
    /// Aya Expanse is a highly performant 8B multilingual model, designed to rival monolingual performance through innovations in instruction tuning with data arbitrage, preference training, and model merging. Serves 23 languages.
    /// </summary>
    public static readonly ChatModel ModelExpanse8B = new ChatModel("c4ai-aya-expanse-8b", LLmProviders.Cohere, 8_196);
    
    /// <summary>
    /// <inheritdoc cref="ModelExpanse8B"/>
    /// </summary>
    public readonly ChatModel Expanse8B = ModelExpanse8B;

    /// <summary>
    /// Aya Expanse is a highly performant 32B multilingual model, designed to rival monolingual performance through innovations in instruction tuning with data arbitrage, preference training, and model merging. Serves 23 languages.
    /// </summary>
    public static readonly ChatModel ModelExpanse32B = new ChatModel("c4ai-aya-expanse-32b", LLmProviders.Cohere, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelExpanse32B"/>
    /// </summary>
    public readonly ChatModel Expanse32B = ModelExpanse32B;
    
    /// <summary>
    /// All known Aya models from Cohere.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelExpanse8B, ModelExpanse32B, ModelVision8B, ModelVision32B]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelCohereAya()
    {

    }
}