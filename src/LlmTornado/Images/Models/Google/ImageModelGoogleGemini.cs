using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.Google;

/// <summary>
/// Gemini native image models from Google (generateContent-based image generation).
/// </summary>
public class ImageModelGoogleGemini : IVendorModelClassProvider
{
    /// <summary>
    /// Nano Banana 2 (Gemini 3.1 Flash Image). Supports video-to-image, text-to-image, and conversational editing.
    /// </summary>
    public static readonly ImageModel ModelGemini31FlashImage = new ImageModel("gemini-3.1-flash-image", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelGemini31FlashImage"/>
    /// </summary>
    public readonly ImageModel Gemini31FlashImage = ModelGemini31FlashImage;

    /// <summary>
    /// Preview variant of Gemini 3.1 Flash Image.
    /// </summary>
    public static readonly ImageModel ModelGemini31FlashImagePreview = new ImageModel("gemini-3.1-flash-image-preview", LLmProviders.Google);

    /// <summary>
    /// <inheritdoc cref="ModelGemini31FlashImagePreview"/>
    /// </summary>
    public readonly ImageModel Gemini31FlashImagePreview = ModelGemini31FlashImagePreview;

    /// <summary>
    /// Models that support video-to-image generation.
    /// </summary>
    public static HashSet<string> VideoToImageModels { get; } =
    [
        ModelGemini31FlashImage.Name,
        ModelGemini31FlashImagePreview.Name
    ];

    /// <summary>
    /// Returns whether the model supports video-to-image generation.
    /// </summary>
    public static bool SupportsVideoToImage(string? modelName)
    {
        return modelName is not null && VideoToImageModels.Contains(modelName);
    }

    /// <summary>
    /// All known Gemini image models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() =>
    [
        ModelGemini31FlashImage,
        ModelGemini31FlashImagePreview
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ImageModelGoogleGemini()
    {
    }
}
