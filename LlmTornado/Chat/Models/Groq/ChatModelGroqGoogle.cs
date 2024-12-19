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
    /// gemma-7b-it
    /// </summary>
    [Obsolete("Deprecated by Groq")]
    public static readonly ChatModel ModelGemma7B = new ChatModel("groq-gemma-7b-it", LLmProviders.Groq, 8_192)
    {
        ApiName = "gemma-7b-it"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma7B"/>
    /// </summary>
    public readonly ChatModel Gemma7B = ModelGemma7B;
    
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
    public static readonly List<IModel> ModelsAll =
    [
        // ModelGemma7B, // deprecated
        ModelGemma29B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGroqGoogle()
    {

    }
}