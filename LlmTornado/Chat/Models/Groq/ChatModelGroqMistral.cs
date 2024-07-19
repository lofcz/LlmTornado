using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Mistral models hosted by Groq.
/// </summary>
public class ChatModelGroqMistral : IVendorModelClassProvider
{
    /// <summary>
    /// mixtral-8x7b-32768
    /// </summary>
    public static readonly ChatModel ModelMixtral8X7B = new ChatModel("groq-mixtral-8x7b-32768", LLmProviders.Groq, 32_768)
    {
        ApiName = "mixtral-8x7b-32768"
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelMixtral8X7B"/>
    /// </summary>
    public readonly ChatModel Mixtral8X7B = ModelMixtral8X7B;
    
    /// <summary>
    /// All known Mistral models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelMixtral8X7B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    internal ChatModelGroqMistral()
    {

    }

}