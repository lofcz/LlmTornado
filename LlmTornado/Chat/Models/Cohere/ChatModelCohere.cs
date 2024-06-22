using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Cohere.
/// </summary>
public class ChatModelCohere: IVendorModelProvider
{
    /// <summary>
    /// Command models.
    /// </summary>
    public readonly ChatModelCohereCommand Command = new ChatModelCohereCommand();
    
    /// <summary>
    /// All known chat models from Cohere.
    /// </summary>
    public List<IModel> AllModels { get; } = [
        ..ChatModelCohereCommand.ModelsAll
    ];
    
    internal ChatModelCohere()
    {
        
    }
}