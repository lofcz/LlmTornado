using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Meta models hosted by Groq.
/// </summary>
public class ChatModelGroqGroq : IVendorModelClassProvider
{
    /// <summary>
    /// groq-llama3-groq-8b-8192-tool-use-preview
    /// </summary>
    public static readonly ChatModel ModelLlama38B = new ChatModel("groq-llama3-groq-8b-8192-tool-use-preview", LLmProviders.Groq, 8_192)
    {
        ApiName = "llama3-groq-8b-8192-tool-use-preview"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama38B"/>
    /// </summary>
    public readonly ChatModel Llama38B = ModelLlama38B;
    
    /// <summary>
    /// llama3-groq-70b-8192-tool-use-preview
    /// </summary>
    public static readonly ChatModel ModelLlama370B = new ChatModel("llama3-groq-70b-8192-tool-use-preview", LLmProviders.Groq, 8_192)
    {
        ApiName = "llama3-groq-70b-8192-tool-use-preview"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama370B"/>
    /// </summary>
    public readonly ChatModel Llama370B = ModelLlama370B;
    
    /// <summary>
    /// All known Meta models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelLlama38B,
        ModelLlama370B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGroqGroq()
    {

    }
}