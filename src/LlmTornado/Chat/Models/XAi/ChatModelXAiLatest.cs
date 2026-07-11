using System;
using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Latest models from xAI by performance tier. These are pointers to existing models, repointed to newer models
/// as they release; pin a concrete snapshot (e.g. <see cref="ChatModelXAiGrok45.V45"/>) if you need a stable model.
/// </summary>
public class ChatModelXAiLatest : IVendorModelClassProvider
{
    /// <summary>
    /// Absolute top-end tier. xAI currently ships no API model above the flagship tier (no Heavy slug),
    /// so Max points to the same model as <see cref="ModelLarge"/>; it will be split out when such a model releases.
    /// Currently <c>grok-4.5</c>.
    /// </summary>
    public static readonly ChatModel ModelMax = ChatModelXAiGrok45.ModelV45;

    /// <summary>
    /// <inheritdoc cref="ModelMax"/>
    /// </summary>
    public readonly ChatModel Max = ModelMax;

    /// <summary>
    /// Flagship / frontier tier. Currently <c>grok-4.5</c>.
    /// </summary>
    public static readonly ChatModel ModelLarge = ChatModelXAiGrok45.ModelV45;

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly ChatModel Large = ModelLarge;

    /// <summary>
    /// Balanced intelligence/cost tier. Currently <c>grok-4-1-fast-reasoning</c>.
    /// </summary>
    public static readonly ChatModel ModelMedium = ChatModelXAiGrok41.ModelV41FastReasoning;

    /// <summary>
    /// <inheritdoc cref="ModelMedium"/>
    /// </summary>
    public readonly ChatModel Medium = ModelMedium;

    /// <summary>
    /// Cheap, fast, high-volume tier. Currently <c>grok-4-1-fast-non-reasoning</c>.
    /// </summary>
    public static readonly ChatModel ModelSmall = ChatModelXAiGrok41.ModelV41FastNonReasoning;

    /// <summary>
    /// <inheritdoc cref="ModelSmall"/>
    /// </summary>
    public readonly ChatModel Small = ModelSmall;

    /// <summary>
    /// Models currently pointed to by the latest tiers of xAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMax, ModelMedium, ModelSmall
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelXAiLatest()
    {

    }
}
