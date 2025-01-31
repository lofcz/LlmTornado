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
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiO3()
    {
        
    }
}