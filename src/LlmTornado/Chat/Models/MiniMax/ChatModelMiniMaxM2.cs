using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.MiniMax;

/// <summary>
/// M2 series models from MiniMax.
/// </summary>
public class ChatModelMiniMaxM2 : IVendorModelClassProvider
{
    /// <summary>
    /// Peak performance, ultimate value, masters complex tasks. Output speed approximately 60 tps.
    /// </summary>
    public static readonly ChatModel ModelM25 = new ChatModel("MiniMax-M2.5", LLmProviders.MiniMax, 204_800);
    
    /// <summary>
    /// <inheritdoc cref="ModelM25"/>
    /// </summary>
    public readonly ChatModel M25 = ModelM25;
    
    /// <summary>
    /// Same performance as M2.5, faster and more agile. Output speed approximately 100 tps.
    /// </summary>
    public static readonly ChatModel ModelM25Highspeed = new ChatModel("MiniMax-M2.5-highspeed", LLmProviders.MiniMax, 204_800);
    
    /// <summary>
    /// <inheritdoc cref="ModelM25Highspeed"/>
    /// </summary>
    public readonly ChatModel M25Highspeed = ModelM25Highspeed;
    
    /// <summary>
    /// Powerful multi-language programming capabilities with enhanced programming experience. Output speed approximately 60 tps.
    /// </summary>
    public static readonly ChatModel ModelM21 = new ChatModel("MiniMax-M2.1", LLmProviders.MiniMax, 204_800);
    
    /// <summary>
    /// <inheritdoc cref="ModelM21"/>
    /// </summary>
    public readonly ChatModel M21 = ModelM21;
    
    /// <summary>
    /// Faster and more agile variant of M2.1. Output speed approximately 100 tps.
    /// </summary>
    public static readonly ChatModel ModelM21Highspeed = new ChatModel("MiniMax-M2.1-highspeed", LLmProviders.MiniMax, 204_800);
    
    /// <summary>
    /// <inheritdoc cref="ModelM21Highspeed"/>
    /// </summary>
    public readonly ChatModel M21Highspeed = ModelM21Highspeed;
    
    /// <summary>
    /// Agentic capabilities, advanced reasoning.
    /// </summary>
    public static readonly ChatModel ModelM2 = new ChatModel("MiniMax-M2", LLmProviders.MiniMax, 204_800);
    
    /// <summary>
    /// <inheritdoc cref="ModelM2"/>
    /// </summary>
    public readonly ChatModel M2 = ModelM2;
    
    /// <summary>
    /// All known M2 series models from MiniMax.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelM25, ModelM25Highspeed, ModelM21, ModelM21Highspeed, ModelM2]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMiniMaxM2()
    {

    }
}
