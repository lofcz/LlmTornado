using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Grok 4 class models from xAI.
/// </summary>
public class ChatModelXAiGrok4 : IVendorModelClassProvider
{
    /// <summary>
    /// Our latest and greatest flagship model, offering unparalleled performance in natural language, math and reasoning - the perfect jack of all trades.
    /// </summary>
    public static readonly ChatModel ModelV4 = new ChatModel("grok-4", LLmProviders.XAi, 256_000, [ "grok-4-0709", "grok-4-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV4"/>
    /// </summary>
    public readonly ChatModel V4 = ModelV4;
    
    /// <summary>
    /// All Grok 4 models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelV4]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrok4()
    {
        
    }
}