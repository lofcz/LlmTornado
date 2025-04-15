using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// GPT-4.5 class models from OpenAI.
/// </summary>
[Obsolete("Will be removed by OpenAI in 3 months")]
public class ChatModelOpenAiGpt45 : IVendorModelClassProvider
{
    /// <summary>
    /// Latest snapshot of GPT-4.5, currently points to <see cref="ModelPreview250227"/>.
    /// </summary>
    public static readonly ChatModel ModelPreview = new ChatModel("gpt-4.5-preview", LLmProviders.OpenAi, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelPreview"/>
    /// </summary>
    public readonly ChatModel Preview = ModelPreview;
    
    /// <summary>
    /// GPT-4.5 excels at tasks that benefit from creative, open-ended thinking and conversation, such as writing, learning, or exploring new ideas.
    /// </summary>
    public static readonly ChatModel ModelPreview250227 = new ChatModel("gpt-4.5-preview-2025-02-27", LLmProviders.OpenAi, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelPreview250227"/>
    /// </summary>
    public readonly ChatModel Preview250227 = ModelPreview250227;
    
    /// <summary>
    /// All known GPT-4.5 models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelPreview,
        ModelPreview250227
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt45()
    {
        
    }
}