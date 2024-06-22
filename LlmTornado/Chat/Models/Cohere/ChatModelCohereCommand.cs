using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelCohereCommand : IVendorModelClassProvider
{
    /// <summary>
    /// Command R+ is an instruction-following conversational model that performs language tasks at a higher quality, more reliably, and with a longer context than previous models. It is best suited for complex RAG workflows and multi-step tool use.
    /// </summary>
    public static readonly ChatModel ModelRPlus = new ChatModel("command-r-plus", LLmProviders.Cohere, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="CommandRPlus"/>
    /// </summary>
    public readonly ChatModel RPlus = ModelRPlus;
    
    /// <summary>
    /// Be advised that command-nightly is the latest, most experimental, and (possibly) unstable version of its default counterpart. Nightly releases are updated regularly, without warning, and are not recommended for production use.
    /// </summary>
    public static readonly ChatModel ModelNightly = new ChatModel("command-nightly", LLmProviders.Cohere, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelNightly"/>
    /// </summary>
    public readonly ChatModel CommandNightly = ModelNightly;
    
    /// <summary>
    /// An instruction-following conversational model that performs language tasks with high quality, more reliably and with a longer context than our base generative models.
    /// </summary>
    public static readonly ChatModel ModelDefault = new ChatModel("command", LLmProviders.Cohere, 4_000);

    /// <summary>
    /// <inheritdoc cref="ModelDefault"/>
    /// </summary>
    public readonly ChatModel Default = ModelDefault;
    
    /// <summary>
    /// A smaller, faster version of command. Almost as capable, but a lot faster.
    /// </summary>
    public static readonly ChatModel ModelLight = new ChatModel("command-light", LLmProviders.Cohere, 4_000);

    /// <summary>
    /// <inheritdoc cref="ModelLight"/>
    /// </summary>
    public readonly ChatModel CommandLight = ModelLight;
    
    /// <summary>
    /// Be advised that command-light-nightly is the latest, most experimental, and (possibly) unstable version of its default counterpart. Nightly releases are updated regularly, without warning, and are not recommended for production use.
    /// </summary>
    public static readonly ChatModel ModelLightNightly = new ChatModel("command-light-nightly", LLmProviders.Cohere, 4_000);

    /// <summary>
    /// <inheritdoc cref="ModelLightNightly"/>
    /// </summary>
    public readonly ChatModel CommandLightNightly = ModelLightNightly;
    
    /// <summary>
    /// All known Coral models from Cohere.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelRPlus,
        ModelNightly,
        ModelDefault,
        ModelLight,
        ModelLightNightly
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelCohereCommand()
    {

    }
}