namespace LlmTornado.Chat.Vendors;

/// <summary>
///     Base class shared between chat results from different vendors.
/// </summary>
internal abstract class VendorChatResult : IVendorChatResult
{
    /// <summary>
    ///     The result vendor specific response was transformed into.
    /// </summary>
    public ChatResult? ChatResult { get; set; }

    public abstract ChatResult ToChatResult(string? postData, object? chatRequest);
}

internal interface IVendorChatResult
{
    public ChatResult ToChatResult(string? postData, object? chatRequest);
}