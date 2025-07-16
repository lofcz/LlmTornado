using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Audio.Models.Mistral;

/// <summary>
/// Free (as in free weights) audio models from Mistral.
/// </summary>
public class AudioModelMistralFree : IVendorModelClassProvider
{
    /// <summary>
    /// Voxtral Mini (3B).
    /// </summary>
    public static readonly AudioModel ModelVoxtralMini2507 = new AudioModel("voxtral-mini-2507", LLmProviders.Mistral, 32_000);

    /// <summary>
    /// <inheritdoc cref="ModelVoxtralMini2507"/>
    /// </summary>
    public readonly AudioModel VoxtralMini2507 = ModelVoxtralMini2507;
    
    /// <summary>
    /// All known free models from Mistral.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelVoxtralMini2507
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal AudioModelMistralFree()
    {
        
    }
}