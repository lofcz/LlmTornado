using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Mistral;

/// <summary>
/// All Premier (closed-weights) models from Mistral.
/// </summary>
public class ChatModelMistralPremier : IVendorModelClassProvider
{
    /// <summary>
    /// Our cutting-edge language model for coding with the second version released January 2025, Codestral specializes in low-latency, high-frequency tasks such as fill-in-the-middle (FIM), code correction and test generation.
    /// </summary>
    public static readonly ChatModel ModelCodestral = new ChatModel("codestral-latest", LLmProviders.Mistral, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelCodestral"/>
    /// </summary>
    public readonly ChatModel Codestral = ModelCodestral;
    
    /// <summary>
    /// Our top-tier reasoning model for high-complexity tasks with the lastest version released November 2024. 
    /// </summary>
    public static readonly ChatModel ModelMistralLarge = new ChatModel("mistral-large-latest", LLmProviders.Mistral, 131_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralLarge"/>
    /// </summary>
    public readonly ChatModel MistralLarge = ModelMistralLarge;
    
    /// <summary>
    /// Our frontier-class multimodal model released November 2024.
    /// </summary>
    public static readonly ChatModel ModelPixtralLarge = new ChatModel("pixtral-large-latest", LLmProviders.Mistral, 131_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPixtralLarge"/>
    /// </summary>
    public readonly ChatModel PixtralLarge = ModelPixtralLarge;
    
    /// <summary>
    /// A powerful and efficient model for languages from the Middle East and South Asia.
    /// </summary>
    public static readonly ChatModel ModelMistralSaba = new ChatModel("mistral-saba-latest", LLmProviders.Mistral, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSaba"/>
    /// </summary>
    public readonly ChatModel MistralSaba = ModelMistralSaba;
    
    /// <summary>
    /// World’s best edge model.
    /// </summary>
    public static readonly ChatModel ModelMinistral3B = new ChatModel("ministral-3b-latest", LLmProviders.Mistral, 131_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMinistral3B"/>
    /// </summary>
    public readonly ChatModel Ministral3B = ModelMinistral3B;
    
    /// <summary>
    /// Powerful edge model with extremely high performance/price ratio.
    /// </summary>
    public static readonly ChatModel ModelMinistral8B = new ChatModel("ministral-8b-latest", LLmProviders.Mistral, 131_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMinistral8B"/>
    /// </summary>
    public readonly ChatModel Ministral8B = ModelMinistral8B;
    
    /// <summary>
    /// All known Premier models from Mistral.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelMistralLarge,
        ModelPixtralLarge,
        ModelMistralSaba,
        ModelMinistral3B,
        ModelMinistral8B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMistralPremier()
    {

    }
}