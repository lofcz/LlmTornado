using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Model aliases provided by Blablador for easy model selection.
/// These aliases point to current recommended models and may change over time.
/// </summary>
public class ChatModelBlabladorAliases : IVendorModelClassProvider
{
    /// <summary>
    /// The largest model available. As of May 2025, points to Qwen3 30B A3B.
    /// Most accurate but also slowest.
    /// </summary>
    public static readonly ChatModel ModelAliasLarge = new ChatModel("alias-large", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelAliasLarge"/>
    /// </summary>
    public readonly ChatModel Large = ModelAliasLarge;
    
    /// <summary>
    /// High throughput model for fast responses. As of December 2024, points to Ministral-8B-Instruct-2410.
    /// </summary>
    public static readonly ChatModel ModelAliasFast = new ChatModel("alias-fast", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelAliasFast"/>
    /// </summary>
    public readonly ChatModel Fast = ModelAliasFast;
    
    /// <summary>
    /// Model for embeddings. As of March 2024, points to GritLM-7B.
    /// </summary>
    public static readonly ChatModel ModelAliasEmbeddings = new ChatModel("alias-embeddings", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelAliasEmbeddings"/>
    /// </summary>
    public readonly ChatModel Embeddings = ModelAliasEmbeddings;
    
    /// <summary>
    /// Model for code generation. As of September 2025, points to Qwen3-Coder-30B-A3B-Instruct.
    /// </summary>
    public static readonly ChatModel ModelAliasCode = new ChatModel("alias-code", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelAliasCode"/>
    /// </summary>
    public readonly ChatModel Code = ModelAliasCode;
    
    /// <summary>
    /// Model for reasoning tasks. As of May 2024, points to Qwen3 30B A3B with special reasoning prompts.
    /// </summary>
    public static readonly ChatModel ModelAliasReasoning = new ChatModel("alias-reasoning", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelAliasReasoning"/>
    /// </summary>
    public readonly ChatModel Reasoning = ModelAliasReasoning;
    
    /// <summary>
    /// All known Blablador alias models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelAliasLarge, ModelAliasFast, ModelAliasEmbeddings, ModelAliasCode, ModelAliasReasoning
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelBlabladorAliases()
    {

    }
}

