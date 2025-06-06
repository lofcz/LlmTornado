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
    /// All known Mistral models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    internal ChatModelGroqMistral()
    {

    }

}