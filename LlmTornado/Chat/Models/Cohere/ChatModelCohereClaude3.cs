using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelCohereClaude3 : IVendorModelClassProvider
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
    /// Be advised that command-nightly is the latest, most experimental, and (possibly) unstable version of its default counterpart. Nightly releases are updated regularly, without warning, and are not recommended for production use.
    /// </summary>
    public static readonly ChatModel ModelCommandNightly = new ChatModel("command-nightly", LLmProviders.Cohere, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelCommandNightly"/>
    /// </summary>
    public readonly ChatModel CommandNightly = ModelCommandNightly;
    
    /// <summary>
    /// An instruction-following conversational model that performs language tasks with high quality, more reliably and with a longer context than our base generative models.
    /// </summary>
    public static readonly ChatModel ModelCommand = new ChatModel("command", LLmProviders.Cohere, 4_000);

    /// <summary>
    /// <inheritdoc cref="ModelCommand"/>
    /// </summary>
    public readonly ChatModel Command = ModelCommand;
    
    /// <summary>
    /// A smaller, faster version of command. Almost as capable, but a lot faster.
    /// </summary>
    public static readonly ChatModel ModelCommandLight = new ChatModel("command-light", LLmProviders.Cohere, 4_000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandLight"/>
    /// </summary>
    public readonly ChatModel CommandLight = ModelCommandLight;
    
    /// <summary>
    /// Be advised that command-light-nightly is the latest, most experimental, and (possibly) unstable version of its default counterpart. Nightly releases are updated regularly, without warning, and are not recommended for production use.
    /// </summary>
    public static readonly ChatModel ModelCommandLightNightly = new ChatModel("command-light-nightly", LLmProviders.Cohere, 4_000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandLightNightly"/>
    /// </summary>
    public readonly ChatModel CommandLightNightly = ModelCommandLightNightly;
    
    /// <summary>
    /// All known Coral models from Cohere.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelCommandRPlus,
        ModelCommandNightly,
        ModelCommand,
        ModelCommandLight,
        ModelCommandLightNightly
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelCohereClaude3()
    {

    }
}