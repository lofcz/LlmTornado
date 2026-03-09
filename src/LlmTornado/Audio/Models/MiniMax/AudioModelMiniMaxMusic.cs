using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.MiniMax;

/// <summary>
/// MiniMax music generation models.
/// </summary>
public class AudioModelMiniMaxMusic : IVendorModelClassProvider
{
    /// <summary>
    /// music-2.5 - MiniMax's music generation model. Generates songs from lyrics and a style prompt.
    /// Supports structure tags in lyrics: [Intro], [Verse], [Chorus], [Bridge], [Outro], etc.
    /// Output formats: mp3, wav, pcm. Configurable sample rate and bitrate.
    /// </summary>
    public static readonly AudioModel ModelMusic25 = new AudioModel("music-2.5", LLmProviders.MiniMax);
    
    /// <summary>
    /// <inheritdoc cref="ModelMusic25"/>
    /// </summary>
    public readonly AudioModel Music25 = ModelMusic25;
    
    /// <summary>
    /// All known music models from MiniMax.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMusic25
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal AudioModelMiniMaxMusic()
    {
        
    }
}
