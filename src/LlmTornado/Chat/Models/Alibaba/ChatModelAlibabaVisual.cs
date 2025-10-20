using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba visual models - image and video understanding capabilities.
/// </summary>
public class ChatModelAlibabaVisual : IVendorModelClassProvider
{
    /// <summary>
    /// Qwen3-VL-Plus - World-leading visual agent capabilities
    /// </summary>
    public static readonly ChatModel ModelQwen3VlPlus = new ChatModel("qwen3-vl-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3VlPlus"/>
    /// </summary>
    public readonly ChatModel Qwen3VlPlus = ModelQwen3VlPlus;

    /// <summary>
    /// Qwen3-VL-Flash - Small-scale visual understanding model
    /// </summary>
    public static readonly ChatModel ModelQwen3VlFlash = new ChatModel("qwen3-vl-flash", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3VlFlash"/>
    /// </summary>
    public readonly ChatModel Qwen3VlFlash = ModelQwen3VlFlash;

    /// <summary>
    /// Qwen-VL-Max - Most capable large visual language model
    /// </summary>
    public static readonly ChatModel ModelQwenVlMax = new ChatModel("qwen-vl-max", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlMax"/>
    /// </summary>
    public readonly ChatModel QwenVlMax = ModelQwenVlMax;

    /// <summary>
    /// Qwen-VL-Plus - Enhanced large visual language model
    /// </summary>
    public static readonly ChatModel ModelQwenVlPlus = new ChatModel("qwen-vl-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlPlus"/>
    /// </summary>
    public readonly ChatModel QwenVlPlus = ModelQwenVlPlus;

    /// <summary>
    /// Qwen-VL-OCR - Large OCR recognition model
    /// </summary>
    public static readonly ChatModel ModelQwenVlOcr = new ChatModel("qwen-vl-ocr", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlOcr"/>
    /// </summary>
    public readonly ChatModel QwenVlOcr = ModelQwenVlOcr;

    /// <summary>
    /// Qwen-VL-Plus-Latest - Dynamically updated version
    /// </summary>
    public static readonly ChatModel ModelQwenVlPlusLatest = new ChatModel("qwen-vl-plus-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlPlusLatest"/>
    /// </summary>
    public readonly ChatModel QwenVlPlusLatest = ModelQwenVlPlusLatest;

    /// <summary>
    /// Qwen-VL-Max-Latest - Dynamically updated version
    /// </summary>
    public static readonly ChatModel ModelQwenVlMaxLatest = new ChatModel("qwen-vl-max-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlMaxLatest"/>
    /// </summary>
    public readonly ChatModel QwenVlMaxLatest = ModelQwenVlMaxLatest;

    /// <summary>
    /// Qwen3-VL-Flash-2025-10-15 - Snapshot from October 15, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3VlFlash20251015 = new ChatModel("qwen3-vl-flash-2025-10-15", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3VlFlash20251015"/>
    /// </summary>
    public readonly ChatModel Qwen3VlFlash20251015 = ModelQwen3VlFlash20251015;

    /// <summary>
    /// Qwen3-VL-Plus-2025-09-23 - Snapshot from September 23, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3VlPlus20250923 = new ChatModel("qwen3-vl-plus-2025-09-23", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3VlPlus20250923"/>
    /// </summary>
    public readonly ChatModel Qwen3VlPlus20250923 = ModelQwen3VlPlus20250923;

    /// <summary>
    /// Qwen-VL-Max-2025-08-13 - Snapshot from August 13, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenVlMax20250813 = new ChatModel("qwen-vl-max-2025-08-13", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlMax20250813"/>
    /// </summary>
    public readonly ChatModel QwenVlMax20250813 = ModelQwenVlMax20250813;

    /// <summary>
    /// Qwen-VL-Max-2025-04-08 - Snapshot from April 8, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenVlMax20250408 = new ChatModel("qwen-vl-max-2025-04-08", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlMax20250408"/>
    /// </summary>
    public readonly ChatModel QwenVlMax20250408 = ModelQwenVlMax20250408;

    /// <summary>
    /// Qwen-VL-Plus-2025-08-15 - Snapshot from August 15, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenVlPlus20250815 = new ChatModel("qwen-vl-plus-2025-08-15", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlPlus20250815"/>
    /// </summary>
    public readonly ChatModel QwenVlPlus20250815 = ModelQwenVlPlus20250815;

    /// <summary>
    /// Qwen-VL-Plus-2025-05-07 - Snapshot from May 7, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenVlPlus20250507 = new ChatModel("qwen-vl-plus-2025-05-07", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlPlus20250507"/>
    /// </summary>
    public readonly ChatModel QwenVlPlus20250507 = ModelQwenVlPlus20250507;

    /// <summary>
    /// Qwen-VL-Plus-2025-01-25 - Snapshot from January 25, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenVlPlus20250125 = new ChatModel("qwen-vl-plus-2025-01-25", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlPlus20250125"/>
    /// </summary>
    public readonly ChatModel QwenVlPlus20250125 = ModelQwenVlPlus20250125;

    /// <summary>
    /// Qwen3-VL-30B-A3B-Thinking - Thinking edition of second-largest MoE model
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl30BA3BThinking = new ChatModel("qwen3-vl-30b-a3b-thinking", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl30BA3BThinking"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl30BA3BThinking = ModelQwen3Vl30BA3BThinking;

    /// <summary>
    /// Qwen3-VL-30B-A3B-Instruct - Second-largest MoE model
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl30BA3BInstruct = new ChatModel("qwen3-vl-30b-a3b-instruct", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl30BA3BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl30BA3BInstruct = ModelQwen3Vl30BA3BInstruct;

    /// <summary>
    /// Qwen3-VL-8B-Thinking - 8B Dense thinking edition
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl8BThinking = new ChatModel("qwen3-vl-8b-thinking", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl8BThinking"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl8BThinking = ModelQwen3Vl8BThinking;

    /// <summary>
    /// Qwen3-VL-8B-Instruct - 8B Dense model
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl8BInstruct = new ChatModel("qwen3-vl-8b-instruct", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl8BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl8BInstruct = ModelQwen3Vl8BInstruct;

    /// <summary>
    /// Qwen3-VL-235B-A22B-Instruct - Largest MoE model
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl235BA22BInstruct = new ChatModel("qwen3-vl-235b-a22b-instruct", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl235BA22BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl235BA22BInstruct = ModelQwen3Vl235BA22BInstruct;

    /// <summary>
    /// Qwen3-VL-235B-A22B-Thinking - Largest MoE thinking edition
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl235BA22BThinking = new ChatModel("qwen3-vl-235b-a22b-thinking", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl235BA22BThinking"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl235BA22BThinking = ModelQwen3Vl235BA22BThinking;

    /// <summary>
    /// Qwen2.5-VL-72B-Instruct - Most powerful open-source VL model
    /// </summary>
    public static readonly ChatModel ModelQwen2_5Vl72BInstruct = new ChatModel("qwen2.5-vl-72b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_5Vl72BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_5Vl72BInstruct = ModelQwen2_5Vl72BInstruct;

    /// <summary>
    /// Qwen2.5-VL-32b-instruct - 32B version with human-preferred responses
    /// </summary>
    public static readonly ChatModel ModelQwen2_5Vl32bInstruct = new ChatModel("qwen2.5-vl-32b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_5Vl32bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_5Vl32bInstruct = ModelQwen2_5Vl32bInstruct;

    /// <summary>
    /// Qwen2.5-VL-7B-Instruct - 7B version with balanced performance
    /// </summary>
    public static readonly ChatModel ModelQwen2_5Vl7BInstruct = new ChatModel("qwen2.5-vl-7b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_5Vl7BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_5Vl7BInstruct = ModelQwen2_5Vl7BInstruct;

    /// <summary>
    /// Qwen2.5-VL-3B-Instruct - 3B version suitable for mobile devices
    /// </summary>
    public static readonly ChatModel ModelQwen2_5Vl3BInstruct = new ChatModel("qwen2.5-vl-3b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_5Vl3BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_5Vl3BInstruct = ModelQwen2_5Vl3BInstruct;

    /// <summary>
    /// All known visual models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelQwen3VlPlus, ModelQwen3VlFlash, ModelQwenVlMax, ModelQwenVlPlus, ModelQwenVlOcr, ModelQwenVlPlusLatest,
        ModelQwenVlMaxLatest, ModelQwen3VlFlash20251015, ModelQwen3VlPlus20250923, ModelQwenVlMax20250813,
        ModelQwenVlMax20250408, ModelQwenVlPlus20250815, ModelQwenVlPlus20250507, ModelQwenVlPlus20250125,
        ModelQwen3Vl30BA3BThinking, ModelQwen3Vl30BA3BInstruct, ModelQwen3Vl8BThinking, ModelQwen3Vl8BInstruct,
        ModelQwen3Vl235BA22BInstruct, ModelQwen3Vl235BA22BThinking, ModelQwen2_5Vl72BInstruct, ModelQwen2_5Vl32bInstruct,
        ModelQwen2_5Vl7BInstruct, ModelQwen2_5Vl3BInstruct
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAlibabaVisual()
    {
    }
}
