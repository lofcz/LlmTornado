using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.MoonshotAi;

/// <summary>
/// All models from Moonshot AI.
/// </summary>
public class ChatModelMoonshotAiModels : IVendorModelClassProvider
{
    /// <summary>
    /// kimi-k2
    /// </summary>
    public static readonly ChatModel ModelKimiK2 = new ChatModel("kimi-k2", LLmProviders.MoonshotAi, 63_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiK2"/>
    /// </summary>
    public readonly ChatModel KimiK2 = ModelKimiK2;
    
    /// <summary>
    /// kimi-k2-instruct
    /// </summary>
    public static readonly ChatModel ModelKimiK2Instruct = new ChatModel("kimi-k2-instruct", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiK2Instruct"/>
    /// </summary>
    public readonly ChatModel KimiK2Instruct = ModelKimiK2Instruct;
    
    /// <summary>
    /// All known models from Moonshot AI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelKimiK2, ModelKimiK2Instruct]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMoonshotAiModels()
    {

    }
}