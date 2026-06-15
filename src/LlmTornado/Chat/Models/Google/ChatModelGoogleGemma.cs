using System;
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
    /// Highly efficient 26B Mixture-of-Experts model designed for high-throughput, advanced reasoning. Supports text and image input with a 256K context window.
    /// </summary>
    public static readonly ChatModel Model426BA4BIt = new ChatModel("gemma-4-26b-a4b-it", LLmProviders.Google, 256_000);

    /// <summary>
    /// <inheritdoc cref="Model426BA4BIt"/>
    /// </summary>
    public readonly ChatModel V426BA4BIt = Model426BA4BIt;

    /// <summary>
    /// Powerful 31B dense model bridging server-grade performance and local execution. Supports text and image input with a 256K context window.
    /// </summary>
    public static readonly ChatModel Model431BIt = new ChatModel("gemma-4-31b-it", LLmProviders.Google, 256_000);

    /// <summary>
    /// <inheritdoc cref="Model431BIt"/>
    /// </summary>
    public readonly ChatModel V431BIt = Model431BIt;
    
    /// <summary>
    /// All known Gemma models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelV327B, Model3Ne4B, Model426BA4BIt, Model431BIt]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGemma()
    {

    }
}