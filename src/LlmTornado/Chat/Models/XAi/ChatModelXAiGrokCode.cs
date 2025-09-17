using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Grok Code class models from xAI.
/// </summary>
public class ChatModelXAiGrokCode : IVendorModelClassProvider
{
    /// <summary>
    /// Our latest and greatest flagship model, offering unparalleled performance in natural language, math and reasoning - the perfect jack of all trades.
    /// </summary>
    public static readonly ChatModel ModelFast1 = new ChatModel("grok-code-fast-1", LLmProviders.XAi, 256_000, [ "grok-code-fast", "grok-code-fast-1-0825" ]);

    /// <summary>
    /// <inheritdoc cref="ModelFast1"/>
    /// </summary>
    public readonly ChatModel Fast1 = ModelFast1;
    
    /// <summary>
    /// All Grok Code models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelFast1]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrokCode()
    {
        
    }
}