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
    public readonly ChatModelCohereClaude3 Claude3 = new ChatModelCohereClaude3();
    
    /// <summary>
    /// All known chat models from Cohere.
    /// </summary>
    public List<IModel> AllModels { get; } = [
        ..ChatModelCohereClaude3.ModelsAll
    ];
    
    internal ChatModelCohere()
    {
        
    }
}