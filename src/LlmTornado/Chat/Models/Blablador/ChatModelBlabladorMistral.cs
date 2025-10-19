using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Mistral AI models hosted by Blablador.
/// </summary>
public class ChatModelBlabladorMistral : IVendorModelClassProvider
{
    /// <summary>
    /// Ministral-8B-Instruct-2410 - 8B instruction-tuned model from Mistral AI (October 2024).
    /// </summary>
    public static readonly ChatModel ModelMinistral8BInstruct2410 = new ChatModel("blablador-ministral-8b-instruct-2410", LLmProviders.Blablador, 128_000)
    {
        ApiName = "models/Ministral-8B-Instruct-2410"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelMinistral8BInstruct2410"/>
    /// </summary>
    public readonly ChatModel Ministral8BInstruct2410 = ModelMinistral8BInstruct2410;
    
    /// <summary>
    /// Ministral-8B - Base 8B parameter model from Mistral AI.
    /// </summary>
    public static readonly ChatModel ModelMinistral8B = new ChatModel("Ministral-8B", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMinistral8B"/>
    /// </summary>
    public readonly ChatModel Ministral8B = ModelMinistral8B;
    
    /// <summary>
    /// All known Mistral models hosted by Blablador.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMinistral8BInstruct2410, ModelMinistral8B
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelBlabladorMistral()
    {

    }
}

