using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelCohereCoral : IVendorModelClassProvider
{
    /// <summary>
    /// Command R+ is an instruction-following conversational model that performs language tasks at a higher quality, more reliably, and with a longer context than previous models. It is best suited for complex RAG workflows and multi-step tool use.
    /// </summary>
    public static readonly ChatModel ModelCommandRPlus = new ChatModel("command-r-plus", LLmProviders.Cohere, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandRPlus"/>
    /// </summary>
    public readonly ChatModel CommandRPlus = ModelCommandRPlus;

    /// <summary>
    /// All known Coral models from Cohere.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelCommandRPlus
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelCohereCoral()
    {

    }
}