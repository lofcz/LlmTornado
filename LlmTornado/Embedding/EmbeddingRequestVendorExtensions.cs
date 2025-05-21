using LlmTornado.Embedding.Vendors.Cohere;
using LlmTornado.Embedding.Vendors.Google;
using LlmTornado.Embedding.Vendors.Voyage;

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
    ///     Google extensions.
    /// </summary>
    public EmbeddingRequestVendorGoogleExtensions? Google { get; set; }
    
    /// <summary>
    ///     Voyage extensions.
    /// </summary>
    public EmbeddingRequestVendorVoyageExtensions? Voyage { get; set; }

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
    ///     Google extensions.
    /// </summary>
    public EmbeddingRequestVendorExtensions(EmbeddingRequestVendorGoogleExtensions extensions)
    {
        Google = extensions;
    }
    
    /// <summary>
    ///     Voyage extensions.
    /// </summary>
    public EmbeddingRequestVendorExtensions(EmbeddingRequestVendorVoyageExtensions extensions)
    {
        Voyage = extensions;
    }
}