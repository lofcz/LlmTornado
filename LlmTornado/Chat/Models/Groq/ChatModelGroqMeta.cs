using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Meta models hosted by Groq.
/// </summary>
public class ChatModelGroqMeta : IVendorModelClassProvider
{
    /// <summary>
    /// llama3-70b-8192
    /// </summary>
    public static readonly ChatModel ModelLlama370B = new ChatModel("groq-llama3-70b-8192", LLmProviders.Groq, 8_192)
    {
        ApiName = "llama3-70b-8192"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama370B"/>
    /// </summary>
    public readonly ChatModel Llama370B = ModelLlama370B;
    
    /// <summary>
    /// llama3-8b-8192
    /// </summary>
    public static readonly ChatModel ModelLlama38B = new ChatModel("groq-llama3-8b-8192", LLmProviders.Groq, 8_182)
    {
        ApiName = "llama3-8b-8192"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama38B"/>
    /// </summary>
    public readonly ChatModel Llama38B = ModelLlama38B;
    
    /// <summary>
    /// All known Meta models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelLlama370B,
        ModelLlama38B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGroqMeta()
    {

    }
}