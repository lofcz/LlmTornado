namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     A block of text, either cited by one or more document or uncited.
/// </summary>
public class VendorCohereCitationBlock
{
    /// <summary>
    ///     Text of the block.
    /// </summary>
    public string Text { get; set; }
    
    /// <summary>
    ///     Citation reference.
    /// </summary>
    public VendorCohereChatCitation? Citation { get; set; }
}