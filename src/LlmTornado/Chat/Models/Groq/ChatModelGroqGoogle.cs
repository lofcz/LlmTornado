using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Google models hosted by Groq.
/// </summary>
public class ChatModelGroqGoogle : IVendorModelClassProvider
{
    /// <summary>
    /// gemma2-9b-it
    /// </summary>
    public static readonly ChatModel ModelGemma29B = new ChatModel("groq-gemma2-9b-it", LLmProviders.Groq, 8_192)
    {
        ApiName = "gemma2-9b-it"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma29B"/>
    /// </summary>
    public readonly ChatModel Gemma29B = ModelGemma29B;
    
    /// <summary>
    /// All known Google models from Groq.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelGemma29B]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGroqGoogle()
    {

    }
}