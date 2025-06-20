using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.OpenRouter;

/// <summary>
/// All models from Open Router.
/// </summary>
public class ChatModelOpenRouterAll : IVendorModelClassProvider
{
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// All known models from Open Router.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
       
    ];
    
    internal ChatModelOpenRouterAll()
    {

    }
}