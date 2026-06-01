using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.Google;

/// <summary>
/// Google Lyria 3 music generation models.
/// </summary>
public class AudioModelGoogleLyria : IVendorModelClassProvider
{
    /// <summary>
    /// lyria-3-clip-preview - Generates 30-second MP3 clips from text or image prompts.
    /// Supports vocals, timed lyrics, and full instrumental arrangements at 44.1 kHz stereo.
    /// </summary>
    public static readonly AudioModel ModelLyria3ClipPreview = new AudioModel("lyria-3-clip-preview", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelLyria3ClipPreview"/>
    /// </summary>
    public readonly AudioModel Lyria3ClipPreview = ModelLyria3ClipPreview;
    
    /// <summary>
    /// lyria-3-pro-preview - Generates full-length songs (a couple of minutes) with verses, choruses, and bridges.
    /// Supports text and image inputs, optional WAV output, at 44.1 kHz stereo.
    /// </summary>
    public static readonly AudioModel ModelLyria3ProPreview = new AudioModel("lyria-3-pro-preview", LLmProviders.Google);
    
    /// <summary>
    /// <inheritdoc cref="ModelLyria3ProPreview"/>
    /// </summary>
    public readonly AudioModel Lyria3ProPreview = ModelLyria3ProPreview;
    
    /// <summary>
    /// All known Lyria music models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelLyria3ClipPreview,
        ModelLyria3ProPreview
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal AudioModelGoogleLyria()
    {
        
    }
}
