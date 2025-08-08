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
    /// An enterprise grade text model, that excels at using tools to explore codebases, editing multiple files and power software engineering agents.
    /// </summary>
    public static readonly ChatModel ModelDevstralMedium2507 = new ChatModel("devstral-medium-2507", LLmProviders.Mistral, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDevstralMedium2507"/>
    /// </summary>
    public readonly ChatModel DevstralMedium2507 = ModelDevstralMedium2507;
    
    /// <summary>
    /// Our frontier-class reasoning model released June 2025. 
    /// </summary>
    public static readonly ChatModel ModelMagistralMedium2507 = new ChatModel("magistral-medium-2507", LLmProviders.Mistral, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMagistralMedium2507"/>
    /// </summary>
    public readonly ChatModel MagistralMedium2507 = ModelMagistralMedium2507;
    
    /// <summary>
    /// Our frontier-class reasoning model released June 2025. 
    /// </summary>
    public static readonly ChatModel ModelMagistralMedium2506 = new ChatModel("magistral-medium-2506", LLmProviders.Mistral, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMagistralMedium2506"/>
    /// </summary>
    public readonly ChatModel MagistralMedium2506 = ModelMagistralMedium2506;
    
    /// <summary>
    /// Our cutting-edge language model for coding released end of July 2025, Codestral specializes in low-latency, high-frequency tasks such as fill-in-the-middle (FIM), code correction and test generation.
    /// </summary>
    public static readonly ChatModel ModelCodestral2508 = new ChatModel("codestral-2508", LLmProviders.Mistral, 256_000, [ "codestral-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelCodestral2508"/>
    /// </summary>
    public readonly ChatModel Codestral2508 = ModelCodestral2508;
    
    /// <summary>
    /// Our cutting-edge language model for coding with the second version released January 2025, Codestral specializes in low-latency, high-frequency tasks such as fill-in-the-middle (FIM), code correction and test generation.
    /// </summary>
    public static readonly ChatModel ModelCodestral2501 = new ChatModel("codestral-2501", LLmProviders.Mistral, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelCodestral2501"/>
    /// </summary>
    public readonly ChatModel Codestral2501 = ModelCodestral2501;
    
    /// <summary>
    /// Our top-tier reasoning model for high-complexity tasks with the lastest version released November 2024. 
    /// </summary>
    public static readonly ChatModel ModelMistralLarge = new ChatModel("mistral-large-2411", LLmProviders.Mistral, 128_000, [ "mistral-large-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralLarge"/>
    /// </summary>
    public readonly ChatModel MistralLarge = ModelMistralLarge;
    
    /// <summary>
    /// Our frontier-class multimodal model released November 2024.
    /// </summary>
    public static readonly ChatModel ModelPixtralLarge = new ChatModel("pixtral-large-2411", LLmProviders.Mistral, 128_000, [ "pixtral-large-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelPixtralLarge"/>
    /// </summary>
    public readonly ChatModel PixtralLarge = ModelPixtralLarge;
    
    /// <summary>
    /// A powerful and efficient model for languages from the Middle East and South Asia.
    /// </summary>
    public static readonly ChatModel ModelMistralSaba = new ChatModel("mistral-saba-2502", LLmProviders.Mistral, 32_000, [ "mistral-saba-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSaba"/>
    /// </summary>
    public readonly ChatModel MistralSaba = ModelMistralSaba;
    
    /// <summary>
    /// World’s best edge model.
    /// </summary>
    public static readonly ChatModel ModelMinistral3B = new ChatModel("ministral-3b-2410", LLmProviders.Mistral, 128_000, [ "ministral-3b-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelMinistral3B"/>
    /// </summary>
    public readonly ChatModel Ministral3B = ModelMinistral3B;
    
    /// <summary>
    /// Powerful edge model with extremely high performance/price ratio.
    /// </summary>
    public static readonly ChatModel ModelMinistral8B = new ChatModel("ministral-8b-2410", LLmProviders.Mistral, 128_000, [ "ministral-8b-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelMinistral8B"/>
    /// </summary>
    public readonly ChatModel Ministral8B = ModelMinistral8B;
    
    /// <summary>
    /// Mistral Medium 3: frontier-class multimodal model released May 2025.
    /// </summary>
    public static readonly ChatModel ModelMedium3 = new ChatModel("mistral-medium-2505", LLmProviders.Mistral, 128_000, [ "mistral-medium-latest" ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelMedium3"/>
    /// </summary>
    public readonly ChatModel Medium3 = ModelMedium3;
    
    /// <summary>
    /// All known Premier models from Mistral.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelMistralLarge,
        ModelPixtralLarge,
        ModelMistralSaba,
        ModelMinistral3B,
        ModelMinistral8B,
        ModelMedium3,
        ModelMagistralMedium2506,
        ModelMagistralMedium2507,
        ModelDevstralMedium2507,
        ModelCodestral2501,
        ModelCodestral2508
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMistralPremier()
    {

    }
}