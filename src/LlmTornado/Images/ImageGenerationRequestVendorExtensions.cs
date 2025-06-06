using LlmTornado.Images.Vendors.Google;

namespace LlmTornado.Images;

/// <summary>
///     Image generation request features supported only by a single/few providers with no shared equivalent.
/// </summary>
public class ImageGenerationRequestVendorExtensions
{
    /// <summary>
    ///     Google extensions.
    /// </summary>
    public ImageGenerationRequestGoogleExtensions? Google { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public ImageGenerationRequestVendorExtensions()
    {
        
    }
    
    /// <summary>
    ///     Google extensions.
    /// </summary>
    /// <param name="googleExtensions"></param>
    public ImageGenerationRequestVendorExtensions(ImageGenerationRequestGoogleExtensions googleExtensions)
    {
        Google = googleExtensions;
    }
}