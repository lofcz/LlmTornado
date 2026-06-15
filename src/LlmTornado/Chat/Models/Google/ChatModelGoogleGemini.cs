using System;
using System.Collections.Generic;
using System.Diagnostics;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Models.Vendors.Google;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Gemini class models from Google.
/// </summary>
public class ChatModelGoogleGemini : IVendorModelClassProvider
{
    /// <summary>
    /// Gemini 2.5 Pro is our state-of-the-art thinking model, capable of reasoning over complex problems in code, math, and STEM, as well as analyzing large datasets, codebases, and documents using long context.
    /// </summary>
    public static readonly ChatModel ModelGemini25Pro = new ChatModel("gemini-2.5-pro", LLmProviders.Google, 1_000_000)
    {
        ReasoningTokensMin = 128,
        ReasoningTokensMax = 32_768,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25Pro"/>
    /// </summary>
    public readonly ChatModel Gemini25Pro = ModelGemini25Pro;
    
    /// <summary>
    /// Alias pointing to gemini-3.1-pro-preview (previously gemini-3-pro-preview). Best for complex tasks that require broad world knowledge and advanced reasoning across modalities.
    /// </summary>
    public static readonly ChatModel ModelGeminiProLatest = new ChatModel("gemini-pro-latest", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 128,
        ReasoningTokensMax = 32_768,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGeminiProLatest"/>
    /// </summary>
    public readonly ChatModel GeminiProLatest = ModelGeminiProLatest;
    
    /// <summary>
    /// Alias pointing to gemini-3.5-flash. Gemini 3.5 Flash is our most intelligent Flash model for sustained frontier performance in agentic and coding tasks.
    /// </summary>
    public static readonly ChatModel ModelGeminiFlashLatest = new ChatModel("gemini-flash-latest", LLmProviders.Google, 1_048_576) 
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGeminiFlashLatest"/>
    /// </summary>
    public readonly ChatModel GeminiFlashLatest = ModelGeminiFlashLatest;
    
    /// <summary>
    /// Our best model in terms of price-performance, offering well-rounded capabilities. 2.5 Flash is best for large scale processing, low-latency, high volume tasks that require thinking, and agentic use cases.
    /// </summary>
    public static readonly ChatModel ModelGemini25Flash = new ChatModel("gemini-2.5-flash", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25Flash"/>
    /// </summary>
    public readonly ChatModel Gemini25Flash = ModelGemini25Flash;
    
    /// <summary>
    /// Alias pointing to gemini-3.1-flash-lite. Low-latency, cost-efficient model for high-volume agentic workflows and lightweight tasks.
    /// </summary>
    public static readonly ChatModel ModelGeminiFlashLiteLatest = new ChatModel("gemini-flash-lite-latest", LLmProviders.Google, 1_048_576) 
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGeminiFlashLiteLatest"/>
    /// </summary>
    public readonly ChatModel GeminiFlashLiteLatest = ModelGeminiFlashLiteLatest;
    
    /// <summary>
    /// A Gemini 2.5 Flash model optimized for cost-efficiency and high throughput.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLite = new ChatModel("gemini-2.5-flash-lite", LLmProviders.Google, 1_000_000) 
    {
        ReasoningTokensMin = 512,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ 0, -1 ]
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLite"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLite = ModelGemini25FlashLite;

    /// <summary>
    /// Gemini 3.5 Flash is our most intelligent Flash model, delivering sustained frontier performance optimized for agentic execution, coding, and long-horizon tasks at scale.
    /// Input: Text, Image, Video, Audio, and PDF. Output: Text. Context: 1M in / 64k out. Default thinking level: medium.
    /// </summary>
    public static readonly ChatModel ModelGemini35Flash = new ChatModel("gemini-3.5-flash", LLmProviders.Google, 1_048_576)
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ],
        GoogleLifecycle = new GoogleModelLifecycleInfo { Stage = GoogleModelStage.Stable }
    };

    /// <summary>
    /// <inheritdoc cref="ModelGemini35Flash"/>
    /// </summary>
    public readonly ChatModel Gemini35Flash = ModelGemini35Flash;

    /// <summary>
    /// Gemini 3.1 Flash-Lite is a low-latency, cost-efficient multimodal model for high-volume agentic workflows and lightweight tasks.
    /// Input: Text, Image, Video, Audio, and PDF. Output: Text. Context: 1M in / 64k out.
    /// </summary>
    public static readonly ChatModel ModelGemini31FlashLite = new ChatModel("gemini-3.1-flash-lite", LLmProviders.Google, 1_048_576)
    {
        ReasoningTokensMin = 0,
        ReasoningTokensMax = 24_576,
        ReasoningTokensSpecialValues = [ -1 ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelGemini31FlashLite"/>
    /// </summary>
    public readonly ChatModel Gemini31FlashLite = ModelGemini31FlashLite;

    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks (stable).
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25Flash or ModelGemini35Flash instead.")]
    public static readonly ChatModel ModelGemini2Flash001 = new ChatModel("gemini-2.0-flash-001", LLmProviders.Google, 1_000_000)
    {
        GoogleLifecycle = new GoogleModelLifecycleInfo
        {
            Stage = GoogleModelStage.Legacy,
            RetirementTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ReplacementModel = "gemini-2.5-flash"
        }
    };

    /// <summary>
    /// <inheritdoc cref="ModelGemini2Flash001"/>
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25Flash or ModelGemini35Flash instead.")]
    public readonly ChatModel Gemini2Flash001 = ModelGemini2Flash001;

    /// <summary>
    /// Fast and versatile performance across a diverse variety of tasks (latest).
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25Flash or ModelGemini35Flash instead.")]
    public static readonly ChatModel ModelGemini2FlashLatest = new ChatModel("gemini-2.0-flash", LLmProviders.Google, 1_000_000)
    {
        GoogleLifecycle = new GoogleModelLifecycleInfo
        {
            Stage = GoogleModelStage.Legacy,
            RetirementTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ReplacementModel = "gemini-2.5-flash"
        }
    };

    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashLatest"/>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25Flash or ModelGemini35Flash instead.")]
    public readonly ChatModel Gemini2FlashLatest = ModelGemini2FlashLatest;
    
    /// <summary>
    /// A Gemini 2.0 Flash model optimized for cost efficiency and low latency (stable).
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25FlashLite or ModelGemini31FlashLite instead.")]
    public static readonly ChatModel ModelGemini2FlashLite001 = new ChatModel("gemini-2.0-flash-lite-001", LLmProviders.Google, 1_000_000)
    {
        GoogleLifecycle = new GoogleModelLifecycleInfo
        {
            Stage = GoogleModelStage.Legacy,
            RetirementTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ReplacementModel = "gemini-2.5-flash-lite"
        }
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashLite001"/>
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25FlashLite or ModelGemini31FlashLite instead.")]
    public readonly ChatModel Gemini2FlashLite001 = ModelGemini2FlashLite001;
    
    /// <summary>
    /// A Gemini 2.0 Flash model optimized for cost efficiency and low latency (latest).
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25FlashLite or ModelGemini31FlashLite instead.")]
    public static readonly ChatModel ModelGemini2FlashLiteLatest = new ChatModel("gemini-2.0-flash-lite", LLmProviders.Google, 1_000_000)
    {
        GoogleLifecycle = new GoogleModelLifecycleInfo
        {
            Stage = GoogleModelStage.Legacy,
            RetirementTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ReplacementModel = "gemini-2.5-flash-lite"
        }
    };
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini2FlashLiteLatest"/>
    /// </summary>
    [Obsolete("Shut down June 1, 2026. Use ModelGemini25FlashLite or ModelGemini31FlashLite instead.")]
    public readonly ChatModel Gemini2FlashLiteLatest = ModelGemini2FlashLiteLatest;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public static readonly ChatModel ModelGemini15ProLatest = new ChatModel("gemini-1.5-pro-latest", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15ProLatest"/>
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public readonly ChatModel Gemini15ProLatest = ModelGemini15ProLatest;

    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public static readonly ChatModel ModelGemini15Pro = new ChatModel("gemini-1.5-pro", LLmProviders.Google, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro"/>
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public readonly ChatModel Gemini15Pro = ModelGemini15Pro;

    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public static readonly ChatModel ModelGemini15Pro001 = new ChatModel("gemini-1.5-pro-001", LLmProviders.Google, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro001"/>
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public readonly ChatModel Gemini15Pro001 = ModelGemini15Pro001;
    
    /// <summary>
    /// Complex reasoning tasks such as code and text generation, text editing, problem-solving, data extraction and generation.
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public static readonly ChatModel ModelGemini15Pro002 = new ChatModel("gemini-1.5-pro-002", LLmProviders.Google, 1_000_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini15Pro002"/>
    /// </summary>
    [Obsolete("Use ModelGemini25Pro instead.")]
    public readonly ChatModel Gemini15Pro002 = ModelGemini15Pro002;

    /// <summary>
    /// Gemini 1.5 Flash-8B is a small model designed for lower intelligence tasks.
    /// </summary>
    [Obsolete("Use ModelGeminiFlashLatest instead.")]
    public static readonly ChatModel ModelGemini15Flash8BLatest = new ChatModel("gemini-1.5-flash-8b-latest", LLmProviders.Google, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash8BLatest"/>
    /// </summary>
    [Obsolete("Use ModelGeminiFlashLatest instead.")]
    public readonly ChatModel Gemini15Flash8BLatest = ModelGemini15Flash8BLatest;

    /// <summary>
    /// Gemini 1.5 Flash-8B is a small model designed for lower intelligence tasks.
    /// </summary>
    [Obsolete("Use ModelGeminiFlashLatest instead.")]
    public static readonly ChatModel ModelGemini15Flash8B = new ChatModel("gemini-1.5-flash-8b", LLmProviders.Google, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelGemini15Flash8B"/>
    /// </summary>
    [Obsolete("Use ModelGeminiFlashLatest instead.")]
    public readonly ChatModel Gemini15Flash8B = ModelGemini15Flash8B;
    
    /// <summary>
    /// Gemini 2.5 Flash Image is our latest, fastest, and most efficient natively multimodal model that lets you generate and edit images conversationally.
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashImage = new ChatModel("gemini-2.5-flash-image", LLmProviders.Google, 32_768);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashImage"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashImage = ModelGemini25FlashImage;

    /// <summary>
    /// Nano Banana Pro (Gemini 3 Pro Image) is a reasoning-driven image generation and editing model for professional asset production.
    /// Supports high-resolution output (1K, 2K, 4K), advanced text rendering, Google Search grounding, and thinking mode.
    /// Input: Image and Text. Output: Image and Text. Context: 65k in / 32k out.
    /// </summary>
    public static readonly ChatModel ModelGemini3ProImage = new ChatModel("gemini-3-pro-image", LLmProviders.Google, 65_536);

    /// <summary>
    /// <inheritdoc cref="ModelGemini3ProImage"/>
    /// </summary>
    public readonly ChatModel Gemini3ProImage = ModelGemini3ProImage;

    /// <summary>
    /// Nano Banana 2 (Gemini 3.1 Flash Image) delivers high-quality image generation and conversational editing at Flash speed.
    /// Supports resolutions 512, 1K, 2K, and 4K; image search grounding; and extended aspect ratios (including 1:4, 4:1, 1:8, 8:1).
    /// Input: Text and Image / PDF. Output: Image and Text. Context: 131k in / 32k out.
    /// </summary>
    public static readonly ChatModel ModelGemini31FlashImage = new ChatModel("gemini-3.1-flash-image", LLmProviders.Google, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelGemini31FlashImage"/>
    /// </summary>
    public readonly ChatModel Gemini31FlashImage = ModelGemini31FlashImage;
    
    /// <summary>
    /// All known Gemini models from Google.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelGemini15ProLatest, 
        ModelGemini15Pro, ModelGemini15Pro001, ModelGemini15Pro002, ModelGemini15Flash8B, ModelGemini15Flash8BLatest, ModelGemini2Flash001,
        ModelGemini2FlashLatest, ModelGemini2FlashLite001, ModelGemini2FlashLiteLatest, ModelGemini25Pro, ModelGemini25Flash, 
        ModelGemini25FlashLite, ModelGemini31FlashLite, ModelGemini35Flash, ModelGeminiFlashLiteLatest, ModelGeminiFlashLatest, ModelGeminiProLatest, ModelGemini25FlashImage,
        ModelGemini3ProImage, ModelGemini31FlashImage
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelGoogleGemini()
    {

    }
}