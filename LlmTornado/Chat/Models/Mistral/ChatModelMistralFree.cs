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
    /// A new leader in the small models category with the lastest version v3 released January 2025.
    /// </summary>
    public static readonly ChatModel ModelMistralSmall = new ChatModel("mistral-small-latest", LLmProviders.Mistral, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall"/>
    /// </summary>
    public readonly ChatModel MistralSmall = ModelMistralSmall;
    
    /// <summary>
    /// A 12B model with image understanding capabilities in addition to text. 
    /// </summary>
    public static readonly ChatModel ModelPixtral = new ChatModel("pixtral-12b-2409", LLmProviders.Mistral, 131_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPixtral"/>
    /// </summary>
    public readonly ChatModel Pixtral = ModelPixtral;
    
    /// <summary>
    /// All known Free models from Mistral.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelMistralSmall,
        ModelPixtral
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMistralFree()
    {

    }
}