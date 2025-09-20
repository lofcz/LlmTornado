using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.MoonshotAi;

/// <summary>
/// All models from Moonshot AI.
/// </summary>
public class ChatModelMoonshotAiModels : IVendorModelClassProvider
{
    /// <summary>
    /// kimi-k2-0905-preview
    /// </summary>
    public static readonly ChatModel ModelKimiK20905Preview = new ChatModel("kimi-k2-0905-preview", LLmProviders.MoonshotAi, 262_144);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiK20905Preview"/>
    /// </summary>
    public readonly ChatModel KimiK20905Preview = ModelKimiK20905Preview;
    
    /// <summary>
    /// kimi-k2-0711-preview
    /// </summary>
    public static readonly ChatModel ModelKimiK20711Preview = new ChatModel("kimi-k2-0711-preview", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiK20711Preview"/>
    /// </summary>
    public readonly ChatModel KimiK20711Preview = ModelKimiK20711Preview;
    
    /// <summary>
    /// kimi-k2-turbo-preview
    /// </summary>
    public static readonly ChatModel ModelKimiK2TurboPreview = new ChatModel("kimi-k2-turbo-preview", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiK2TurboPreview"/>
    /// </summary>
    public readonly ChatModel KimiK2TurboPreview = ModelKimiK2TurboPreview;
    
    /// <summary>
    /// moonshot-v1-8k
    /// </summary>
    public static readonly ChatModel ModelMoonshotV18k = new ChatModel("moonshot-v1-8k", LLmProviders.MoonshotAi, 8_192);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV18k"/>
    /// </summary>
    public readonly ChatModel MoonshotV18k = ModelMoonshotV18k;
    
    /// <summary>
    /// moonshot-v1-32k
    /// </summary>
    public static readonly ChatModel ModelMoonshotV132k = new ChatModel("moonshot-v1-32k", LLmProviders.MoonshotAi, 32_768);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV132k"/>
    /// </summary>
    public readonly ChatModel MoonshotV132k = ModelMoonshotV132k;
    
    /// <summary>
    /// moonshot-v1-128k
    /// </summary>
    public static readonly ChatModel ModelMoonshotV1128k = new ChatModel("moonshot-v1-128k", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV1128k"/>
    /// </summary>
    public readonly ChatModel MoonshotV1128k = ModelMoonshotV1128k;
    
    /// <summary>
    /// moonshot-v1-auto
    /// </summary>
    public static readonly ChatModel ModelMoonshotV1Auto = new ChatModel("moonshot-v1-auto", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV1Auto"/>
    /// </summary>
    public readonly ChatModel MoonshotV1Auto = ModelMoonshotV1Auto;
    
    /// <summary>
    /// kimi-latest
    /// </summary>
    public static readonly ChatModel ModelKimiLatest = new ChatModel("kimi-latest", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiLatest"/>
    /// </summary>
    public readonly ChatModel KimiLatest = ModelKimiLatest;
    
    /// <summary>
    /// moonshot-v1-8k-vision-preview
    /// </summary>
    public static readonly ChatModel ModelMoonshotV18kVisionPreview = new ChatModel("moonshot-v1-8k-vision-preview", LLmProviders.MoonshotAi, 8_192);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV18kVisionPreview"/>
    /// </summary>
    public readonly ChatModel MoonshotV18kVisionPreview = ModelMoonshotV18kVisionPreview;
    
    /// <summary>
    /// moonshot-v1-32k-vision-preview
    /// </summary>
    public static readonly ChatModel ModelMoonshotV132kVisionPreview = new ChatModel("moonshot-v1-32k-vision-preview", LLmProviders.MoonshotAi, 32_768);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV132kVisionPreview"/>
    /// </summary>
    public readonly ChatModel MoonshotV132kVisionPreview = ModelMoonshotV132kVisionPreview;
    
    /// <summary>
    /// moonshot-v1-128k-vision-preview
    /// </summary>
    public static readonly ChatModel ModelMoonshotV1128kVisionPreview = new ChatModel("moonshot-v1-128k-vision-preview", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelMoonshotV1128kVisionPreview"/>
    /// </summary>
    public readonly ChatModel MoonshotV1128kVisionPreview = ModelMoonshotV1128kVisionPreview;
    
    /// <summary>
    /// kimi-thinking-preview
    /// </summary>
    public static readonly ChatModel ModelKimiThinkingPreview = new ChatModel("kimi-thinking-preview", LLmProviders.MoonshotAi, 131_072);
    
    /// <summary>
    /// <inheritdoc cref="ModelKimiThinkingPreview"/>
    /// </summary>
    public readonly ChatModel KimiThinkingPreview = ModelKimiThinkingPreview;
    
    /// <summary>
    /// All known models from Moonshot AI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelKimiK20905Preview,
        ModelKimiK20711Preview,
        ModelKimiK2TurboPreview,
        ModelMoonshotV18k,
        ModelMoonshotV132k,
        ModelMoonshotV1128k,
        ModelMoonshotV1Auto,
        ModelKimiLatest,
        ModelMoonshotV18kVisionPreview,
        ModelMoonshotV132kVisionPreview,
        ModelMoonshotV1128kVisionPreview,
        ModelKimiThinkingPreview
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMoonshotAiModels()
    {

    }
}