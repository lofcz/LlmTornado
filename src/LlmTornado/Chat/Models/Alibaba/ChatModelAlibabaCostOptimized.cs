using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba cost-optimized models - fast, low-cost models with long context.
/// </summary>
public class ChatModelAlibabaCostOptimized : IVendorModelClassProvider
{
    /// <summary>
    /// Qwen3-Coder-Flash - Code generation model with tool interaction
    /// </summary>
    public static readonly ChatModel ModelQwen3CoderFlash = new ChatModel("qwen3-coder-flash", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3CoderFlash"/>
    /// </summary>
    public readonly ChatModel Qwen3CoderFlash = ModelQwen3CoderFlash;

    /// <summary>
    /// Qwen-Flash - Fast and low-cost model for simple tasks
    /// </summary>
    public static readonly ChatModel ModelQwenFlash = new ChatModel("qwen-flash", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenFlash"/>
    /// </summary>
    public readonly ChatModel QwenFlash = ModelQwenFlash;

    /// <summary>
    /// Qwen-MT-Turbo - Fast translation model with 92 languages
    /// </summary>
    public static readonly ChatModel ModelQwenMtTurbo = new ChatModel("qwen-mt-turbo", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenMtTurbo"/>
    /// </summary>
    public readonly ChatModel QwenMtTurbo = ModelQwenMtTurbo;

    /// <summary>
    /// Qwen-Flash-2025-07-28 - Snapshot from July 28, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenFlash20250728 = new ChatModel("qwen-flash-2025-07-28", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenFlash20250728"/>
    /// </summary>
    public readonly ChatModel QwenFlash20250728 = ModelQwenFlash20250728;

    /// <summary>
    /// Qwen3-Coder-Flash-2025-07-28 - Snapshot from July 28, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3CoderFlash20250728 = new ChatModel("qwen3-coder-flash-2025-07-28", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3CoderFlash20250728"/>
    /// </summary>
    public readonly ChatModel Qwen3CoderFlash20250728 = ModelQwen3CoderFlash20250728;

    /// <summary>
    /// Qwen3-Coder-30B-A3B-Instruct - SOTA coding performance for smaller scale
    /// </summary>
    public static readonly ChatModel ModelQwen3Coder30BA3BInstruct = new ChatModel("qwen3-coder-30b-a3b-instruct", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Coder30BA3BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Coder30BA3BInstruct = ModelQwen3Coder30BA3BInstruct;

    /// <summary>
    /// Qwen3-30B-A3B-Instruct-2507 - Open-source model from July 2025
    /// </summary>
    public static readonly ChatModel ModelQwen330BA3BInstruct2507 = new ChatModel("qwen3-30b-a3b-instruct-2507", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330BA3BInstruct2507"/>
    /// </summary>
    public readonly ChatModel Qwen330BA3BInstruct2507 = ModelQwen330BA3BInstruct2507;

    /// <summary>
    /// Qwen3-30B-A3B-Thinking-2507 - Open-source reasoning model from July 2025
    /// </summary>
    public static readonly ChatModel ModelQwen330BA3BThinking2507 = new ChatModel("qwen3-30b-a3b-thinking-2507", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330BA3BThinking2507"/>
    /// </summary>
    public readonly ChatModel Qwen330BA3BThinking2507 = ModelQwen330BA3BThinking2507;

    /// <summary>
    /// Qwen3-30B-A3B - Hybrid reasoning model
    /// </summary>
    public static readonly ChatModel ModelQwen330BA3B = new ChatModel("qwen3-30b-a3b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330BA3B"/>
    /// </summary>
    public readonly ChatModel Qwen330BA3B = ModelQwen330BA3B;

    /// <summary>
    /// Qwen3-14B - Hybrid reasoning model with SOTA performance
    /// </summary>
    public static readonly ChatModel ModelQwen314B = new ChatModel("qwen3-14b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen314B"/>
    /// </summary>
    public readonly ChatModel Qwen314B = ModelQwen314B;

    /// <summary>
    /// Qwen3-8B - Hybrid reasoning model with SOTA performance
    /// </summary>
    public static readonly ChatModel ModelQwen38B = new ChatModel("qwen3-8b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen38B"/>
    /// </summary>
    public readonly ChatModel Qwen38B = ModelQwen38B;

    /// <summary>
    /// Qwen3-4B - Hybrid reasoning model with SOTA performance
    /// </summary>
    public static readonly ChatModel ModelQwen34B = new ChatModel("qwen3-4b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen34B"/>
    /// </summary>
    public readonly ChatModel Qwen34B = ModelQwen34B;

    /// <summary>
    /// Qwen3-1.7B - Hybrid reasoning model with enhanced user experience
    /// </summary>
    public static readonly ChatModel ModelQwen31_7B = new ChatModel("qwen3-1.7b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen31_7B"/>
    /// </summary>
    public readonly ChatModel Qwen31_7B = ModelQwen31_7B;

    /// <summary>
    /// Qwen3-0.6B - Hybrid reasoning model with enhanced capabilities
    /// </summary>
    public static readonly ChatModel ModelQwen30_6B = new ChatModel("qwen3-0.6b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen30_6B"/>
    /// </summary>
    public readonly ChatModel Qwen30_6B = ModelQwen30_6B;

    /// <summary>
    /// All known cost-optimized models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelQwen3CoderFlash, ModelQwenFlash, ModelQwenMtTurbo, ModelQwenFlash20250728, ModelQwen3CoderFlash20250728,
        ModelQwen3Coder30BA3BInstruct, ModelQwen330BA3BInstruct2507, ModelQwen330BA3BThinking2507, ModelQwen330BA3B,
        ModelQwen314B, ModelQwen38B, ModelQwen34B, ModelQwen31_7B, ModelQwen30_6B
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAlibabaCostOptimized()
    {
    }
}
