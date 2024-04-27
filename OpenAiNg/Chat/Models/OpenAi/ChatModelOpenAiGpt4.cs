using System.Collections.Generic;
using OpenAiNg.Code;
using OpenAiNg.Code.Models;

namespace OpenAiNg.Chat.Models;

/// <summary>
/// GPT-4 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiGpt4 : IVendorModelClassProvider
{
    /// <summary>
    /// The latest GPT-4 Turbo model with vision capabilities. Vision requests can now use JSON mode and function calling. Currently points to gpt-4-turbo-2024-04-09.
    /// </summary>
    public static readonly ChatModel ModelTurbo = new ChatModel("gpt-4-turbo", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelTurbo"/>
    /// </summary>
    public readonly ChatModel Turbo = ModelTurbo;
    
    /// <summary>
    /// GPT-4 Turbo with Vision model. Vision requests can now use JSON mode and function calling. gpt-4-turbo currently points to this version.
    /// </summary>
    public static readonly ChatModel ModelTurbo240409 = new ChatModel("gpt-4-turbo-2024-04-09", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelTurbo240409"/>
    /// </summary>
    public readonly ChatModel Turbo240409 = ModelTurbo240409;
    
    /// <summary>
    /// GPT-4 Turbo preview model intended to reduce cases of “laziness” where the model doesn’t complete a task. Returns a maximum of 4,096 output tokens.
    /// </summary>
    public static readonly ChatModel ModelPreview240125 = new ChatModel("gpt-4-0125-preview", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelPreview240125"/>
    /// </summary>
    public readonly ChatModel Preview240125 = ModelPreview240125;
    
    /// <summary>
    /// GPT-4 Turbo preview model featuring improved instruction following, JSON mode, reproducible outputs, parallel function calling, and more. Returns a maximum of 4,096 output tokens. This is a preview model.
    /// </summary>
    public static readonly ChatModel ModelPreview231106 = new ChatModel("gpt-4-1106-preview", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelPreview231106"/>
    /// </summary>
    public readonly ChatModel Preview231106 = ModelPreview231106;
    
    /// <summary>
    /// GPT-4 model with the ability to understand images, in addition to all other GPT-4 Turbo capabilities. This is a preview model, we recommend developers to now use gpt-4-turbo which includes vision capabilities. Currently points to gpt-4-1106-vision-preview.
    /// </summary>
    public static readonly ChatModel ModelVisionPreview = new ChatModel("gpt-4-vision-preview", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelVisionPreview"/>
    /// </summary>
    public readonly ChatModel VisionPreview = ModelVisionPreview;
    
    /// <summary>
    /// GPT-4 model with the ability to understand images, in addition to all other GPT-4 Turbo capabilities. This is a preview model, we recommend developers to now use gpt-4-turbo which includes vision capabilities. Returns a maximum of 4,096 output tokens.
    /// </summary>
    public static readonly ChatModel ModelVisionPreview231106 = new ChatModel("gpt-4-1106-vision-preview", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelVisionPreview231106"/>
    /// </summary>
    public readonly ChatModel VisionPreview231106 = ModelVisionPreview231106;
    
    /// <summary>
    /// Currently points to gpt-4-0613.
    /// </summary>
    public static readonly ChatModel ModelDefault = new ChatModel("gpt-4", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelDefault"/>
    /// </summary>
    public readonly ChatModel Default = ModelDefault;
    
    /// <summary>
    /// Snapshot of gpt-4 from June 13th 2023 with improved function calling support.
    /// </summary>
    public static readonly ChatModel ModelPreview230613 = new ChatModel("gpt-4-0613", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelPreview230613"/>
    /// </summary>
    public readonly ChatModel Preview230613 = ModelPreview230613;
    
    /// <summary>
    /// Currently points to gpt-4-32k-0613.
    /// </summary>
    public static readonly ChatModel ModelContext32K = new ChatModel("gpt-4-32k", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelContext32K"/>
    /// </summary>
    public readonly ChatModel Context32K = ModelContext32K;
    
    /// <summary>
    /// Snapshot of gpt-4-32k from June 13th 2023 with improved function calling support. This model was never rolled out widely in favor of GPT-4 Turbo.
    /// </summary>
    public static readonly ChatModel ModelContext32K230613 = new ChatModel("gpt-4-32k-0613", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelContext32K230613"/>
    /// </summary>
    public readonly ChatModel Context32K230613 = ModelContext32K230613;
    
    /// <summary>
    /// All known GPT 3.5 models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelTurbo,
        ModelTurbo240409,
        ModelPreview240125,
        ModelPreview231106,
        ModelVisionPreview,
        ModelVisionPreview231106,
        ModelDefault,
        ModelPreview230613,
        ModelContext32K,
        ModelContext32K230613
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt4()
    {
        
    }
}