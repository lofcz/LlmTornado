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
    /// codex-mini-latest is a fine-tuned version of o4-mini specifically for use in Codex CLI. For direct use in the API, we recommend starting with gpt-4.1.
    /// </summary>
    public static readonly ChatModel ModelMiniLatest = new ChatModel("codex-mini-latest", LLmProviders.OpenAi, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelMiniLatest"/>
    /// </summary>
    public readonly ChatModel MiniLatest = ModelMiniLatest;

    /// <summary>
    /// All known Codex models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelMiniLatest
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiCodex()
    {
        
    }
}