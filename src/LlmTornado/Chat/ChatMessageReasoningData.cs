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
    /// Opaque token used to verify or rehydrate reasoning content. 
    /// For Anthropic this is the "signature" on thinking blocks, for Google the "thoughtSignature", 
    /// and for xAI the "encrypted_content" used for thinking trace rehydration.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Returns whether the reasoning block is redacted or not. Null means this can't be resolved. 
    /// </summary>
    public bool? IsRedacted => Provider switch
    {
        LLmProviders.XAi => Signature is not null && Content is null,
        LLmProviders.Anthropic => Signature is not null && Content is null,
        _ => null
    };

    /// <summary>
    /// Returns whether Anthropic omitted thinking text while preserving the signature for multi-turn continuity.
    /// </summary>
    public bool? IsOmitted => Provider switch
    {
        LLmProviders.Anthropic => Signature is not null && Content == string.Empty,
        _ => null
    };
    
    internal LLmProviders Provider { get; set; }
}