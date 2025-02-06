using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Google;

namespace LlmTornado.Chat;

/// <summary>
///     Chat outbound features supported only by a single/few providers with no shared equivalent.
/// </summary>
public class ChatRequestVendorExtensions
{
    /// <summary>
    ///     Cohere extensions.
    /// </summary>
    public ChatRequestVendorCohereExtensions? Cohere { get; set; }
    
    /// <summary>
    ///     Anthropic extensions.
    /// </summary>
    public ChatRequestVendorAnthropicExtensions? Anthropic { get; set; }
    
    /// <summary>
    ///     Google extensions.
    /// </summary>
    public ChatRequestVendorGoogleExtensions? Google { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public ChatRequestVendorExtensions()
    {
        
    }

    /// <summary>
    ///     Cohere extensions.
    /// </summary>
    /// <param name="cohereExtensions"></param>
    public ChatRequestVendorExtensions(ChatRequestVendorCohereExtensions cohereExtensions)
    {
        Cohere = cohereExtensions;
    }
    
    /// <summary>
    ///     Cohere extensions.
    /// </summary>
    /// <param name="anthropicExtensions"></param>
    public ChatRequestVendorExtensions(ChatRequestVendorAnthropicExtensions anthropicExtensions)
    {
        Anthropic = anthropicExtensions;
    }

    /// <summary>
    ///     Google extensions.
    /// </summary>
    /// <param name="googleExtensions"></param>
    public ChatRequestVendorExtensions(ChatRequestVendorGoogleExtensions googleExtensions)
    {
        Google = googleExtensions;
    }
}