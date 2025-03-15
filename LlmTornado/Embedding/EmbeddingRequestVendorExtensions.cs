using LlmTornado.Embedding.Vendors.Cohere;
using LlmTornado.Embedding.Vendors.Google;

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
    ///     Cohere extensions.
    /// </summary>
    public EmbeddingRequestVendorGoogleExtensions? Google { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public EmbeddingRequestVendorExtensions()
    {
        
    }
    
    /// <summary>
    ///     Cohere extensions.
    /// </summary>
    public EmbeddingRequestVendorExtensions(EmbeddingRequestVendorCohereExtensions extensions)
    {
        Cohere = extensions;
    }
    
    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public EmbeddingRequestVendorExtensions(EmbeddingRequestVendorGoogleExtensions extensions)
    {
        Google = extensions;
    }
}