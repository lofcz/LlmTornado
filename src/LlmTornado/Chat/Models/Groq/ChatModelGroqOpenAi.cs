using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// OpenAI models hosted by Groq.
/// </summary>
public class ChatModelGroqOpenAi : IVendorModelClassProvider
{
    /// <summary>
    /// openai/gpt-oss-120b
    /// </summary>
    public static readonly ChatModel ModelGptOss120B = new ChatModel("grok-openai/gpt-oss-120b", LLmProviders.Groq, 131_072)
    {
        ApiName = "openai/gpt-oss-120b"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGptOss120B"/>
    /// </summary>
    public readonly ChatModel GptOss120B = ModelGptOss120B;
    
    /// <summary>
    /// openai/gpt-oss-20b
    /// </summary>
    public static readonly ChatModel ModelGptOss20B = new ChatModel("grok-openai/gpt-oss-20b", LLmProviders.Groq, 131_072)
    {
        ApiName = "openai/gpt-oss-20b"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGptOss20B"/>
    /// </summary>
    public readonly ChatModel GptOss20B = ModelGptOss20B;
    
    /// <summary>
    /// All known OpenAI models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelGptOss120B,
        ModelGptOss20B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    internal ChatModelGroqOpenAi()
    {

    }

}