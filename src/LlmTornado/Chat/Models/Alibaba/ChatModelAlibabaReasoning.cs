using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba reasoning models - advanced models for complex multi-step reasoning tasks.
/// </summary>
public class ChatModelAlibabaReasoning : IVendorModelClassProvider
{
    /// <summary>
    /// QVQ-Max - Tongyi Qianwen QVQ visual reasoning model with chain-of-thought output
    /// </summary>
    public static readonly ChatModel ModelQvqMax = new ChatModel("qvq-max", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQvqMax"/>
    /// </summary>
    public readonly ChatModel QvqMax = ModelQvqMax;

    /// <summary>
    /// QVQ-Max-Latest - Dynamically updated version
    /// </summary>
    public static readonly ChatModel ModelQvqMaxLatest = new ChatModel("qvq-max-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQvqMaxLatest"/>
    /// </summary>
    public readonly ChatModel QvqMaxLatest = ModelQvqMaxLatest;

    /// <summary>
    /// QVQ-Max-2025-03-25 - Historical snapshot from March 25, 2025
    /// </summary>
    public static readonly ChatModel ModelQvqMax20250325 = new ChatModel("qvq-max-2025-03-25", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQvqMax20250325"/>
    /// </summary>
    public readonly ChatModel QvqMax20250325 = ModelQvqMax20250325;

    /// <summary>
    /// All known reasoning models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelQvqMax, ModelQvqMaxLatest, ModelQvqMax20250325
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAlibabaReasoning()
    {
    }
}
