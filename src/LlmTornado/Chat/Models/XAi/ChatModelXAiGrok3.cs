using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Grok 3 class models from xAI.
/// </summary>
public class ChatModelXAiGrok3 : IVendorModelClassProvider
{
    /// <summary>
    /// Our flagship model that excels at enterprise use cases like data extraction, coding, and text summarization. Possesses deep domain knowledge in finance, healthcare, law, and science.
    /// </summary>
    public static readonly ChatModel ModelV3 = new ChatModel("grok-3", LLmProviders.XAi, 131_072, [ "grok-3-beta", "grok-3-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV3"/>
    /// </summary>
    public readonly ChatModel V3 = ModelV3;
    
    /// <summary>
    /// Our flagship model that excels at enterprise use cases like data extraction, coding, and text summarization. Possesses deep domain knowledge in finance, healthcare, law, and science.
    /// </summary>
    public static readonly ChatModel ModelV3Fast = new ChatModel("grok-3-fast", LLmProviders.XAi, 131_072, [ "grok-3-fast-beta", "grok-3-fast-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV3Fast"/>
    /// </summary>
    public readonly ChatModel V3Fast = ModelV3Fast;
    
    /// <summary>
    /// A lightweight model that thinks before responding. Fast, smart, and great for logic-based tasks that do not require deep domain knowledge. The raw thinking traces are accessible.
    /// </summary>
    public static readonly ChatModel ModelV3Mini = new ChatModel("grok-3-mini", LLmProviders.XAi, 131_072, [ "grok-3-mini-beta", "grok-3-mini-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV3Mini"/>
    /// </summary>
    public readonly ChatModel V3Mini = ModelV3Mini;
    
    /// <summary>
    /// A lightweight model that thinks before responding. Fast, smart, and great for logic-based tasks that do not require deep domain knowledge. The raw thinking traces are accessible.
    /// </summary>
    public static readonly ChatModel ModelV3MiniFast = new ChatModel("grok-3-mini-fast", LLmProviders.XAi, 131_072, [ "grok-3-mini-fast-beta", "grok-3-mini-fast-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV3MiniFast"/>
    /// </summary>
    public readonly ChatModel V3MiniFast = ModelV3MiniFast;
    
    /// <summary>
    /// All Grok 3 models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelV3, ModelV3Fast, ModelV3Mini, ModelV3MiniFast]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrok3()
    {
        
    }
}