using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Preview Gemini class models from Google.
/// Similar to <see cref="ChatModelGoogleGeminiExperimental"/> but billing-enabled.
/// </summary>
public class ChatModelGoogleGeminiPreview : IVendorModelClassProvider
{
    /// <summary>
    /// Gemini 3 Flash is our latest 3-series model, with Pro-level intelligence at the speed and pricing of Flash.
    /// </summary>
    public static readonly ChatModel ModelGemini3FlashPreview = new ChatModel("gemini-3-flash-preview", LLmProviders.Google, 1_048_576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini3FlashPreview"/>
    /// </summary>
    public readonly ChatModel Gemini3FlashPreview = ModelGemini3FlashPreview;
    
    /// <summary>
    /// Gemini 3 Pro is the first model in the new series. gemini-3-pro-preview is best for your complex tasks that require broad world knowledge and advanced reasoning across modalities.
    /// </summary>
    public static readonly ChatModel ModelGemini3ProPreview = new ChatModel("gemini-3-pro-preview", LLmProviders.Google, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelGemini3ProPreview"/>
    /// </summary>
    public readonly ChatModel Gemini3ProPreview = ModelGemini3ProPreview;

    /// <summary>
    /// Gemini 3.1 Pro Preview is the next iteration of performance, behavior, and intelligence improvements in the 3 Pro family.
    /// Features better thinking, improved token efficiency, and a more grounded, factually consistent experience.
    /// Optimized for software engineering behavior, agentic workflows, precise tool usage, and reliable multi-step execution.
    /// Input: Text, Image, Video, Audio, PDF. Output: Text. Context: 1M in / 64k out.
    /// </summary>
    public static readonly ChatModel ModelGemini31ProPreview = new ChatModel("gemini-3.1-pro-preview", LLmProviders.Google, 1_048_576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini31ProPreview"/>
    /// </summary>
    public readonly ChatModel Gemini31ProPreview = ModelGemini31ProPreview;

    /// <summary>
    /// Variant of <see cref="ModelGemini31ProPreview"/> optimized for agentic workflows that prioritize custom tools over built-in bash commands.
    /// Use this endpoint when the standard model ignores your custom tools in favor of bash.
    /// </summary>
    public static readonly ChatModel ModelGemini31ProPreviewCustomtools = new ChatModel("gemini-3.1-pro-preview-customtools", LLmProviders.Google, 1_048_576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini31ProPreviewCustomtools"/>
    /// </summary>
    public readonly ChatModel Gemini31ProPreviewCustomtools = ModelGemini31ProPreviewCustomtools;
    
    /// <summary>
    /// Gemini 3 Pro Image Preview is a state-of-the-art image generation and editing model optimized for professional asset production.
    /// Features high-resolution output (1K, 2K, 4K), advanced text rendering, Google Search grounding, and thinking mode.
    /// Supports up to 14 reference images for composition and character consistency.
    /// </summary>
    public static readonly ChatModel ModelGemini3ProImagePreview = new ChatModel("gemini-3-pro-image-preview", LLmProviders.Google, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelGemini3ProImagePreview"/>
    /// </summary>
    public readonly ChatModel Gemini3ProImagePreview = ModelGemini3ProImagePreview;
    
    /// <summary>
    /// Gemini 3.1 Flash Image Preview delivers high-quality, photorealistic imagery at Flash speed.
    /// Features subject consistency (up to 5 characters), object fidelity (up to 14 objects),
    /// precise instruction following, and production-ready output from 512px to 4K.
    /// </summary>
    public static readonly ChatModel ModelGemini31FlashImagePreview = new ChatModel("gemini-3.1-flash-image-preview", LLmProviders.Google, 1_048_576);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini31FlashImagePreview"/>
    /// </summary>
    public readonly ChatModel Gemini31FlashImagePreview = ModelGemini31FlashImagePreview;
    
    /// <summary>
    /// Gemini 2.5 Computer Use Preview model enables building browser control agents that interact with and automate tasks using screenshots and UI actions like mouse clicks and keyboard inputs.
    /// </summary>
    public static readonly ChatModel ModelGemini25ComputerUsePreview102025 = new ChatModel("gemini-2.5-computer-use-preview-10-2025", LLmProviders.Google, 1_048_576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25ComputerUsePreview102025"/>
    /// </summary>
    public readonly ChatModel Gemini25ComputerUsePreview102025 = ModelGemini25ComputerUsePreview102025;

    /// <summary>
    /// Gemini Robotics-ER, short for Gemini Robotics-Embodied Reasoning, is a thinking model that enhances robots' abilities to understand and interact with the physical world.
    /// </summary>
    public static readonly ChatModel ModelGeminiRoboticsRe15Preview = new ChatModel("gemini-robotics-er-1.5-preview", LLmProviders.Google, 1_048_576);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiRoboticsRe15Preview"/>
    /// </summary>
    public readonly ChatModel GeminiRoboticsRe15Preview = ModelGeminiRoboticsRe15Preview;
    
    /// <summary>
    /// The latest adaptive thinking, cost efficient model
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreview0925 = new ChatModel("gemini-2.5-flash-preview-09-2025", LLmProviders.Google, 1_048_576);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreview0925"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreview0925 = ModelGemini25FlashPreview0925;
    
    /// <summary>
    /// The latest model based on the Gemini 2.5 Flash lite model optimized for cost-efficiency, high throughput and high quality.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLitePreview0925 = new ChatModel("gemini-2.5-flash-lite-preview-09-2025", LLmProviders.Google, 1_048_576);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLitePreview0925"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLitePreview0925 = ModelGemini25FlashLitePreview0925;
    
    /// <summary>
    /// Gemini 2.5 Flash Image Preview is our latest, fastest, and most efficient natively multimodal model that lets you generate and edit images conversationally.
    /// </summary>
    [Obsolete("Shut down January 15, 2026. Use Gemini3ProImagePreview instead.")]
    public static readonly ChatModel ModelGemini25FlashImagePreview = new ChatModel("gemini-2.5-flash-image-preview", LLmProviders.Google, 32_768);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashImagePreview"/>
    /// </summary>
    [Obsolete("Shut down January 15, 2026. Use Gemini3ProImagePreview instead.")]
    public readonly ChatModel Gemini25FlashImagePreview = ModelGemini25FlashImagePreview;
    
    /// <summary>
    /// A Gemini 2.5 Flash model optimized for cost efficiency and low latency.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLitePreview0617 = new ChatModel("gemini-2.5-flash-lite-preview-06-17", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 512,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1, 0 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLitePreview0617"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLitePreview0617 = ModelGemini25FlashLitePreview0617;
    
    /// <summary>
    /// Gemini 2.5 Flash Preview TTS is our price-performant text-to-speech model, delivering high control and transparency for structured workflows like podcast generation, audiobooks, customer support, and more. Gemini 2.5 Flash rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreviewTts = new ChatModel("gemini-2.5-flash-preview-tts", LLmProviders.Google, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreviewTts"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreviewTts = ModelGemini25FlashPreviewTts;
    
    /// <summary>
    /// Gemini 2.5 Pro Preview TTS is our most powerful text-to-speech model, delivering high control and transparency for structured workflows like podcast generation, audiobooks, customer support, and more. Gemini 2.5 Pro rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreviewTts = new ChatModel("gemini-2.5-pro-preview-tts", LLmProviders.Google, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreviewTts"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreviewTts = ModelGemini25ProPreviewTts;
    
    /// <summary>
    /// Our best model in terms of price-performance, offering well-rounded capabilities. Gemini 2.5 Flash rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreview0520 = new ChatModel("gemini-2.5-flash-preview-05-20", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreview0520"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreview0520 = ModelGemini25FlashPreview0520;
    
    /// <summary>
    /// Our best model in terms of price-performance, offering well-rounded capabilities. Gemini 2.5 Flash rate limits are more restricted since it is an experimental / preview model.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreview0417 = new ChatModel("gemini-2.5-flash-preview-04-17", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreview0417"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreview0417 = ModelGemini25FlashPreview0417;
    
    /// <summary>
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview0605 = new ChatModel("gemini-2.5-pro-preview-06-05", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview0605"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview0605 = ModelGemini25ProPreview0605;
    
    /// <summary>
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview0506 = new ChatModel("gemini-2.5-pro-preview-05-06", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview0506"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview0506 = ModelGemini25ProPreview0506;
    
    /// <summary>
    /// A public experimental Gemini model with thinking mode always on by default.
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview0325 = new ChatModel("gemini-2.5-pro-preview-03-25", LLmProviders.Google, 2_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview0325"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview0325 = ModelGemini25ProPreview0325;
    
    /// <summary>
    /// Gemini 2.0 Flash Preview Image Generation delivers improved image generation features, including generating and editing images conversationally.
    /// </summary>
    public static readonly ChatModel ModelGemini2FlashPreviewImageGeneration = new ChatModel("gemini-2.0-flash-preview-image-generation", LLmProviders.Google, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashPreviewImageGeneration"/>
    /// </summary>
    public readonly ChatModel Gemini2FlashPreviewImageGeneration = ModelGemini2FlashPreviewImageGeneration;
    
    /// <summary>
    /// All known Preview Gemini models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelGemini3FlashPreview, ModelGemini3ProPreview, ModelGemini3ProImagePreview, ModelGemini31FlashImagePreview,
        ModelGemini31ProPreview, ModelGemini31ProPreviewCustomtools,
        ModelGemini25ComputerUsePreview102025, ModelGemini25ProPreview0325, ModelGemini25ProPreview0506, ModelGemini25ProPreview0605, ModelGemini25FlashPreview0417,
        ModelGemini25FlashPreview0520, ModelGemini2FlashPreviewImageGeneration, ModelGemini25FlashPreviewTts, ModelGemini25ProPreviewTts,
        ModelGemini25FlashLitePreview0617, ModelGemini25FlashImagePreview, ModelGemini25FlashPreview0925, ModelGemini25FlashLitePreview0925,
        ModelGeminiRoboticsRe15Preview
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGeminiPreview()
    {

    }
}
