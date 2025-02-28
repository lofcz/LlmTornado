using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Aya class models from Anthropic.
/// </summary>
public class ChatModelCohereAya : IVendorModelClassProvider
{
    /// <summary>
    /// Aya Expanse is a highly performant 8B multilingual model, designed to rival monolingual performance through innovations in instruction tuning with data arbitrage, preference training, and model merging. Serves 23 languages.
    /// </summary>
    public static readonly ChatModel ModelExpanse8B = new ChatModel("c4ai-aya-expanse-8b", LLmProviders.Cohere, 8_196);
    
    /// <summary>
    /// <inheritdoc cref="ModelExpanse8B"/>
    /// </summary>
    public readonly ChatModel Expanse8B = ModelExpanse8B;

    /// <summary>
    /// Aya Expanse is a highly performant 32B multilingual model, designed to rival monolingual performance through innovations in instruction tuning with data arbitrage, preference training, and model merging. Serves 23 languages.
    /// </summary>
    public static readonly ChatModel ModelExpanse32B = new ChatModel("c4ai-aya-expanse-32b", LLmProviders.Cohere, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelExpanse32B"/>
    /// </summary>
    public readonly ChatModel Expanse32B = ModelExpanse32B;
    
    /// <summary>
    /// All known Coral models from Cohere.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelExpanse8B,
        ModelExpanse32B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelCohereAya()
    {

    }
}