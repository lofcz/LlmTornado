namespace LlmTornado.Chat;

/// <summary>
/// Reasoning data.
/// </summary>
public class ChatMessageReasoningData
{
    /// <summary>
    /// Content of the reasoning.
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// Crypto token used to verify COT hasn't been tampered with. Used only by Anthropic.
    /// </summary>
    public string? Signature { get; set; }
}