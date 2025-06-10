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
    /// Building upon Mistral Small 3.1 (2503), with added reasoning capabilities, undergoing SFT from Magistral Medium traces and RL on top, it's a small, efficient reasoning model with 24B parameters.
    /// </summary>
    public static readonly ChatModel ModelMagistralSmall = new ChatModel("magistral-small-2506", LLmProviders.Mistral, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMagistralSmall"/>
    /// </summary>
    public readonly ChatModel MagistralSmall = ModelMagistralSmall;
    
    /// <summary>
    /// A 24B text model, open source model that excels at using tools to explore codebases, editing multiple files and power software engineering agents.
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall = new ChatModel("devstral-small-2505", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall"/>
    /// </summary>
    public readonly ChatModel DevstralSmall = ModelDevstralSmall;
    
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
    public static readonly List<IModel> ModelsAll =
    [
        ModelMistralSmall2503,
        ModelMistralSmall,
        ModelPixtral,
        ModelDevstralSmall,
        ModelMagistralSmall
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMistralFree()
    {

    }
}