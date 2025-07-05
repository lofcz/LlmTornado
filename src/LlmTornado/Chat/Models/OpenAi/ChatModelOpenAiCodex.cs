using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Codex class models from OpenAI.
/// </summary>
public class ChatModelOpenAiCodex : IVendorModelClassProvider
{
    /// <summary>
    /// Computer use is a practical application of our Computer-Using Agent (CUA) model, computer-use-preview, which combines the vision capabilities of GPT-4o with advanced reasoning to simulate controlling computer interfaces and performing tasks.
    /// </summary>
    public static readonly ChatModel ModelComputerUsePreview = new ChatModel("computer-use-preview", LLmProviders.OpenAi, 200_000)
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelComputerUsePreview"/>
    /// </summary>
    public readonly ChatModel ComputerUsePreview = ModelComputerUsePreview;

    
    /// <summary>
    /// codex-mini-latest is a fine-tuned version of o4-mini specifically for use in Codex CLI. For direct use in the API, we recommend starting with gpt-4.1.
    /// </summary>
    public static readonly ChatModel ModelMiniLatest = new ChatModel("codex-mini-latest", LLmProviders.OpenAi, 200_000)
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelMiniLatest"/>
    /// </summary>
    public readonly ChatModel MiniLatest = ModelMiniLatest;

    /// <summary>
    /// All known Codex models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelMiniLatest,
        ModelComputerUsePreview
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiCodex()
    {
        
    }
}