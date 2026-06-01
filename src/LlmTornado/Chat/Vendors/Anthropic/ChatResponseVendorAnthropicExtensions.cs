namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// Chat response features supported only by Anthropic.
/// </summary>
public class ChatResponseVendorAnthropicExtensions
{
    /// <summary>
    /// Cache diagnostics returned when cache diagnostics was enabled on the request.
    /// A null value on the response means either the first turn (nothing to compare) or no divergence was found.
    /// </summary>
    public AnthropicCacheDiagnosticsResponse? CacheDiagnostics { get; set; }
    
    /// <summary>
    /// Empty extensions.
    /// </summary>
    public ChatResponseVendorAnthropicExtensions()
    {
        
    }
}
