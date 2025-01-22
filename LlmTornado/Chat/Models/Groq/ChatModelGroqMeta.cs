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
    /// llama-3.3-70b-versatile
    /// </summary>
    public static readonly ChatModel ModelLlama3370BVersatile = new ChatModel("groq-llama-3.3-70b-versatile", LLmProviders.Groq, 128_000)
    {
        ApiName = "llama-3.3-70b-versatile"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama3370BVersatile"/>
    /// </summary>
    public readonly ChatModel Llama3370BVersatile = ModelLlama3370BVersatile;
    
    /// <summary>
    /// llama-3.1-8b-instant
    /// </summary>
    public static readonly ChatModel ModelLlama318BInstant = new ChatModel("groq-llama-3.1-8b-instant", LLmProviders.Groq, 128_000)
    {
        ApiName = "llama-3.1-8b-instant"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama318BInstant"/>
    /// </summary>
    public readonly ChatModel Llama318BInstant = ModelLlama318BInstant;
    
    /// <summary>
    /// llama-guard-3-8b
    /// </summary>
    public static readonly ChatModel ModelLlamaGuard3BB = new ChatModel("groq-llama-guard-3-8b", LLmProviders.Groq, 8_192)
    {
        ApiName = "llama-guard-3-8b"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelLlamaGuard3BB"/>
    /// </summary>
    public readonly ChatModel LlamaGuard3BB = ModelLlamaGuard3BB;
    
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
    public static readonly ChatModel ModelLlama38B = new ChatModel("groq-llama3-8b-8192", LLmProviders.Groq, 8_192)
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
        ModelLlama3370BVersatile,
        ModelLlama318BInstant,
        ModelLlamaGuard3BB,
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