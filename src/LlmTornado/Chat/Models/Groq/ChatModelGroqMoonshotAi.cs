using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Moonshot AI models hosted by Groq.
/// </summary>
public class ChatModelGroqMoonshotAi : IVendorModelClassProvider
{
    /// <summary>
    /// qwen/qwen3-32b
    /// </summary>
    public static readonly ChatModel ModelKimiK2Instruct = new ChatModel("grok-moonshotai/kimi-k2-instruct", LLmProviders.Groq, 131_072)
    {
        ApiName = "moonshotai/kimi-k2-instruct"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiK2Instruct"/>
    /// </summary>
    public readonly ChatModel KimiK2Instruct = ModelKimiK2Instruct;
    
    /// <summary>
    /// All known Moonshot AI models from Groq.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelKimiK2Instruct]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    internal ChatModelGroqMoonshotAi()
    {

    }

}