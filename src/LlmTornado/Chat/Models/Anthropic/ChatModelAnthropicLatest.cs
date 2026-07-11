using System;
using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Latest models from Anthropic by performance tier. These are pointers to existing models, repointed to newer models
/// as they release; pin a concrete snapshot (e.g. <see cref="ChatModelAnthropicClaude5.Fable"/>) if you need a stable model.
/// </summary>
public class ChatModelAnthropicLatest : IVendorModelClassProvider
{
    /// <summary>
    /// Absolute top-end tier; slowest and most expensive. Currently <c>claude-fable-5</c>.
    /// </summary>
    public static readonly ChatModel ModelMax = ChatModelAnthropicClaude5.ModelFable;

    /// <summary>
    /// <inheritdoc cref="ModelMax"/>
    /// </summary>
    public readonly ChatModel Max = ModelMax;

    /// <summary>
    /// Flagship / frontier tier. Currently <c>claude-opus-4-8</c>.
    /// </summary>
    public static readonly ChatModel ModelLarge = ChatModelAnthropicClaude48.ModelOpus;

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly ChatModel Large = ModelLarge;

    /// <summary>
    /// Balanced intelligence/cost tier. Currently <c>claude-sonnet-5</c>.
    /// </summary>
    public static readonly ChatModel ModelMedium = ChatModelAnthropicClaude5.ModelSonnet;

    /// <summary>
    /// <inheritdoc cref="ModelMedium"/>
    /// </summary>
    public readonly ChatModel Medium = ModelMedium;

    /// <summary>
    /// Cheap, fast, high-volume tier. Currently <c>claude-haiku-4-5-20251001</c>.
    /// </summary>
    public static readonly ChatModel ModelSmall = ChatModelAnthropicClaude45.ModelHaiku251001;

    /// <summary>
    /// <inheritdoc cref="ModelSmall"/>
    /// </summary>
    public readonly ChatModel Small = ModelSmall;

    /// <summary>
    /// Models currently pointed to by the latest tiers of Anthropic.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMax, ModelLarge, ModelMedium, ModelSmall
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicLatest()
    {

    }
}
