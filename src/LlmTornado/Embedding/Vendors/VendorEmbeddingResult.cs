namespace LlmTornado.Embedding.Vendors;

/// <summary>
///     Base class shared between chat results from different vendors.
/// </summary>
internal abstract class VendorEmbeddingResult : IVendorEmbeddingResult
{
    /// <summary>
    ///     The result vendor specific response was transformed into.
    /// </summary>
    public EmbeddingResult? Result { get; set; }

    public abstract EmbeddingResult ToResult(string? postData);
}

internal interface IVendorEmbeddingResult
{
    public EmbeddingResult ToResult(string? postData);
}