using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Gemma class models from Google.
/// </summary>
public class ChatModelGoogleGemma : IVendorModelClassProvider
{
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks (stable).
    /// </summary>
    public static readonly ChatModel Model3Ne4B = new ChatModel("gemma-3n-e4b-it", LLmProviders.Google, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="Model3Ne4B"/>
    /// </summary>
    public readonly ChatModel V3Ne4B = Model3Ne4B;
    
    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks (stable).
    /// </summary>
    public static readonly ChatModel ModelV327B = new ChatModel("gemma-3-27b-it", LLmProviders.Google, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelV327B"/>
    /// </summary>
    public readonly ChatModel V327B = ModelV327B;
    
    /// <summary>
    /// All known Gemma models from Google.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelV327B,
        Model3Ne4B
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGemma()
    {

    }
}