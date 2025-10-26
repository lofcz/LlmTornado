using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Chat.Vendors.XAi;

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
    ///     XAi extensions.
    /// </summary>
    public ChatResponseVendorXAiExtensions? XAi { get; set; }

    /// <summary>
    ///     Google extensions.
    /// </summary>
    public ChatResponseVendorGoogleExtensions? Google { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public ChatResponseVendorExtensions()
    {

    }
}