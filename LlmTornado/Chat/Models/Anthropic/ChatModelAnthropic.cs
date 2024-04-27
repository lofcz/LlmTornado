using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Anthropic.
/// </summary>
public class ChatModelAnthropic: IVendorModelProvider
{
    /// <summary>
    /// Claude 3 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude3 Claude3 = new ChatModelAnthropicClaude3();
    
    /// <summary>
    /// All known chat models from Anthropic.
    /// </summary>
    public List<IModel> AllModels { get; } = [
        ..ChatModelAnthropicClaude3.ModelsAll
    ];
    
    internal ChatModelAnthropic()
    {
        
    }
}