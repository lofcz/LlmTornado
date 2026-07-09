using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Grok 4.5 class models from xAI.
/// </summary>
public class ChatModelXAiGrok45 : IVendorModelClassProvider
{
    /// <summary>
    /// Grok 4.5 is xAI's most intelligent and fastest model for chat, coding, and agentic tool use.
    /// 500K context window. Supports function calling, structured outputs, and reasoning.
    /// </summary>
    public static readonly ChatModel ModelV45 = new ChatModel("grok-4.5", LLmProviders.XAi, 500_000, [ "grok-4.5-latest", "grok-build-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV45"/>
    /// </summary>
    public readonly ChatModel V45 = ModelV45;
    
    /// <summary>
    /// All Grok 4.5 models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV45
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrok45()
    {
        
    }
}
