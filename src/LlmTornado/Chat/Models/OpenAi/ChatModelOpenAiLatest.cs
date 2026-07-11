using System;
using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Latest models from OpenAI by performance tier. These are pointers to existing models, repointed to newer models
/// as they release; pin a concrete snapshot (e.g. <see cref="ChatModelOpenAiGpt56.V56Sol"/>) if you need a stable model.
/// </summary>
public class ChatModelOpenAiLatest : IVendorModelClassProvider
{
    /// <summary>
    /// Absolute top-end tier; slowest and most expensive. Currently <c>gpt-5.5-pro</c> — the newest dedicated pro
    /// slug, as GPT-5.6's pro mode is a request setting (<c>reasoning.mode: "pro"</c>), not a separate model.
    /// </summary>
    public static readonly ChatModel ModelMax = ChatModelOpenAiGpt55.ModelV55Pro;

    /// <summary>
    /// <inheritdoc cref="ModelMax"/>
    /// </summary>
    public readonly ChatModel Max = ModelMax;

    /// <summary>
    /// Flagship / frontier tier. Currently <c>gpt-5.6-sol</c>.
    /// </summary>
    public static readonly ChatModel ModelLarge = ChatModelOpenAiGpt56.ModelV56Sol;

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly ChatModel Large = ModelLarge;

    /// <summary>
    /// Balanced intelligence/cost tier. Currently <c>gpt-5.6-terra</c>.
    /// </summary>
    public static readonly ChatModel ModelMedium = ChatModelOpenAiGpt56.ModelV56Terra;

    /// <summary>
    /// <inheritdoc cref="ModelMedium"/>
    /// </summary>
    public readonly ChatModel Medium = ModelMedium;

    /// <summary>
    /// Cheap, fast, high-volume tier. Currently <c>gpt-5.6-luna</c>.
    /// </summary>
    public static readonly ChatModel ModelSmall = ChatModelOpenAiGpt56.ModelV56Luna;

    /// <summary>
    /// <inheritdoc cref="ModelSmall"/>
    /// </summary>
    public readonly ChatModel Small = ModelSmall;

    /// <summary>
    /// Models currently pointed to by the latest tiers of OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMax, ModelLarge, ModelMedium, ModelSmall
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelOpenAiLatest()
    {

    }
}
