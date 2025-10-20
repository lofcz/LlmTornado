using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba (DashScope) model provider.
/// </summary>
public class ChatModelAlibaba : BaseVendorModelProvider
{
    /// <summary>
    /// Provider.
    /// </summary>
    public override LLmProviders Provider => LLmProviders.Alibaba;

    /// <summary>
    /// Flagship models - highest performance, most powerful models.
    /// </summary>
    public readonly ChatModelAlibabaFlagship Flagship = new ChatModelAlibabaFlagship();

    /// <summary>
    /// Cost-optimized models - fast, low-cost models with long context.
    /// </summary>
    public readonly ChatModelAlibabaCostOptimized CostOptimized = new ChatModelAlibabaCostOptimized();

    /// <summary>
    /// Visual models - image and video understanding capabilities.
    /// </summary>
    public readonly ChatModelAlibabaVisual Visual = new ChatModelAlibabaVisual();

    /// <summary>
    /// Multimodal models - text, images, audio, and video processing.
    /// </summary>
    public readonly ChatModelAlibabaMultimodal Multimodal = new ChatModelAlibabaMultimodal();

    /// <summary>
    /// Embedding models - multilingual text embedding models for semantic analysis.
    /// </summary>
    public readonly ChatModelAlibabaEmbeddings Embeddings = new ChatModelAlibabaEmbeddings();

    /// <summary>
    /// Reasoning models - advanced models for complex multi-step reasoning tasks.
    /// </summary>
    public readonly ChatModelAlibabaReasoning Reasoning = new ChatModelAlibabaReasoning();

    /// <summary>
    /// Older models - legacy models no longer updated, may be deprecated.
    /// </summary>
    public readonly ChatModelAlibabaOlder Older = new ChatModelAlibabaOlder();

    /// <summary>
    /// All models owned by the provider.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// All known chat models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => 
    [
        ..ChatModelAlibabaFlagship.ModelsAll,
        ..ChatModelAlibabaCostOptimized.ModelsAll,
        ..ChatModelAlibabaVisual.ModelsAll,
        ..ChatModelAlibabaMultimodal.ModelsAll,
        ..ChatModelAlibabaEmbeddings.ModelsAll,
        ..ChatModelAlibabaReasoning.ModelsAll,
        ..ChatModelAlibabaOlder.ModelsAll
    ]);

    /// <summary>
    /// Map of models owned by the provider.
    /// </summary>
    public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;

    private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
    {
        HashSet<string> map = [];
        ModelsAll.ForEach(x => map.Add(x.Name));
        return map;
    });

    /// <summary>
    /// Checks whether the vendor owns given model.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public override bool OwnsModel(string model)
    {
        return AllModelsMap.Contains(model);
    }

    internal ChatModelAlibaba()
    {
    }
}
