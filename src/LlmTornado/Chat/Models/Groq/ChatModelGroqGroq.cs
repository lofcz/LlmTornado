using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Groq models hosted by Groq.
/// </summary>
public class ChatModelGroqGroq : IVendorModelClassProvider
{
    /// <summary>
    /// All known Meta models from Groq.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGroqGroq()
    {

    }
}