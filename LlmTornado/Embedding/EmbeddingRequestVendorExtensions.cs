using LlmTornado.Embedding.Vendors.Cohere;

namespace LlmTornado.Embedding;

/// <summary>
///		Embedding features supported only by a single/few providers with no shared equivalent.
/// </summary>
public class EmbeddingRequestVendorExtensions
{
    /// <summary>
    ///     Cohere extensions.
    /// </summary>
    public EmbeddingRequestVendorCohereExtensions? Cohere { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public EmbeddingRequestVendorExtensions()
    {
        
    }
}