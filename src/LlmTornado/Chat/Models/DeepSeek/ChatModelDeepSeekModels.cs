using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepSeek;

/// <summary>
/// All models from DeepSeek.
/// </summary>
public class ChatModelDeepSeekModels : IVendorModelClassProvider
{
    /// <summary>
    /// DeepSeek-V3
    /// </summary>
    public static readonly ChatModel ModelChat = new ChatModel("deepseek-chat", LLmProviders.DeepSeek, 64_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelChat"/>
    /// </summary>
    public readonly ChatModel Chat = ModelChat;
    
    /// <summary>
    /// DeepSeek-R1
    /// </summary>
    public static readonly ChatModel ModelReasoner = new ChatModel("deepseek-reasoner", LLmProviders.DeepSeek, 64_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelReasoner"/>
    /// </summary>
    public readonly ChatModel Reasoner = ModelReasoner;
    
    /// <summary>
    /// All known Coral models from Cohere.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelChat, ModelReasoner]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepSeekModels()
    {

    }
}