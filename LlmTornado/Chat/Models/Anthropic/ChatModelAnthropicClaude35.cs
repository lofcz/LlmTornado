using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude35 : IVendorModelClassProvider
{
    /// <summary>
    /// Most balanced model between intelligence and speed, a great choice for enterprise workloads and scaled AI deployments.
    /// </summary>
    public static readonly ChatModel ModelSonnet = new ChatModel("claude-3-5-sonnet-20240620", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    public readonly ChatModel Sonnet = ModelSonnet;
    
    /// <summary>
    /// All known Claude 3 models from Anthropic.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelSonnet
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude35()
    {

    }
}