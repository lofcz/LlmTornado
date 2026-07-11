using System;
using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Latest models from Google by performance tier. These are pointers to existing models, repointed to newer models
/// as they release; pin a concrete snapshot (e.g. <see cref="ChatModelGoogleGemini.Gemini35Flash"/>) if you need a stable model.
/// </summary>
public class ChatModelGoogleLatest : IVendorModelClassProvider
{
    /// <summary>
    /// Absolute top-end tier. Google currently ships no API model above the Pro tier (no Ultra/Deep Think slug),
    /// so Max points to the same model as <see cref="ModelLarge"/>; it will be split out when such a model releases.
    /// Currently <c>gemini-3.1-pro-preview</c>.
    /// </summary>
    public static readonly ChatModel ModelMax = ChatModelGoogleGeminiPreview.ModelGemini31ProPreview;

    /// <summary>
    /// <inheritdoc cref="ModelMax"/>
    /// </summary>
    public readonly ChatModel Max = ModelMax;

    /// <summary>
    /// Flagship / frontier tier. Currently <c>gemini-3.1-pro-preview</c> — no stable Gemini-3-class Pro exists yet.
    /// </summary>
    public static readonly ChatModel ModelLarge = ChatModelGoogleGeminiPreview.ModelGemini31ProPreview;

    /// <summary>
    /// <inheritdoc cref="ModelLarge"/>
    /// </summary>
    public readonly ChatModel Large = ModelLarge;

    /// <summary>
    /// Balanced intelligence/cost tier. Currently <c>gemini-3.5-flash</c>.
    /// </summary>
    public static readonly ChatModel ModelMedium = ChatModelGoogleGemini.ModelGemini35Flash;

    /// <summary>
    /// <inheritdoc cref="ModelMedium"/>
    /// </summary>
    public readonly ChatModel Medium = ModelMedium;

    /// <summary>
    /// Cheap, fast, high-volume tier. Currently <c>gemini-3.1-flash-lite</c>.
    /// </summary>
    public static readonly ChatModel ModelSmall = ChatModelGoogleGemini.ModelGemini31FlashLite;

    /// <summary>
    /// <inheritdoc cref="ModelSmall"/>
    /// </summary>
    public readonly ChatModel Small = ModelSmall;

    /// <summary>
    /// Models currently pointed to by the latest tiers of Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMax, ModelMedium, ModelSmall
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleLatest()
    {

    }
}
