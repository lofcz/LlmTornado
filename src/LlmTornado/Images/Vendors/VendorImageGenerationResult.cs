namespace LlmTornado.Images.Vendors;

/// <summary>
///     Base class shared between chat results from different vendors.
/// </summary>
internal abstract class VendorImageGenerationResult : IVendorImageGenerationResult
{
    /// <summary>
    ///     The result vendor specific response was transformed into.
    /// </summary>
    public ImageGenerationResult? ChatResult { get; set; }

    public abstract ImageGenerationResult ToChatResult(string? postData);
}

internal interface IVendorImageGenerationResult
{
    public ImageGenerationResult ToChatResult(string? postData);
}