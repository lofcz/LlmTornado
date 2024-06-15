using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Cohere.
/// </summary>
public class ChatModelGoogle: IVendorModelProvider
{
    /// <summary>
    /// Coral models.
    /// </summary>
    public readonly ChatModelGoogleGemini Gemini = new ChatModelGoogleGemini();
    
    /// <summary>
    /// All known chat models from Google.
    /// </summary>
    public List<IModel> AllModels { get; } = [
        ..ChatModelGoogleGemini.ModelsAll
    ];
    
    internal ChatModelGoogle()
    {
        
    }
}