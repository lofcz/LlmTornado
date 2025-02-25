using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
/// Reasoning data.
/// </summary>
public class ChatMessageReasoningData
{
    /// <summary>
    /// Content of the reasoning. This can be empty in case of redacted COTs.
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Crypto token used to verify COT hasn't been tampered with. Used only by Anthropic.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Returns whether the reasoning block is redacted or not. Null means this can't be resolved. 
    /// </summary>
    public bool? IsRedacted => Provider is not LLmProviders.Anthropic ? null : Signature is not null && Content is null;
    
    internal LLmProviders Provider { get; set; }
}