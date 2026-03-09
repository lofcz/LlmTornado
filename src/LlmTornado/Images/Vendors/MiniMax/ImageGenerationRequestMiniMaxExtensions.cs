using System.Collections.Generic;

namespace LlmTornado.Images.Vendors.MiniMax;

/// <summary>
/// Extensions to image generation request for MiniMax.
/// </summary>
public class ImageGenerationRequestMiniMaxExtensions
{
    /// <summary>
    /// Aspect ratio of the generated image. Defaults to 1:1.
    /// Supported values: Square (1:1), Landscape16x9, Landscape4x3, Landscape3x2, Portrait2x3, Portrait3x4, Portrait9x16, Landscape21x9.
    /// If both width/height and aspect_ratio are provided, aspect_ratio takes priority.
    /// </summary>
    public ImageAspectRatio? AspectRatio { get; set; }
    
    /// <summary>
    /// Random seed. Using the same seed and parameters produces reproducible images.
    /// If not provided, a random seed is generated for each image.
    /// </summary>
    public long? Seed { get; set; }
    
    /// <summary>
    /// Enable automatic optimization of prompt. Default: false.
    /// </summary>
    public bool? PromptOptimizer { get; set; }
    
    /// <summary>
    /// Subject references for image-to-image generation. Each entry references an image
    /// (URL or base64 data URL) to use as a character reference in the output.
    /// Currently only "character" type is supported by MiniMax.
    /// </summary>
    public List<ImageMiniMaxSubjectReference>? SubjectReferences { get; set; }
}

/// <summary>
/// A subject reference for MiniMax image-to-image generation.
/// </summary>
public class ImageMiniMaxSubjectReference
{
    /// <summary>
    /// Subject type. Currently only "character" (portrait) is supported.
    /// </summary>
    public string Type { get; set; } = "character";
    
    /// <summary>
    /// Reference image file. Supports public URLs or Base64-encoded Data URLs (data:image/jpeg;base64,...).
    /// For best results, upload a single front-facing portrait photo.
    /// Formats: JPG, JPEG, PNG. Size: less than 10MB.
    /// </summary>
    public string ImageFile { get; set; } = null!;
    
    /// <summary>
    /// Creates a character subject reference from an image URL or base64 data URL.
    /// </summary>
    /// <param name="imageFile">Public URL or base64 data URL of the reference image.</param>
    public ImageMiniMaxSubjectReference(string imageFile)
    {
        ImageFile = imageFile;
    }
    
    /// <summary>
    /// Creates a subject reference with a specified type.
    /// </summary>
    /// <param name="type">Subject type (e.g. "character").</param>
    /// <param name="imageFile">Public URL or base64 data URL of the reference image.</param>
    public ImageMiniMaxSubjectReference(string type, string imageFile)
    {
        Type = type;
        ImageFile = imageFile;
    }
    
    /// <summary>
    /// Parameterless constructor for serialization.
    /// </summary>
    public ImageMiniMaxSubjectReference()
    {
    }
}
