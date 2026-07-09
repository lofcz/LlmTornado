using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Grok Build class models from xAI.
/// </summary>
public class ChatModelXAiGrokBuild : IVendorModelClassProvider
{
    /// <summary>
    /// Grok Build 0.1 is xAI's intelligent coding model for agentic software, engineering, and workflow tasks.
    /// 256K context window. Supports function calling, structured outputs, and reasoning.
    /// </summary>
    public static readonly ChatModel ModelV01 = new ChatModel("grok-build-0.1", LLmProviders.XAi, 256_000, [ "grok-build", "grok-code-fast-1", "grok-code-fast", "grok-code-fast-1-0825" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV01"/>
    /// </summary>
    public readonly ChatModel V01 = ModelV01;
    
    /// <summary>
    /// All Grok Build models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV01
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrokBuild()
    {
        
    }
}
