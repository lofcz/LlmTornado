using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Alibaba models hosted by Groq.
/// </summary>
public class ChatModelGroqAlibaba : IVendorModelClassProvider
{
    /// <summary>
    /// qwen/qwen3-32b
    /// </summary>
    public static readonly ChatModel ModelQwen332B = new ChatModel("grok-qwen/qwen3-32b", LLmProviders.Groq, 131_072)
    {
        ApiName = "qwen/qwen3-32b"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen332B"/>
    /// </summary>
    public readonly ChatModel Qwen332B = ModelQwen332B;
    
    /// <summary>
    /// All known Alibaba models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelQwen332B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    internal ChatModelGroqAlibaba()
    {

    }

}