using System.Collections.Generic;
using OpenAiNg.Code.Models;

namespace OpenAiNg.Chat.Models;

/// <summary>
/// Known chat models from Anthropic
/// </summary>
public class ChatModelAnthropic: IVendorModelProvider
{
    /// <summary>
    /// GPT 3.5 models
    /// </summary>
    public readonly ChatModelAnthropicClaude3 Claude3 = new ChatModelAnthropicClaude3();
    
    /// <summary>
    /// All known chat models from OpenAI.
    /// </summary>
    public List<IModel> AllModels { get; } = [
        ..ChatModel.OpenAi.Gpt35.AllModels,
        ..ChatModel.OpenAi.Gpt4.AllModels
    ];
    
    internal ChatModelAnthropic()
    {
        
    }
}