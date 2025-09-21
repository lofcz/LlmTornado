using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Mistral;

/// <summary>
/// All Free (as in open-weights) models from Mistral.
/// </summary>
public class ChatModelMistralFree : IVendorModelClassProvider
{
    /// <summary>
    /// Voxtral Small (24B).
    /// </summary>
    public static readonly ChatModel ModelVoxtralSmall2507 = new ChatModel("voxtral-small-2507", LLmProviders.Mistral, 32_000, [ "voxtral-small-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelVoxtralSmall2507"/>
    /// </summary>
    public readonly ChatModel VoxtralSmall2507 = ModelVoxtralSmall2507;
    
    /// <summary>
    /// Voxtral Mini (3B).
    /// </summary>
    public static readonly ChatModel ModelVoxtralMini2507 = new ChatModel("voxtral-mini-2507", LLmProviders.Mistral, 32_000, [ "voxtral-mini-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelVoxtralMini2507"/>
    /// </summary>
    public readonly ChatModel VoxtralMini2507 = ModelVoxtralMini2507;
    
    /// <summary>
    /// Devstral Small 1.1 (24B)
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall2507 = new ChatModel("devstral-small-2507", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall2507"/>
    /// </summary>
    public readonly ChatModel DevstralSmall2507 = ModelDevstralSmall2507;
    
    /// <summary>
    /// Mistral Small 3.2
    /// </summary>
    public static readonly ChatModel ModelMistralSmall2506 = new ChatModel("mistral-small-2506", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall2506"/>
    /// </summary>
    public readonly ChatModel MistralSmall2506 = ModelMistralSmall2506;
    
    /// <summary>
    /// Our small reasoning model released September 2025 with vision support.
    /// </summary>
    public static readonly ChatModel ModelMagistralSmall2509 = new ChatModel("magistral-small-2509", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMagistralSmall2509"/>
    /// </summary>
    public readonly ChatModel MagistralSmall2509 = ModelMagistralSmall2509;
    
    /// <summary>
    /// Building upon Mistral Small 3.1 (2503), with added reasoning capabilities, undergoing SFT from Magistral Medium traces and RL on top, it's a small, efficient reasoning model with 24B parameters.
    /// </summary>
    public static readonly ChatModel ModelMagistralSmall2507 = new ChatModel("magistral-small-2507", LLmProviders.Mistral, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMagistralSmall2507"/>
    /// </summary>
    public readonly ChatModel MagistralSmall2507 = ModelMagistralSmall2507;
    
    /// <summary>
    /// Building upon Mistral Small 3.1 (2503), with added reasoning capabilities, undergoing SFT from Magistral Medium traces and RL on top, it's a small, efficient reasoning model with 24B parameters.
    /// </summary>
    public static readonly ChatModel ModelMagistralSmall2506 = new ChatModel("magistral-small-2506", LLmProviders.Mistral, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMagistralSmall2506"/>
    /// </summary>
    public readonly ChatModel MagistralSmall2506 = ModelMagistralSmall2506;
    
    /// <summary>
    /// A 24B text model, open source model that excels at using tools to explore codebases, editing multiple files and power software engineering agents.
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall2505 = new ChatModel("devstral-small-2505", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall2505"/>
    /// </summary>
    public readonly ChatModel DevstralSmall = ModelDevstralSmall2505;
    
    /// <summary>
    /// A new leader in the small models category with image understanding capabilities, with the lastest version v3.1 released March 2025.
    /// </summary>
    public static readonly ChatModel ModelMistralSmall = new ChatModel("mistral-small-latest", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall"/>
    /// </summary>
    public readonly ChatModel MistralSmall = ModelMistralSmall;
    
    /// <summary>
    /// A new leader in the small models category with image understanding capabilities, with the lastest version v3.1 released March 2025.
    /// </summary>
    public static readonly ChatModel ModelMistralSmall2503 = new ChatModel("mistral-small-2503", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall2503"/>
    /// </summary>
    public readonly ChatModel MistralSmall2503 = ModelMistralSmall2503;
    
    /// <summary>
    /// A 12B model with image understanding capabilities in addition to text. 
    /// </summary>
    public static readonly ChatModel ModelPixtral = new ChatModel("pixtral-12b-2409", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPixtral"/>
    /// </summary>
    public readonly ChatModel Pixtral = ModelPixtral;
    
    /// <summary>
    /// All known Free models from Mistral.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelMistralSmall2503, ModelMistralSmall, ModelPixtral, ModelDevstralSmall2505, ModelMagistralSmall2506, ModelMagistralSmall2507, 
        ModelMistralSmall2506, ModelDevstralSmall2507, ModelVoxtralSmall2507, ModelVoxtralMini2507, ModelMagistralSmall2509
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMistralFree()
    {

    }
}