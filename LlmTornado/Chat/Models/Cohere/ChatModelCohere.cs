using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Cohere.
/// </summary>
public class ChatModelCohere: IVendorModelProvider
{
    /// <summary>
    /// Coral models.
    /// </summary>
    public readonly ChatModelCohereCoral Coral = new ChatModelCohereCoral();
    
    /// <summary>
    /// All known chat models from Cohere.
    /// </summary>
    public List<IModel> AllModels { get; } = [
        ..ChatModelCohereCoral.ModelsAll
    ];
    
    internal ChatModelCohere()
    {
        
    }
}