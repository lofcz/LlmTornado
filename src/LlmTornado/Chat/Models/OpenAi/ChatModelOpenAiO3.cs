using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// O3 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiO3 : IVendorModelClassProvider
{
    /// <summary>
    /// Latest o3 mini model snapshot.
    /// </summary>
    public static readonly ChatModel ModelMini250131 = new ChatModel("o3-mini-2025-01-31", LLmProviders.OpenAi, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelMini250131"/>
    /// </summary>
    public readonly ChatModel Mini250131 = ModelMini250131;
    
    /// <summary>
    /// O3 is a well-rounded and powerful model across domains. It sets a new standard for math, science, coding, and visual reasoning tasks. It also excels at technical writing and instruction-following. Use it to think through multi-step problems that involve analysis across text, code, and images.
    /// </summary>
    public static readonly ChatModel ModelV3 = new ChatModel("o3", LLmProviders.OpenAi, 200_000, [ "o3-2025-04-16" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV3"/>
    /// </summary>
    public readonly ChatModel V3 = ModelV3;
    
    /// <summary>
    /// Latest o3 mini model snapshot.
    /// </summary>
    public static readonly ChatModel ModelMini = new ChatModel("o3-mini", LLmProviders.OpenAi, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMini"/>
    /// </summary>
    public readonly ChatModel Mini = ModelMini;
    
    public static readonly List<IModel> ModelsAll = [
        ModelMini250131,
        ModelMini,
        ModelV3
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiO3()
    {
        
    }
}