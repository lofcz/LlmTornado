using LlmTornado.Chat.Vendors.Cohere;

namespace LlmTornado.Chat;

/// <summary>
///     Chat inbound features supported only by a single/few providers with no shared equivalent.
/// </summary>
public class ChatResponseVendorExtensions
{
    /// <summary>
    ///     Cohere extensions.
    /// </summary>
    public ChatResponseVendorCohereExtensions? Cohere { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public ChatResponseVendorExtensions()
    {
        
    }
}